using algocv_sharp;
using CommunityToolkit.Diagnostics;
using Net.Utilities.Graphics.Primitives.Enums.Medias.Imaging;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models;

namespace Net.Utilities.Algorithms.AlgoCSharp;

/// <summary>
/// AlgoImage ⇄ BitmapImage 桥（algo 侧唯一转换入口，随本包对引用方可见）。
/// <para>方向契约：HImage 与 AlgoImage 禁止直接转换，跨库图像流必须经 BitmapImage 枢纽。</para>
/// </summary>
public static class BitmapImageExtensions
{
    extension(BitmapImage @this)
    {
        /// <summary>
        /// 转为 algocv 图像，不做转置，图像方向由算法接口的 XAxis 参数决定.
        /// </summary>
        public Image ToAlgoCVImage()
        {
            Guard.IsFalse(@this.IsDisposed);
            Guard.IsFalse(@this.IsEmpty);

            var dataType = @this.PixelFormatEnum switch
            {
                PixelFormatEnum.Gray8 => ImageDataType.UInt8,
                PixelFormatEnum.Gray12 or PixelFormatEnum.Gray16 => ImageDataType.UInt16,
                PixelFormatEnum.Bgr8888 => ImageDataType.UInt8,
                PixelFormatEnum.Bgra8888 => ImageDataType.UInt8,
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<ImageDataType>(nameof(@this.PixelFormatEnum), Constants.ImageFileExtensionsFilter)
            };

            var image = new Image(@this.Width, @this.Height, @this.ImageInfo.Channels, dataType);
            if (@this.ReadPixels(@this.ImageInfo, image.DataPtr, image.Stride) == false)
            {
                image.Dispose();

                return ThrowHelper.ThrowNotSupportedException<Image>(Constants.ImageFileExtensionsFilter);
            }

            return image;
        }
    }

    extension(Image @this)
    {
        /// <summary>
        /// 转为 <see cref="BitmapImage"/>，按通道与数据类型自动推导位深.
        /// </summary>
        public BitmapImage ToBitmapImage()
        {
            Guard.IsFalse(@this.IsEmpty);
            Guard.IsFalse(@this.DataPtr == IntPtr.Zero);

            var (channels, bitsPerPixel) = (@this.Channels, @this.DataType) switch
            {
                (1, ImageDataType.UInt8) => (1, 8),
                (1, ImageDataType.UInt16) => (1, 16),
                (3, ImageDataType.UInt8) => (3, 24),
                (4, ImageDataType.UInt8) => (4, 32),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<(int Channels, int BitsPerPixel)>(nameof(@this.DataType), Constants.UnsupportedImageFormat)
            };

            var imageInfo = ImageInfoFactory.Create(@this.Width, @this.Height, channels, bitsPerPixel);
            if (channels is 1 or 4)
            {
                return new BitmapImage(imageInfo, @this.DataPtr, @this.Stride);
            }

            return CopyBgrToBgra(@this, imageInfo);
        }

        /// <summary>
        /// 转为 <see cref="BitmapImage"/> 并显式指定每像素位深（仅单通道 UInt16 支持：12bit = log 域标记，16bit = 线性域标记）。
        /// </summary>
        /// <param name="bitsPerPixel">每像素位深，仅允许 12 或 16</param>
        /// <returns><see cref="BitmapImage"/></returns>
        public BitmapImage ToBitmapImage(int bitsPerPixel)
        {
            Guard.IsTrue(@this.DataType == ImageDataType.UInt16 && @this.Channels == 1 && bitsPerPixel is 12 or 16, Constants.UnsupportedImageFormat);

            var imageInfo = ImageInfoFactory.Create(@this.Width, @this.Height, 1, bitsPerPixel);

            return new BitmapImage(imageInfo, @this.DataPtr, @this.Stride);
        }
    }

    /// <summary>
    /// 3 通道 BGR 逐像素补 alpha 转为 4 通道 BGRA（BitmapImage 仅支持 1/4 通道直通构造）.
    /// </summary>
    private static BitmapImage CopyBgrToBgra(Image source, ImageInfo imageInfo)
    {
        var destRowBytes = imageInfo.RowBytes;
        var srcStride = source.Stride;
        var srcPtr = source.DataPtr;
        var buffer = new byte[checked(source.Height * destRowBytes)];
        unsafe
        {
            for (var y = 0; y < source.Height; y++)
            {
                var src = new Span<byte>((byte*)srcPtr + y * srcStride, srcStride);
                var dstOffset = y * destRowBytes;
                for (var x = 0; x < source.Width; x++)
                {
                    var si = x * 3;
                    var di = dstOffset + x * 4;
                    buffer[di] = src[si];
                    buffer[di + 1] = src[si + 1];
                    buffer[di + 2] = src[si + 2];
                    buffer[di + 3] = byte.MaxValue;
                }
            }

            fixed (byte* ptr = buffer)
            {
                return new BitmapImage(imageInfo, (IntPtr)ptr, destRowBytes);
            }
        }
    }
}
