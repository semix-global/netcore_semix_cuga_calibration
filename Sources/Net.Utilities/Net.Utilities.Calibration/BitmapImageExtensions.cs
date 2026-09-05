using algocv_sharp;
using CommunityToolkit.Diagnostics;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Enums.Medias.Imaging;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models;
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

            return resultImage.ToBitmapImage(@this.ImageInfo.PixelFormatEnum.GetBitsPerPixel());
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
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<ImageDataType>(Constants.ImageFileExtensionsFilter)
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
}