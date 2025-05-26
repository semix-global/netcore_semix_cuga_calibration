using Net.Utilities.Helper.File;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Net.Utilities.WPF.Helper;

public static class BitmapSourceHelper
{
    #region 通用

    /// <summary>
    /// 复制BitmapSource
    /// </summary>
    /// <param name="bitmapSource">bitmapSource</param>
    /// <returns>copy bitmap</returns>
    public static BitmapSource Copy(BitmapSource bitmapSource)
    {
        // 创建一个新的 WriteableBitmap 对象
        var clone = new WriteableBitmap(
            bitmapSource.PixelWidth,
            bitmapSource.PixelHeight,
            bitmapSource.DpiX,
            bitmapSource.DpiY,
            bitmapSource.Format,
            bitmapSource.Palette);
        var srcStride = bitmapSource.PixelWidth * ((bitmapSource.Format.BitsPerPixel + 7) / 8);
        unsafe
        {
            var data = new byte[bitmapSource.PixelHeight * srcStride];
            fixed (byte* srcPtr = data)
            {
                bitmapSource.CopyPixels(Int32Rect.Empty, new IntPtr(srcPtr), data.Length, srcStride);
                clone.WritePixels(new Int32Rect(0, 0, bitmapSource.PixelWidth, bitmapSource.PixelHeight), new IntPtr(srcPtr), data.Length, srcStride);
            }
        }

        clone.Freeze(); //Important to freeze it, otherwise it will still have minor leaks

        return clone;
    }

    /// <summary>
    /// 保存更快, 有压缩
    /// </summary>
    /// <param name="bitmapSource">图片</param>
    /// <param name="filePath">图片路径</param>
    public static void Save(BitmapSource bitmapSource, string filePath)
    {
        DirectoryHelper.CreateFileDirectoryIfNotExists(filePath);
        FileHelper.DeleteFileIfExists(filePath);

        BitmapEncoder encoder = Path.GetExtension(filePath) == ".png" ? new PngBitmapEncoder() : new BmpBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
        using var fileStream = File.Create(filePath);
        encoder.Save(fileStream);
    }

    /// <summary>
    /// 创建空白BitmapSource
    /// </summary>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <returns>空白BitmapSource</returns>
    public static BitmapSource CreateEmptyBitmapSource(int width, int height)
    {
        var pixelFormat = PixelFormats.Indexed1; // 1bit: 只有两种颜色
        var stride = (width * pixelFormat.BitsPerPixel + 7) / 8;
        var pixels = new byte[height * stride];
        var palette = new BitmapPalette([Colors.Blue, Colors.Green]);

        var emptyBitmapSource = BitmapSource.Create(width, height, 96, 96, pixelFormat, palette, pixels, stride);
        emptyBitmapSource.Freeze();

        return emptyBitmapSource;
    }

    #endregion 通用

    #region Raw Bytes

    /// <summary>
    /// BitmapSource to byte raw array(适用于所有BitmapSource 8bit 24bit 32bit, 且BitmapSource的内存排列方式都是bgrx顺序)
    /// </summary>
    /// <param name="bitmapSource">适用于所有BitmapSource 8bit 24bit 32bit</param>
    /// <returns>byte raw array, 排列方式都是bgrx</returns>
    public static byte[] BitmapSourceToByteRawArray(BitmapSource bitmapSource)
    {
        var height = bitmapSource.PixelHeight; // 图像的高度
        var width = bitmapSource.PixelWidth; // 图像的宽度
        var bpp = bitmapSource.Format.BitsPerPixel;
        var channels = GetChannels(bitmapSource.Format);
        // 行步幅数|扫描宽度: 宽度 * 表示每个像素所占用的字节数(比特数 肯定>=bpp/8, 最小满足8的倍数的bit值)
        var srcStride = width * ((bpp + 7) / 8); // 等价于 width * channel (bpp/8 = channels)
        unsafe
        {
            var data = new byte[height * width * channels];
            fixed (byte* dstPtr = data)
            {
                bitmapSource.CopyPixels(Int32Rect.Empty, new IntPtr(dstPtr), data.Length, srcStride);
            }

            return data;
        }
    }

    /// <summary>
    /// byte raw array to BitmapSource(适用于所有BitmapSource 8bit 24bit 32bit, 且BitmapSource的内存排列方式都是bgrx顺序)
    /// </summary>
    /// <param name="data">数据, 排列方式都是bgrx</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="pixelFormat">格式</param>
    /// <returns>BitmapSource(适用于所有BitmapSource 8bit 24bit 32bit, 且BitmapSource的内存排列方式都是bgrx顺序)</returns>
    public static BitmapSource ByteRawArrayToBitmapSource(byte[] data, int width, int height, PixelFormat pixelFormat)
    {
        // pixelFormat.BitsPerPixel // 表示每个像素所占用的bit数 Gray8: 8 Bgr24: 24 Bgr32: 32 Bgra32: 32 Pbgra32: 32
        var channel = GetChannels(pixelFormat);
        var wb = new WriteableBitmap(width, height, 96, 96, pixelFormat, null); // 创建对象耗时
        // wb.PixelWidth // pixel宽度 | wb.PixelHeight // pixel高度 | wb.DpiX // 水平dpi | wb.DpiY // 垂直dpi
        // wb.Format // 格式
        // wb.BackBufferStride // 行步幅数|扫描宽度
        // wb.Palette // 调色板
        // wb.Lock(); // 保留后台缓存区用于更新
        // dst.AddDirtyRect() // 指定更改的位图区域
        // dst.Unlock() // 释放后台缓冲区, 以使之可用于显示
        unsafe
        {
            // var dstPtr = (byte*)wb.BackBuffer; // 指针, 可以用Buffer.MemoryCopy来拷贝类似于BitmapHelper中的方法, 但是速度并没有WritePixels(微软封装的)快
            fixed (byte* srcPtr = data)
            {
                wb.WritePixels(new Int32Rect(0, 0, width, height), new IntPtr(srcPtr), data.Length, width * channel);
            }
        }

        wb.Freeze();

        return wb;
    }

    /// <summary>
    /// 获取图片的通道数
    /// </summary>
    /// <param name="pixelFormat">图片格式</param>
    /// <returns>通道数</returns>
    /// <exception cref="ArgumentException">不支持格式异常</exception>
    public static int GetChannels(PixelFormat pixelFormat)
    {
        if (pixelFormat == PixelFormats.Gray8 || pixelFormat == PixelFormats.Indexed8) return 1;
        // Bgr24: B G R 内存排列方式都是bgr顺序
        if (pixelFormat == PixelFormats.Bgr24) return 3;
        // Bgr32: B G R 预留 内存排列方式都是bgrx顺序
        // Bgra32: B G R A 内存排列方式都是bgrx顺序
        // Pbgra32: alpha分量的数据已经被预先乘进去, 内存排列方式都是bgrx顺序
        if (pixelFormat == PixelFormats.Bgr32 || pixelFormat == PixelFormats.Bgra32 || pixelFormat == PixelFormats.Pbgra32) return 4;

        throw new ArgumentException("Unsupported pixel format");
    }

    /// <summary>
    /// 通道数转PixelFormat
    /// </summary>
    /// <param name="channels">通道数</param>
    /// <returns>PixelFormat</returns>
    /// <exception cref="ArgumentException">不支持格式异常</exception>
    public static PixelFormat GetPixelFormat(int channels)
    {
        return channels switch
        {
            1 => PixelFormats.Gray8,
            3 => PixelFormats.Bgr24,
            4 => PixelFormats.Bgra32,
            _ => throw new ArgumentException("Unsupported channels")
        };
    }

    #endregion Raw Bytes

    #region Memory Bytes

    /// <summary>
    /// bitmap memory byte array to BitmapSource
    /// </summary>
    /// <param name="bytes">bitmap memory byte array</param>
    /// <returns>BitmapSource</returns>
    public static BitmapSource BitmapMemoryByteArrayToBitmapSource(byte[] bytes)
    {
        using var ms = new MemoryStream();
        ms.Write(bytes, 0, bytes.Length); // bmp.Save(memoryStream, ImageFormat.Bmp); // bmp 转 byte[] 方法
        ms.Seek(0, SeekOrigin.Begin);

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.StreamSource = ms;
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        return bitmapImage;
    }

    /// <summary>
    /// image file path to a BitmapSource
    /// </summary>
    /// <param name="imageFilePath">image file path</param>
    /// <returns>BitmapSource</returns>
    public static BitmapSource BitmapFilePathToBitmapSource(string imageFilePath)
    {
        var readAllBytes = File.ReadAllBytes(imageFilePath);

        return BitmapMemoryByteArrayToBitmapSource(readAllBytes);
    }

    /// <summary>
    /// BitmapSource to a bitmap memory byte array
    /// </summary>
    /// <param name="bitmapSource">BitmapSource</param>
    /// <returns>bitmap memory byte array</returns>
    public static byte[] BitmapSourceToBitmapMemoryByteArray(BitmapSource bitmapSource)
    {
        using var ms = new MemoryStream();
        BitmapEncoder encoder = new BmpBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
        encoder.Save(ms);
        return ms.ToArray();
    }

    #endregion Memory Bytes
}