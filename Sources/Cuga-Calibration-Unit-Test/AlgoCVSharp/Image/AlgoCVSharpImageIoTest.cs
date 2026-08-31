using algocv_sharp;
using AwesomeAssertions;
using Net.Utilities.Helpers.Helpers.Files;
using System.IO;

namespace CugaCalibrationUnitTest.AlgoCVSharp.Image;

public class AlgoCVSharpImageIoTest
{
    [Fact]
    public void SaveAndLoad_UInt8Grayscale_ShouldPreservePixels()
    {
        const int width = 8;
        const int height = 6;
        var originalPath = Path.Combine(Path.GetTempPath(), $"{nameof(AlgoCVSharpImageIoTest)}_uint8.bmp");
        FileHelper.DeleteFileIfExists(originalPath);

        try
        {
            using (var original = new algocv_sharp.Image(width, height, 1, ImageDataType.UInt8))
            {
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        original.SetValue(x, y, (byte)((x + y) % 256), 0);
                    }
                }

                original.Save(originalPath);
            }

            using var loaded = new algocv_sharp.Image(originalPath);
            loaded.Width.Should().Be(width);
            loaded.Height.Should().Be(height);

            // BMP load may convert a single-channel image into a BGR image.
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var expected = (byte)((x + y) % 256);
                    for (var c = 0; c < loaded.Channels; c++)
                    {
                        loaded.GetValue<byte>(x, y, c).Should().Be(expected);
                    }
                }
            }
        }
        finally
        {
            FileHelper.DeleteFileIfExists(originalPath);
        }
    }

    [Fact]
    public void SaveAndLoad_MultiChannel_ShouldPreservePixels()
    {
        const int width = 5;
        const int height = 4;
        const int channels = 3;
        var originalPath = Path.Combine(Path.GetTempPath(), $"{nameof(AlgoCVSharpImageIoTest)}_rgb.bmp");
        FileHelper.DeleteFileIfExists(originalPath);

        try
        {
            using (var original = new algocv_sharp.Image(width, height, channels, ImageDataType.UInt8))
            {
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        original.SetValue(x, y, (byte)(x + 1), 0);
                        original.SetValue(x, y, (byte)(y + 1), 1);
                        original.SetValue(x, y, (byte)((x + y) % 256), 2);
                    }
                }

                original.Save(originalPath);
            }

            using var loaded = new algocv_sharp.Image(originalPath);
            loaded.Width.Should().Be(width);
            loaded.Height.Should().Be(height);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    loaded.GetValue<byte>(x, y, 0).Should().Be((byte)(x + 1));
                    loaded.GetValue<byte>(x, y, 1).Should().Be((byte)(y + 1));
                    loaded.GetValue<byte>(x, y, 2).Should().Be((byte)((x + y) % 256));
                }
            }
        }
        finally
        {
            FileHelper.DeleteFileIfExists(originalPath);
        }
    }

    [Fact]
    public void Constructor_NonExistentFile_ShouldThrow()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.raw");

        var act = () =>
        {
            using var _ = new algocv_sharp.Image(missingPath);
        };

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Load_InvalidFile_ShouldThrow()
    {
        var invalidPath = Path.Combine(Path.GetTempPath(), $"{nameof(AlgoCVSharpImageIoTest)}_invalid.raw");
        File.WriteAllText(invalidPath, "this is not an image");

        try
        {
            var act = () =>
            {
                using var _ = new algocv_sharp.Image(invalidPath);
            };
            act.Should().Throw<Exception>();
        }
        finally
        {
            FileHelper.DeleteFileIfExists(invalidPath);
        }
    }
}
