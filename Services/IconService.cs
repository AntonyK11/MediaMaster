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

public static class IconService
{
    public static readonly BitmapImage DefaultIcon = new(new Uri("ms-appx:///Assets/SplashScreen.scale-200.png"));

    public static async Task<ImageSource> CreateSoftwareBitmapSourceAsync(Bitmap bitmap)
    {
        using (MemoryStream memoryStream = new())
        {
            bitmap.Save(memoryStream, ImageFormat.Png);
            memoryStream.Position = 0;
            return await App.DispatcherQueue.EnqueueAsync(async () =>
            {
                BitmapImage bitmapImage = new BitmapImage();

                await bitmapImage.SetSourceAsync(memoryStream.AsRandomAccessStream());

                return bitmapImage;
            });
        }
    }

    public static async Task<WriteableBitmap> ConvertBitmapToWriteableBitmap(Bitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        // Create a WriteableBitmap
        WriteableBitmap writeableBitmap = new WriteableBitmap(width, height);

        // Access the pixel buffer
        await using (Stream stream = writeableBitmap.PixelBuffer.AsStream())
        {
            BitmapData? bitmapData = null;

            try
            {
                // Lock the bitmap data
                var bmpBounds = new Rectangle(0, 0,width, height);
                bitmapData = bitmap.LockBits(bmpBounds, ImageLockMode.ReadOnly, bitmap.PixelFormat);

                // Calculate the number of bytes
                var byteCount = bitmapData.Stride * bitmapData.Height;
                var pixels = new byte[byteCount];

                // Copy the bitmap data to the byte array
                Marshal.Copy(bitmapData.Scan0, pixels, 0, byteCount);

                // Write the pixels to the stream
                await stream.WriteAsync(pixels, 0, byteCount);
            }
            finally
            {
                // Unlock the bitmap data
                if (bitmapData != null)
                {
                    bitmap.UnlockBits(bitmapData);
                }
            }
        }

        return writeableBitmap;
    }

    //public static IntPtr ExtractIcon(string path, int size)
    //{
    //    // Determine the index of the desired icon in the system image list.
    //    var shfi = new SHFILEINFO();
    //    _ = SHGetFileInfo(path, 0, ref shfi, (uint)Marshal.SizeOf(shfi), SHGFI_SYSICONINDEX | SHGFI_LARGEICON);

    //    // Retrieve the system image list.
    //    Guid iidImageList = typeof(IImageList).GUID;
    //    if (SHGetImageList(size, ref iidImageList, out IImageList iml) == 0)
    //    {
    //        if (iml.GetIcon(shfi.iIcon, ILD_TRANSPARENT | ILD_ASYNC, out IntPtr hIcon) == 0 && hIcon != IntPtr.Zero)
    //        {
    //            return hIcon;
    //        }
    //    }

    //    return IntPtr.Zero;
    //}

    //public async static Task<ImageSource> GetExtensionIcon(string path, int size = SHIL_JUMBO)
    //{
    //    if (File.Exists(path))
    //    {
    //        //var hIcon = ExtractIcon(path, size);
    //        //if (hIcon != IntPtr.Zero)
    //        //{
    //        //    Bitmap bitmap;
    //        //    using (Icon icon = Icon.FromHandle(hIcon))
    //        //    {
    //        //        bitmap = icon.ToBitmap();
    //        //    }
    //        //    DestroyIcon(hIcon);

    //        //    return await CreateSoftwareBitmapSourceAsync(bitmap);
    //        //}
    //        ShellFile shellFile = ShellFile.FromFilePath(path);
    //        shellFile.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
    //        // Use default large icon size, but you can specify any size here
    //        var bitmap = shellFile.Thumbnail.ExtraLargeBitmap;

    //        return await CreateSoftwareBitmapSourceAsync(bitmap);
    //    }

    //    return DefaultIcon;
    //}

    //public async static Task<ImageSource> GetIcon(string path, int size, SIIGBF flags)
    //{
    //    if (Path.Exists(path))
    //    {
    //        Guid iidImageFactory = typeof(IShellItemImageFactory).GUID;
    //        int hr = SHCreateItemFromParsingName(path, IntPtr.Zero, ref iidImageFactory, out IShellItemImageFactory imageFactory);
    //        if (hr == 0)
    //        {
    //            imageFactory.GetImage(new SIZE(size, size), flags, out IntPtr hBitmap);
    //            using (Bitmap bitmap = Image.FromHbitmap(hBitmap))
    //            {
    //                DeleteObject(hBitmap);

    //                bitmap.MakeTransparent(Color.Black);
    //                return await CreateSoftwareBitmapSourceAsync(bitmap);
    //            }
    //        }
    //    }
    //    return DefaultIcon;
    //}

    //public async static Task<BitmapImage?> GetFileIcon(string path, int size)
    //{
    //    if (Path.GetExtension(path).Equals(".url", StringComparison.CurrentCultureIgnoreCase)) return DefaultIcon;

    //    var file = await StorageFile.GetFileFromPathAsync(path);
    //    var iconThumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, ((uint)size));

    //    if (iconThumbnail == null) return DefaultIcon;

    //    var bitmap = new BitmapImage();
    //    await bitmap.SetSourceAsync(iconThumbnail);

    //    if (iconThumbnail == null) return DefaultIcon;
    //    return bitmap;
    //}

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

    private static async Task SetImage(string path, ImageMode imageMode, int width, int height, MyCancellationTokenSource cts, Microsoft.UI.Xaml.Controls.Image image)
    {
        if (cts.Token.IsCancellationRequested) return;

        SIIGBF options = imageMode switch
        {
            ImageMode.IconOnly => SIIGBF.SIIGBF_ICONONLY,
            _ => 0
        };

        ImageSource? icon = await GetThumbnail(path, width, height, options);

        if (cts.Token.IsCancellationRequested) return;

        await App.DispatcherQueue.EnqueueAsync(() => image.Source = icon);
    }

    public static async Task<ImageSource?> GetThumbnail(string fileName, int width, int height, SIIGBF options)
    {
        if (Path.Exists(fileName))
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
        }

        return null;
    }

    //public static Bitmap GetBitmapFromHBitmap(IntPtr nativeHBitmap)
    //{
    //    Bitmap bmp = Image.FromHbitmap(nativeHBitmap);

    //    if (Image.GetPixelFormatSize(bmp.PixelFormat) < 32)
    //    {
    //        return bmp;
    //    }

    //    return CreateAlphaBitmap(bmp, PixelFormat.Format32bppArgb);
    //}

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

    //private static unsafe Bitmap CreateAlphaBitmap(Bitmap srcBitmap, PixelFormat targetPixelFormat)
    //{
    //    var result = new Bitmap(srcBitmap.Width, srcBitmap.Height, targetPixelFormat);

    //    var bmpBounds = new Rectangle(0, 0, srcBitmap.Width, srcBitmap.Height);
    //    var srcData = srcBitmap.LockBits(bmpBounds, ImageLockMode.ReadOnly, srcBitmap.PixelFormat);
    //    var destData = result.LockBits(bmpBounds, ImageLockMode.ReadOnly, targetPixelFormat);

    //    var srcDataPtr = (byte*)srcData.Scan0;
    //    var destDataPtr = (byte*)destData.Scan0;

    //    try
    //    {
    //        for (var y = 0; y <= srcData.Height - 1; y++)
    //        {
    //            for (var x = 0; x <= srcData.Width - 1; x++)
    //            {
    //                //this is really important because one stride may be positive and the other negative
    //                var position = srcData.Stride * y + 4 * x;
    //                var position2 = destData.Stride * y + 4 * x;

    //                memcpy(destDataPtr + position2, srcDataPtr + position, 4);
    //            }
    //        }
    //    }
    //    finally
    //    {
    //        srcBitmap.UnlockBits(srcData);
    //        result.UnlockBits(destData);
    //    }

    //    using (srcBitmap)
    //    {
    //        return result;
    //    }
    //}
}
//}

