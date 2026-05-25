using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;

namespace Core.Utilities;

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

        public BitmapImage RotateCounterClockwise90DegreeAndVerticalFlip()
        {
            using var hImage = bitmapImage.ToHImage();

            using var temp1 = hImage.RotateCounterClockwise90Degree();
            using var temp2 = temp1.VerticalFlip();

            return temp2.ToBitmapImage(bitmapImage.ImageInfo.PixelFormatEnum.GetBitsPerPixel());
        }

        public (double Average, double Deviation) GetIntensity()
        {
            using var hImage = bitmapImage.ToHImage();

            return hImage.GetIntensity();
        }
    }
}