using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;

namespace Core.Utilities;

public static class BitmapImageExtensions
{
    extension(BitmapImage bitmapImage)
    {
        public void SaveImage(string filePath)
        {
            using var hImage = bitmapImage.ToHImage();
            hImage.Save(filePath);
        }
    }
}