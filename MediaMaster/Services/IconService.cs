using System.Collections.Concurrent;
using System.Net.Cache;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices;
using CommunityToolkit.WinUI;
using static MediaMaster.WIn32.WindowsNativeValues;
using static MediaMaster.WIn32.WindowsApiService;
using static MediaMaster.WIn32.WindowsNativeInterfaces;
using System.Runtime.InteropServices.WindowsRuntime;
using FaviconFetcher;
using MediaMaster.Extensions;

namespace MediaMaster.Services;

[Flags]
public enum ImageMode : uint
{
    IconOnly = 0x1,
    IconAndThumbnail = 0x2,
    ThumbnailOnly = 0x4,
    CacheOnly = 0x08
}

public readonly struct CachedIcon
{
    public BitmapSource Icon { get; init; }
    public int RequestedWidth { get; init; }
    public int RequestedHeight { get; init; }
}

public static class IconService
{
    private static readonly BitmapImage FileIcon = new(new Uri("ms-appx:///Assets/DefaultIcon.png"));
    private static readonly BitmapImage WebsiteIcon = new(new Uri("ms-appx:///Assets/DefaultIcon.png"));
    private static readonly BitmapImage DefaultIcon = new(new Uri("ms-appx:///Assets/DefaultIcon.png"));

    private static readonly ConcurrentDictionary<string, ICollection<CachedIcon>> ThumbnailCache = [];
    private static readonly ConcurrentQueue<string> ThumbnailCacheKeys = [];
    private static readonly ConcurrentDictionary<string, ICollection<CachedIcon>> IconCache = [];
    private static readonly ConcurrentQueue<string> IconCacheKeys = [];

    private const int CacheLength = 2001;

    public static async Task<BitmapSource?> GetIcon(string? path, ImageMode imageMode, int width, int height, TaskCompletionSource? tcs = null)
    {
        if (path == null) return null;

        return await STATask.StartSTATask(async () =>
        {
            var icon = await GetIconAsync(path, imageMode, width, height, tcs).ConfigureAwait(false);
            return icon == null && !imageMode.HasFlag(ImageMode.CacheOnly) ? GetDefaultIcon(path) : icon;
        });
    }

    public static async void SetIcon(string? path, ImageMode imageMode, int width, int height, Image image, TaskCompletionSource tcs)
    {
        if (path == null) return;

        if (imageMode.HasFlag(ImageMode.IconAndThumbnail))
        {
            await STATask.StartSTATask(async () =>
            {
                var icon = await GetIconAsync(path, ImageMode.IconOnly, width, height, tcs).ConfigureAwait(false);

                if (tcs.Task.IsCompleted) return;

                if (icon == null)
                {
                    icon = GetDefaultIcon(path);
                }

                await App.DispatcherQueue.EnqueueAsync(() => image.Source = icon).ConfigureAwait(false);
            });
        }

        await STATask.StartSTATask(async () =>
        {
            var icon = await GetIconAsync(path, imageMode, width, height, tcs);

            if (tcs.Task.IsCompleted) return;

            if (icon == null)
            {
                icon = GetDefaultIcon(path);
            }

            await App.DispatcherQueue.EnqueueAsync(() => image.Source = icon).ConfigureAwait(false);
        });
        
    }

    public static BitmapSource GetDefaultIcon(string? path)
    {
        if (path?.IsWebsite() is true)
        {
            return WebsiteIcon;
        }

        if (File.Exists(path))
        {
            return FileIcon;
        }

        return DefaultIcon;
        
    }

    public static async Task<BitmapSource?> GetIconAsync(string path, ImageMode imageMode, int width, int height, TaskCompletionSource? tcs = null)
    {
        if (tcs is { Task.IsCompleted: true }) return null;
        ThumbnailCache.TryGetValue(path, out var cachedIconCollection);
        if (imageMode.HasFlag(ImageMode.IconOnly) && !path.IsWebsite())
        {
            IconCache.TryGetValue(path, out cachedIconCollection);
        }

        if (cachedIconCollection != null)
        {
            foreach (var cachedIcon in cachedIconCollection)
            {
                if (width < height)
                {
                    if (width == cachedIcon.RequestedWidth)
                    {
                        return cachedIcon.Icon;
                    }
                }
                else
                {
                    if (height == cachedIcon.RequestedHeight)
                    {
                        return cachedIcon.Icon;
                    }
                }
            }
        }

        if (imageMode.HasFlag(ImageMode.CacheOnly)) return null;

        if (path.IsWebsite())
        {
            return await SetWebsiteIcon(path, width, height, tcs);
        }

        if (File.Exists(path))
        {
            return await SetFileIcon(path, imageMode, width, height, tcs);
        }

        return null;
    }

    private static readonly HttpSource Source = new()
    {
        CachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable)
    };
    private static Fetcher? _fetcher;

    private static async Task<BitmapSource?> SetWebsiteIcon(string path, int width, int height, TaskCompletionSource? tcs = null)
    {
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
        if (icon == null || tcs is { Task.IsCompleted: true }) return null;

        BitmapSource? source = null;
        if (icon.Size.Width != 0 && icon.Size.Height != 0)
        {
            await App.DispatcherQueue.EnqueueAsync(async () =>
            {
                var bitmap = new WriteableBitmap(icon.Size.Width, icon.Size.Height);
                await using (Stream stream = bitmap.PixelBuffer.AsStream())
                {
                    stream.Write(icon.Bytes);
                }

                icon.Dispose();
                source = bitmap;
            });
        }

        if (source == null || tcs is { Task.IsCompleted: true })
        {
            return null;
        }

        if (!ThumbnailCache.TryGetValue(path, out var cachedIconCollection))
        {
            cachedIconCollection = [];
            ThumbnailCache[path] = cachedIconCollection;
            ThumbnailCacheKeys.Enqueue(path);

            if (ThumbnailCache.Count > CacheLength &&  ThumbnailCacheKeys.TryDequeue(out var item))
            {
                ThumbnailCache.Remove(item, out _);
            }
        }
        cachedIconCollection.Add(new CachedIcon { Icon = source, RequestedWidth = width, RequestedHeight = height });
        return source;
    }

    private static async Task<BitmapSource?> SetFileIcon(string path, ImageMode imageMode, int width, int height, TaskCompletionSource? tcs = null)
    {
        SIIGBF options = 0;


        if (imageMode.HasFlag(ImageMode.IconOnly))
        {
            options |= SIIGBF.IconOnly;
        }

        BitmapSource? icon = await GetThumbnail(path, width, height, options);

        if (icon == null || tcs is { Task.IsCompleted: true })
        {
            return null;
        }

        if (imageMode.HasFlag(ImageMode.IconOnly))
        {
            if (!IconCache.TryGetValue(path, out var cachedIconCollection))
            {
                cachedIconCollection = [];
                IconCache[path] = cachedIconCollection;
                IconCacheKeys.Enqueue(path);

                if (IconCache.Count > CacheLength && IconCacheKeys.TryDequeue(out var item))
                {
                    IconCache.Remove(item, out _);
                }
            }
            cachedIconCollection.Add(new CachedIcon { Icon = icon, RequestedWidth = width, RequestedHeight = height });
        }
        else
        {
            if (!ThumbnailCache.TryGetValue(path, out var cachedIconCollection))
            {
                cachedIconCollection = [];
                ThumbnailCache[path] = cachedIconCollection;
                ThumbnailCacheKeys.Enqueue(path);

                if (ThumbnailCache.Count > CacheLength && ThumbnailCacheKeys.TryDequeue(out var item))
                {
                    ThumbnailCache.Remove(item, out _);
                }
            }
            cachedIconCollection.Add(new CachedIcon { Icon = icon, RequestedWidth = width, RequestedHeight = height });
        }

        return icon;
    }

    // https://github.com/rlv-dan/ShellThumbs/blob/master/ShellThumbs.cs#L64-L104
    public static async Task<BitmapSource?> GetThumbnail(string fileName, int width, int height, SIIGBF options)
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

            HResult hr = ((IShellItemImageFactory)nativeShellItem).GetImage(nativeSize, options, out var hBitmap);
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
