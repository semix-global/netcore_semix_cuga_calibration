using algocv_sharp;
using AwesomeAssertions;
using Net.Utilities.Algorithms.AlgoCSharp;
using Net.Utilities.Graphics.Primitives.Enums.Medias.Imaging;
using Net.Utilities.Models.Geometries;

namespace CugaCalibrationUnitTest.AlgoCVSharp.Bridge;

/// <summary>
/// BitmapImageAlgoImageOperations 显示域行为（非双实现逐像素等价）.
/// </summary>
public class BitmapImageAlgoImageOperationsTest
{
    [Theory]
    [InlineData(12)]
    [InlineData(16)]
    public void DrawCrossLine_UInt16Gray_ShouldReturnBgr8888WithGreenCross(int bitsPerPixel)
    {
        using var source = CreateRampUInt16(9, 9);
        using var bitmap = source.ToBitmapImage(bitsPerPixel);
        using var drawn = BitmapImageAlgoImageOperations.DrawCrossLine(bitmap, new Point(4d, 4d));

        drawn.ImageInfo.PixelFormatEnum.Should().Be(PixelFormatEnum.Bgr8888);

        using var pixels = drawn.ToAlgoCVImage();
        pixels.Channels.Should().Be(4);
        pixels.DataType.Should().Be(ImageDataType.UInt8);
        AssertBgra(pixels, 4, 4, 0, 255, 0, 255);
        AssertBgra(pixels, 0, 4, 0, 255, 0, 255);
        AssertBgra(pixels, 4, 0, 0, 255, 0, 255);

        var cornerB = pixels.GetValue<byte>(0, 0, 0);
        var cornerG = pixels.GetValue<byte>(0, 0, 1);
        var cornerR = pixels.GetValue<byte>(0, 0, 2);
        cornerB.Should().Be(cornerG);
        cornerG.Should().Be(cornerR);
        pixels.GetValue<byte>(0, 0, 3).Should().Be(byte.MaxValue);
    }

    [Fact]
    public void DrawCrossLine_UInt8Gray_ShouldKeepBackgroundAndPaintGreenCross()
    {
        using var source = new algocv_sharp.Image(9, 9, 1, ImageDataType.UInt8);
        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                source.SetValue(x, y, (byte)40);
            }
        }

        using var bitmap = source.ToBitmapImage();
        using var drawn = BitmapImageAlgoImageOperations.DrawCrossLine(bitmap, new Point(4d, 4d));

        drawn.ImageInfo.PixelFormatEnum.Should().Be(PixelFormatEnum.Bgr8888);

        using var pixels = drawn.ToAlgoCVImage();
        AssertBgra(pixels, 4, 4, 0, 255, 0, 255);
        AssertBgra(pixels, 0, 0, 40, 40, 40, 255);
    }

    private static algocv_sharp.Image CreateRampUInt16(int width, int height)
    {
        var image = new algocv_sharp.Image(width, height, 1, ImageDataType.UInt16);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image.SetValue(x, y, (ushort)(y * width + x + 1));
            }
        }

        return image;
    }

    private static void AssertBgra(algocv_sharp.Image image, int x, int y, byte b, byte g, byte r, byte a)
    {
        image.GetValue<byte>(x, y, 0).Should().Be(b);
        image.GetValue<byte>(x, y, 1).Should().Be(g);
        image.GetValue<byte>(x, y, 2).Should().Be(r);
        image.GetValue<byte>(x, y, 3).Should().Be(a);
    }
}