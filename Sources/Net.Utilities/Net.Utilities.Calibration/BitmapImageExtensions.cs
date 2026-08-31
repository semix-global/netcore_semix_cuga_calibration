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

    extension(BitmapImage bitmapImage)
    {
        public void SaveImage(string filePath)
        {
            using var hImage = bitmapImage.ToHImage();
            hImage.Save(filePath);
        }

        public BitmapImage ToLinearImage()
        {
            using var hImage = bitmapImage.ToHImage();
            using var temp = RAWImageFactory.RAW12BitsPerPixelLogToLinear(hImage);

            return temp.ToBitmapImage(bitmapImage.ImageInfo.PixelFormatEnum.GetBitsPerPixel());
        }

        public BitmapImage RotateCounterClockwise90Degree()
        {
            using var hImage = bitmapImage.ToHImage();

            using var temp = hImage.RotateCounterClockwise90Degree();

            return temp.ToBitmapImage(bitmapImage.ImageInfo.PixelFormatEnum.GetBitsPerPixel());
        }

        public BitmapImage VerticalFlip()
        {
            using var hImage = bitmapImage.ToHImage();

            using var temp = hImage.VerticalFlip();

            return temp.ToBitmapImage(bitmapImage.ImageInfo.PixelFormatEnum.GetBitsPerPixel());
        }

        public (double Average, double Deviation) GetIntensity()
        {
            using var hImage = bitmapImage.ToHImage();

            return hImage.GetIntensity();
        }

        public BitmapImage SubImage(BitmapImage subImage)
        {
            using var hImage = bitmapImage.ToHImage();
            using var hSubImage = subImage.ToHImage();

            using var resultImage = hImage.SubImage(hSubImage, 1d, 0d);

            using var resultHImage = new HImage(resultImage);

            return resultHImage.ToBitmapImage();
        }

        public BitmapImage ToRoi(Rect rect)
        {
            using var hImage = bitmapImage.ToHImage();

            return hImage.CropPart((HTuple)rect.Y, (HTuple)rect.X, (HTuple)rect.Width, (HTuple)rect.Height).ToBitmapImage();
        }

        public double[] GetHorizontalProjects()
        {
            using var hImage = bitmapImage.ToHImage();

            return hImage.GetHorizontalProjects();
        }

        public (double MaxGrayValue, Point[] maxGrayPoints, double MinGrayValue, Point[] minGrayPoints) GetMaxMinGrayValue(Rect rect)
        {
            using var hImage = bitmapImage.ToHImage();

            return hImage.GetMaxMinGrayValue(rect);
        }

        public algocv_sharp.Image ToAlgoCVImage()
        {
            if (bitmapImage.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(bitmapImage));
            }

            if (bitmapImage.IsEmpty)
            {
                throw new ArgumentException("Cannot convert an empty BitmapImage.", nameof(bitmapImage));
            }

            var (channels, dataType, bitsPerPixel) = bitmapImage.PixelFormatEnum switch
            {
                PixelFormatEnum.Gray8 => (1, ImageDataType.UInt8, 8),
                PixelFormatEnum.Gray12 => (1, ImageDataType.UInt16, 16),
                PixelFormatEnum.Gray16 => (1, ImageDataType.UInt16, 16),
                PixelFormatEnum.Bgr8888 => (3, ImageDataType.UInt8, 24),
                PixelFormatEnum.Bgra8888 => (4, ImageDataType.UInt8, 32),
                _ => throw new NotSupportedException($"Pixel format '{bitmapImage.PixelFormatEnum}' is not supported for algocv_sharp.Image conversion.")
            };

            var width = bitmapImage.Width;
            var height = bitmapImage.Height;
            var image = new algocv_sharp.Image(width, height, channels, dataType);

            var destImageInfo = ImageInfoFactory.Create(width, height, channels, bitsPerPixel);
            if (!bitmapImage.ReadPixels(destImageInfo, image.DataPtr, image.Stride, 0, 0))
            {
                image.Dispose();
                throw new InvalidOperationException("Failed to read pixels from BitmapImage into algocv_sharp.Image.");
            }

            return image;
        }
    }
}