using System.Diagnostics;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;
using CommunityToolkit.WinUI;
using static MediaMaster.Services.WindowsNativeValues;
using static MediaMaster.Services.WindowsApiService;
using Microsoft.UI.Xaml.Media;
using System.Runtime.InteropServices.WindowsRuntime;
using MediaMaster.Controls;
using Microsoft.Identity.Client;

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

public static class TaskDelaySafe
{
    public static async Task Delay(int millisecondsDelay, CancellationToken cancellationToken)
    {
        await TaskDelaySafe.Delay(TimeSpan.FromMilliseconds(millisecondsDelay), cancellationToken);
    }

    public static async Task Delay(TimeSpan delay, CancellationToken cancellationToken)
    {
        var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var task = new TaskCompletionSource<int>();

        tokenSource.Token.Register(() => task.SetResult(0));

        await Task.WhenAny(
            Task.Delay(delay, CancellationToken.None),
            task.Task);
    }
}

public static class IconService
{
    public static readonly BitmapImage DefaultIcon = new(new Uri("ms-appx:///Assets/SplashScreen.scale-200.png"));

    // https://github.com/castorix/WinUI3_MediaEngine/blob/master/CMediaEngine.cs#L2136-L2168
    public static async Task<WriteableBitmap?> GetCaptureWriteableBitmap(IntPtr m_hBitmapCapture)
    {
        if (m_hBitmapCapture != IntPtr.Zero)
        {
            BITMAP bm;
            GetObject(m_hBitmapCapture, Marshal.SizeOf(typeof(BITMAP)), out bm);
            int nWidth = bm.bmWidth;
            int nHeight = bm.bmHeight;
            BITMAPV5HEADER bi = new BITMAPV5HEADER();
            bi.bV5Size = Marshal.SizeOf(typeof(BITMAPV5HEADER));
            bi.bV5Width = nWidth;
            bi.bV5Height = -nHeight;
            bi.bV5Planes = 1;
            bi.bV5BitCount = 32;
            bi.bV5Compression = BI_BITFIELDS;
            bi.bV5AlphaMask = unchecked((int)0xFF000000);
            bi.bV5RedMask = 0x00FF0000;
            bi.bV5GreenMask = 0x0000FF00;
            bi.bV5BlueMask = 0x000000FF;

            IntPtr hDC = CreateCompatibleDC(IntPtr.Zero);
            IntPtr hBitmapOld = SelectObject(hDC, m_hBitmapCapture);
            int nNumBytes = (int)(nWidth * 4 * nHeight);
            byte[] pPixels = new byte[nNumBytes];
            int nScanLines = GetDIBits(hDC, m_hBitmapCapture, 0, (uint)nHeight, pPixels, ref bi, DIB_RGB_COLORS);

            for (int i = 0; i < nNumBytes; i += 4)
            {
                byte alpha = pPixels[i + 3];
                pPixels[i + 0] = (byte)((pPixels[i + 0] * alpha) / 255); // Blue
                pPixels[i + 1] = (byte)((pPixels[i + 1] * alpha) / 255); // Green
                pPixels[i + 2] = (byte)((pPixels[i + 2] * alpha) / 255); // Red
            }

            WriteableBitmap m_hWriteableBitmapCapture = new WriteableBitmap(nWidth, nHeight);

            await using (Stream pixelStream = m_hWriteableBitmapCapture.PixelBuffer.AsStream())
            {
                await pixelStream.WriteAsync(pPixels, 0, pPixels.Length);
            }

            m_hWriteableBitmapCapture.Invalidate();

            SelectObject(hDC, hBitmapOld);
            DeleteDC(hDC);

            return m_hWriteableBitmapCapture;
        }

        return null;
    }

    public static MyCancellationTokenSource AddImage1(string? path, ImageMode imageMode, int width, int height, Microsoft.UI.Xaml.Controls.Image image)
    {
        image.Source = null;
        var tokenSource = new MyCancellationTokenSource();
        Task.Run(() => AddImage(path, imageMode, width, height, tokenSource, image), tokenSource.Token);
        return tokenSource;
    }

    private static async Task AddImage(string? path, ImageMode imageMode, int width, int height, MyCancellationTokenSource cts, Microsoft.UI.Xaml.Controls.Image image)
    {
        using (cts)
        {
            if (cts.Token.IsCancellationRequested) return;

            //await TaskDelaySafe.Delay(1000, cts.Token);

            //if (cts.Token.IsCancellationRequested) return;

            if (File.Exists(path))
            {
                if (imageMode == ImageMode.IconAndThumbnail)
                {
                    await SetImage(path, ImageMode.IconOnly, width, height, cts, image);
                    await SetImage(path, ImageMode.ThumbnailOnly, width, height, cts, image);
                }
                else
                {
                    await SetImage(path, imageMode, width, height, cts, image);
                }
            }
            else
            {
                await App.DispatcherQueue.EnqueueAsync(() => image.Source = DefaultIcon);
            }
        }
    }

    private static async Task SetImage(string path, ImageMode imageMode, int width, int height, CancellationTokenSource cts, Microsoft.UI.Xaml.Controls.Image image)
    {
        if (cts.Token.IsCancellationRequested) return;

        SIIGBF options = imageMode switch
        {
            ImageMode.IconOnly => SIIGBF.SIIGBF_ICONONLY,
            _ => 0
        };

        ImageSource? icon = null;

        if (imageMode != ImageMode.ThumbnailOnly)
        {
            icon = await GetThumbnail(path, width, height, options | SIIGBF.SIIGBF_INCACHEONLY);
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
            BitmapSource? icon = null;
            try
            {
                icon = await App.DispatcherQueue.EnqueueAsync(() => GetCaptureWriteableBitmap(hBitmap));
            }
            catch (COMException ex)
            {
                if (!(ex.ErrorCode == -2147175936 && options.HasFlag(SIIGBF.SIIGBF_THUMBNAILONLY))) // -2147175936 == 0x8004B200
                {
                    throw;
                }
            }
            finally
            {
                DeleteObject(hBitmap);
            }

            return icon;
        }

        return null;
    }

    private static IntPtr GetHBitmap(string fileName, int width, int height, SIIGBF options)
    {
        Guid shellItem2Guid = typeof(IShellItemImageFactory).GUID;
        var retCode = SHCreateItemFromParsingName(fileName, IntPtr.Zero, ref shellItem2Guid, out IShellItemImageFactory nativeShellItem);

        if (retCode == 0)
        {
            SIZE nativeSize = new SIZE(width, height);

            HResult hr = nativeShellItem.GetImage(nativeSize, options, out var hBitmap);
            if (hr == HResult.Ok)
            {
                return hBitmap;
            }
        }

        return IntPtr.Zero;
    }
}
