using System.Net.Cache;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices;
using CommunityToolkit.WinUI;
using static MediaMaster.Services.WindowsNativeValues;
using static MediaMaster.Services.WindowsApiService;
using static MediaMaster.Services.WindowsNativeInterfaces;
using Microsoft.UI.Xaml.Media;
using System.Runtime.InteropServices.WindowsRuntime;
using FaviconFetcher;
using MediaMaster.Extensions;

namespace MediaMaster.Services;

public enum ImageMode
{
    IconOnly,
    IconAndThumbnail,
    ThumbnailOnly
}

public class MyCancellationTokenSource : CancellationTokenSource
{
    public bool IsDisposed { get; private set; }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        IsDisposed = true;
    }
}

public static class IconService
{
    private static readonly BitmapImage DefaultIcon = new(new Uri("ms-appx:///Assets/SplashScreen.scale-200.png"));
    private static readonly BitmapImage WebsiteIcon = new(new Uri("ms-appx:///Assets/SplashScreen.scale-200.png"));


    public static MyCancellationTokenSource? AddImage(string? path, ImageMode imageMode, int width, int height, Microsoft.UI.Xaml.Controls.Image image)
    {
        image.Source = null;
        if (path == null) return null;

        var tokenSource = new MyCancellationTokenSource();
        Task.Factory.StartNew(() => AddImageAsync(path, imageMode, width, height, tokenSource, image), tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        return tokenSource;
    }

    private static async Task AddImageAsync(string path, ImageMode imageMode, int width, int height, MyCancellationTokenSource cts, Microsoft.UI.Xaml.Controls.Image image)
    {
        using (cts)
        {
            if (cts.Token.IsCancellationRequested) return;

            if (path.IsWebsite())
            {
                if (imageMode != ImageMode.ThumbnailOnly)
                {
                    await App.DispatcherQueue.EnqueueAsync(() => image.Source = WebsiteIcon);
                }

                await SetWebsiteIcon(path, width, height, cts, image);
                
            }
            else if (File.Exists(path))
            {
                if (imageMode == ImageMode.IconAndThumbnail)
                {
                    await SetFileIcon(path, ImageMode.IconOnly, width, height, cts, image);
                    await SetFileIcon(path, ImageMode.ThumbnailOnly, width, height, cts, image);
                }
                else
                {
                    await SetFileIcon(path, imageMode, width, height, cts, image);
                }
            }
            else
            {
                await App.DispatcherQueue.EnqueueAsync(() => image.Source = DefaultIcon);
            }
        }
    }

    private static readonly HttpSource Source = new()
    {
        CachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable)
    };
    private static Fetcher? _fetcher;

    private static async Task SetWebsiteIcon(string path, int width, int height, MyCancellationTokenSource cts, Microsoft.UI.Xaml.Controls.Image image)
    {
        if (cts.Token.IsCancellationRequested) return;

        Uri url = new(path);
        IconImage? icon = null;
        FetchOptions options = new()
        {
            PerfectSize = new IconSize(width, height)
        };
        _fetcher ??= new Fetcher(Source);
        try
        {
            icon = _fetcher.Fetch(url, options);
        }
        catch
        {
            // ignore all exceptions
        }
        if (icon == null || cts.Token.IsCancellationRequested) return;

        ImageSource? source = null;
        await App.DispatcherQueue.EnqueueAsync(async () =>
        {
            var bitmap = new WriteableBitmap(icon.ToSKBitmap().Width, icon.ToSKBitmap().Height);
            await using (Stream stream = bitmap.PixelBuffer.AsStream())
            {
                stream.Write(icon.Bytes);
            }

            icon.Dispose();
            source = bitmap;
        });

        if (!cts.Token.IsCancellationRequested && source != null)
        {
            await App.DispatcherQueue.EnqueueAsync(() => image.Source = source);
        }
        
    }

    private static async Task SetFileIcon(string path, ImageMode imageMode, int width, int height, CancellationTokenSource cts, Microsoft.UI.Xaml.Controls.Image image)
    {
        if (cts.Token.IsCancellationRequested) return;

        SIIGBF options = imageMode switch
        {
            ImageMode.IconOnly => SIIGBF.IconOnly,
            _ => 0
        };

        ImageSource? icon = null;

        if (imageMode != ImageMode.ThumbnailOnly)
        {
            icon = await GetThumbnail(path, width, height, options | SIIGBF.InCacheOnly);
        }

        if (icon == null)
        {
            icon = await GetThumbnail(path, width, height, options);
        }

        if (cts.Token.IsCancellationRequested) return;

        if (icon != null)
        {
            await App.DispatcherQueue.EnqueueAsync(() => image.Source = icon);
        }
    }

    // https://github.com/rlv-dan/ShellThumbs/blob/master/ShellThumbs.cs#L64-L104
    public static async Task<ImageSource?> GetThumbnail(string fileName, int width, int height, SIIGBF options)
    {
        var hBitmap = GetHBitmap(fileName, width, height, options);
        if (hBitmap != IntPtr.Zero)
        {
            BitmapSource? icon = await GetCaptureWriteableBitmap(hBitmap);
            DeleteObject(hBitmap);
            return icon;
        }

        return null;
    }

    private static IntPtr GetHBitmap(string fileName, int width, int height, SIIGBF options)
    {
        Guid shellItem2Guid = typeof(IShellItemImageFactory).GUID;
        var retCode = SHCreateItemFromParsingName(fileName, IntPtr.Zero, ref shellItem2Guid, out IShellItem nativeShellItem);

        if (retCode == HResult.Ok)
        {
            Size nativeSize = new(width, height);

            HResult hr = (nativeShellItem as IShellItemImageFactory).GetImage(nativeSize, options, out var hBitmap);
            if (hr == HResult.Ok)
            {
                return hBitmap;
            }
        }

        return IntPtr.Zero;
    }

    // https://github.com/castorix/WinUI3_MediaEngine/blob/master/CMediaEngine.cs#L2136-L2168
    private static async Task<WriteableBitmap?> GetCaptureWriteableBitmap(IntPtr m_hBitmapCapture)
    {
        if (m_hBitmapCapture == IntPtr.Zero) return null;

        var result = GetObject(m_hBitmapCapture, Marshal.SizeOf(typeof(BITMAP)), out BITMAP bm);
        if (result == 0) return null;

        var nWidth = bm.bmWidth;
        var nHeight = bm.bmHeight;
        BITMAPV5HEADER bi = new()
        {
            bV5Size = (uint)Marshal.SizeOf(typeof(BITMAPV5HEADER)),
            bV5Width = nWidth,
            bV5Height = -nHeight,
            bV5Planes = 1,
            bV5BitCount = 32,
            bV5Compression = BI_BITFIELDS,
            bV5AlphaMask = 0xFF000000,
            bV5RedMask = 0x00FF0000,
            bV5GreenMask = 0x0000FF00,
            bV5BlueMask = 0x000000FF
        };

        var hDC = CreateCompatibleDC(IntPtr.Zero);
        try
        {
            var hBitmapOld = SelectObject(hDC, m_hBitmapCapture);
            try
            {
                var nNumBytes = nWidth * 4 * nHeight;
                var pPixels = new byte[nNumBytes];
                result = GetDIBits(hDC, m_hBitmapCapture, 0, (uint)nHeight, pPixels, ref bi, DIB_RGB_COLORS);
                if (result != 0)
                {
                    ApplyAlphaChannel(pPixels);
                    return await CreateWriteableBitmapFromPixels(nWidth, nHeight, pPixels);
                }

                return null;
            }
            finally
            {
                SelectObject(hDC, hBitmapOld);
            }
        }
        finally
        {
            DeleteDC(hDC);
        }
    }

    private static void ApplyAlphaChannel(byte[] pixels)
    {
        var pixelsSpan = new Span<byte>(pixels);

        for (var i = 0; i < pixelsSpan.Length; i += 4)
        {
            var alpha = pixelsSpan[i + 3];
            switch (alpha)
            {
                case 255:
                    break;

                case 0:
                    {
                        pixelsSpan[i + 0] = 0; // Blue
                        pixelsSpan[i + 1] = 0; // Green
                        pixelsSpan[i + 2] = 0; // Red
                        break;
                    }

                default:
                    {
                        var multiplier = alpha / 255.0;
                        pixelsSpan[i + 0] = (byte)(pixelsSpan[i + 0] * multiplier); // Blue
                        pixelsSpan[i + 1] = (byte)(pixelsSpan[i + 1] * multiplier); // Green
                        pixelsSpan[i + 2] = (byte)(pixelsSpan[i + 2] * multiplier); // Red
                        break;
                    }
            }
        }
    }

    private static async Task<WriteableBitmap> CreateWriteableBitmapFromPixels(int width, int height, byte[] pixels)
    {
        return await App.DispatcherQueue.EnqueueAsync(async () =>
        {
            var bitmap = new WriteableBitmap(width, height);
            await using (Stream pixelStream = bitmap.PixelBuffer.AsStream())
            {
                await pixelStream.WriteAsync(pixels);
            }
            bitmap.Invalidate();
            return bitmap;
        });
    }
}
