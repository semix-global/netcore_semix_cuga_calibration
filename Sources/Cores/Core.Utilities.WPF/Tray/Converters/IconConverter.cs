using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Core.Utilities.WPF.Tray.Converters;

internal static class IconConverter
{
    public static Icon? ToIcon(ImageSource? imageSource)
    {
        if (imageSource is not BitmapImage bitmapImage || bitmapImage.UriSource == null)
        {
            return null;
        }

        using var stream = GetIconStream(bitmapImage);
        if (stream == null)
        {
            return null;
        }

        return new Icon(stream);
    }

    private static Stream? GetIconStream(BitmapImage bitmapImage)
    {
        if (bitmapImage.UriSource != null)
        {
            try
            {
                var streamInfo = Application.GetResourceStream(bitmapImage.UriSource);
                if (streamInfo != null)
                {
                    return streamInfo.Stream;
                }

                if (bitmapImage.UriSource.IsFile)
                {
                    return new FileStream(bitmapImage.UriSource.LocalPath, FileMode.Open, FileAccess.Read);
                }
            }
            catch
            {
                // Fall through to return null.
            }
        }

        return null;
    }
}
