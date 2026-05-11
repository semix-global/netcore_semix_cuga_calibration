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
    }
}