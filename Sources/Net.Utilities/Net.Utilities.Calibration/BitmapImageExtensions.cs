using algocv_sharp;
using HalconDotNet;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Enums.Medias.Imaging;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;

namespace Net.Utilities.Calibration;

public static class BitmapImageExtensions
{
#pragma warning disable IDISP012

    public static BitmapImage Empty => BitmapImage.Random(1, 1, 0);

#pragma warning restore IDISP012

    extension(BitmapImage @this)
    {
        public void SaveImage(string filePath)
        {
            using var hImage = @this.ToHImage();

            hImage.Save(filePath);
        }

        public BitmapImage ToLinearImage()
        {
            using var hImage = @this.ToHImage();
            using var temp = RAWImageFactory.RAW12BitsPerPixelLogToLinear(hImage);

            return temp.ToBitmapImage(@this.ImageInfo.PixelFormatEnum.GetBitsPerPixel());
        }

        public BitmapImage RotateCounterClockwise90Degree()
        {
            using var hImage = @this.ToHImage();

            using var temp = hImage.RotateCounterClockwise90Degree();

            return temp.ToBitmapImage(@this.ImageInfo.PixelFormatEnum.GetBitsPerPixel());
        }

        public BitmapImage HorizontalFlip()
        {
            using var hImage = @this.ToHImage();

            using var temp = hImage.HorizontalFlip();

            return temp.ToBitmapImage(@this.ImageInfo.PixelFormatEnum.GetBitsPerPixel());
        }

        public BitmapImage VerticalFlip()
        {
            using var hImage = @this.ToHImage();

            using var temp = hImage.VerticalFlip();

            return temp.ToBitmapImage(@this.ImageInfo.PixelFormatEnum.GetBitsPerPixel());
        }


        public BitmapImage ToROI(Rect rect)
        {
            using var hImage = @this.ToHImage();

            using var temp = hImage.ToRoi(rect);

            return temp.ToBitmapImage(@this.ImageInfo.PixelFormatEnum.GetBitsPerPixel());
        }

        public (double Average, double Deviation) GetIntensity()
        {
            using var hImage = @this.ToHImage();

            return hImage.GetIntensity();
        }

        public BitmapImage SubImage(BitmapImage subImage)
        {
            using var hImage = @this.ToHImage();
            using var hSubImage = subImage.ToHImage();

            using var resultImage = hImage.SubImage(hSubImage, 1d, 0d);

            return resultImage.ToBitmapImage();
        }

        public BitmapImage ToRoi(Rect rect)
        {
            using var hImage = @this.ToHImage();

            return hImage.CropPart((HTuple)rect.Y, (HTuple)rect.X, (HTuple)rect.Width, (HTuple)rect.Height).ToBitmapImage();
        }

        public double[] GetHorizontalProjects()
        {
            using var hImage = @this.ToHImage();

            return hImage.GetHorizontalProjects();
        }

        public (double MaxGrayValue, Point[] maxGrayPoints, double MinGrayValue, Point[] minGrayPoints) GetMaxMinGrayValue(Rect rect)
        {
            using var hImage = @this.ToHImage();

            return hImage.GetMaxMinGrayValue(rect);
        }

        public algocv_sharp.Image ToAlgoCVImage()
        {
            if (@this.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(@this));
            }

            if (@this.IsEmpty)
            {
                throw new ArgumentException("Cannot convert an empty BitmapImage.", nameof(@this));
            }

            var (channels, dataType, bitsPerPixel) = @this.PixelFormatEnum switch
            {
                PixelFormatEnum.Gray8 => (1, ImageDataType.UInt8, 8),
                PixelFormatEnum.Gray12 => (1, ImageDataType.UInt16, 16),
                PixelFormatEnum.Gray16 => (1, ImageDataType.UInt16, 16),
                PixelFormatEnum.Bgr8888 => (3, ImageDataType.UInt8, 24),
                PixelFormatEnum.Bgra8888 => (4, ImageDataType.UInt8, 32),
                _ => throw new NotSupportedException($"Pixel format '{@this.PixelFormatEnum}' is not supported for algocv_sharp.Image conversion.")
            };

            var width = @this.Width;
            var height = @this.Height;
            var image = new algocv_sharp.Image(width, height, channels, dataType);

            var destImageInfo = ImageInfoFactory.Create(width, height, channels, bitsPerPixel);
            if (!@this.ReadPixels(destImageInfo, image.DataPtr, image.Stride))
            {
                image.Dispose();
                throw new InvalidOperationException("Failed to read pixels from BitmapImage into algocv_sharp.Image.");
            }

            return image;
        }
    }
}