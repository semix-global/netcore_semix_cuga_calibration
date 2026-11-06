using algocv_sharp;
using AwesomeAssertions;

namespace CugaCalibrationUnitTest.AlgoCVSharp.ImageProc;

// 命名空间末段与 algocv_sharp.ImageProc 类同名, 直接引用会被解析为嵌套命名空间; 用别名显式绑定到类.
using ImageProc = algocv_sharp.ImageProc;

/// <summary>
/// 1.4.1 新增 ImageProc 几何/颜色/绘制接口. 合成图为行优先, 输出须保持行优先.
/// </summary>
public class ImageProcGeometryTest
{
    [Fact]
    public void Rotate_Clockwise90_ShouldSwapSizeAndMovePixels()
    {
        using var source = CreateUniqueUInt8(width: 2, height: 3);
        using var rotated = ImageProc.Rotate(source, RotateAngle.Rotate90Clockwise);

        rotated.Width.Should().Be(source.Height);
        rotated.Height.Should().Be(source.Width);
        rotated.Channels.Should().Be(source.Channels);
        rotated.DataType.Should().Be(source.DataType);

        for (var y = 0; y < rotated.Height; y++)
        {
            for (var x = 0; x < rotated.Width; x++)
            {
                var expected = source.GetValue<byte>(y, source.Height - 1 - x);
                rotated.GetValue<byte>(x, y).Should().Be(expected);
            }
        }
    }

    [Fact]
    public void Rotate_CounterClockwise90_ShouldSwapSizeAndMovePixels()
    {
        using var source = CreateUniqueUInt8(width: 2, height: 3);
        using var rotated = ImageProc.Rotate(source, RotateAngle.Rotate90CounterClockwise);

        rotated.Width.Should().Be(source.Height);
        rotated.Height.Should().Be(source.Width);

        for (var y = 0; y < rotated.Height; y++)
        {
            for (var x = 0; x < rotated.Width; x++)
            {
                var expected = source.GetValue<byte>(source.Width - 1 - y, x);
                rotated.GetValue<byte>(x, y).Should().Be(expected);
            }
        }
    }

    [Fact]
    public void Rotate_180_ShouldKeepSizeAndReversePixels()
    {
        using var source = CreateUniqueUInt8(width: 2, height: 3);
        using var rotated = ImageProc.Rotate(source, RotateAngle.Rotate180);

        rotated.Width.Should().Be(source.Width);
        rotated.Height.Should().Be(source.Height);

        for (var y = 0; y < rotated.Height; y++)
        {
            for (var x = 0; x < rotated.Width; x++)
            {
                var expected = source.GetValue<byte>(source.Width - 1 - x, source.Height - 1 - y);
                rotated.GetValue<byte>(x, y).Should().Be(expected);
            }
        }
    }

    [Fact]
    public void Mirror_Horizontal_ShouldFlipColumns()
    {
        using var source = CreateUniqueUInt8(width: 2, height: 3);
        using var mirrored = ImageProc.Mirror(source, MirrorMode.Horizontal);

        AssertSameLayout(source, mirrored);

        for (var y = 0; y < mirrored.Height; y++)
        {
            for (var x = 0; x < mirrored.Width; x++)
            {
                var expected = source.GetValue<byte>(source.Width - 1 - x, y);
                mirrored.GetValue<byte>(x, y).Should().Be(expected);
            }
        }
    }

    [Fact]
    public void Mirror_Vertical_ShouldFlipRows()
    {
        using var source = CreateUniqueUInt8(width: 2, height: 3);
        using var mirrored = ImageProc.Mirror(source, MirrorMode.Vertical);

        AssertSameLayout(source, mirrored);

        for (var y = 0; y < mirrored.Height; y++)
        {
            for (var x = 0; x < mirrored.Width; x++)
            {
                var expected = source.GetValue<byte>(x, source.Height - 1 - y);
                mirrored.GetValue<byte>(x, y).Should().Be(expected);
            }
        }
    }

    [Fact]
    public void Mirror_Both_ShouldEqualRotate180()
    {
        using var source = CreateUniqueUInt8(width: 3, height: 4);
        using var mirrored = ImageProc.Mirror(source, MirrorMode.Both);
        using var rotated = ImageProc.Rotate(source, RotateAngle.Rotate180);

        AssertSameLayout(rotated, mirrored);

        for (var y = 0; y < mirrored.Height; y++)
        {
            for (var x = 0; x < mirrored.Width; x++)
            {
                mirrored.GetValue<byte>(x, y).Should().Be(rotated.GetValue<byte>(x, y));
            }
        }
    }

    [Fact]
    public void GeometryApis_Float16_WhenEnumExists_ShouldNotBeSupported()
    {
        // 1.4.1 xml 未列出 ImageDataType.Float16; 若运行时枚举存在则断言 Rotate/Mirror 不支持.
        if (Enum.TryParse("Float16", ignoreCase: false, out ImageDataType float16) == false)
            return;

        using var image = new algocv_sharp.Image(2, 2, 1, float16);
        var rotate = () => ImageProc.Rotate(image, RotateAngle.Rotate90Clockwise);
        var mirror = () => ImageProc.Mirror(image, MirrorMode.Horizontal);
        rotate.Should().Throw<Exception>();
        mirror.Should().Throw<Exception>();
    }

    [Fact]
    public void BgrToGray_SingleChannel_ShouldClonePixels()
    {
        using var source = CreateUniqueUInt8(width: 3, height: 2);
        using var gray = ImageProc.BgrToGray(source);

        gray.Channels.Should().Be(1);
        gray.DataType.Should().Be(source.DataType);
        AssertSamePixels(source, gray);
    }

    [Fact]
    public void BgrToGray_Bgr_ShouldUseOpenCvLumaWeights()
    {
        using var source = new algocv_sharp.Image(2, 2, 3, ImageDataType.UInt8);
        source.SetValue(1, 1, 10, 0);
        source.SetValue(1, 1, 20, 1);
        source.SetValue(1, 1, 30, 2);

        using var gray = ImageProc.BgrToGray(source);

        gray.Channels.Should().Be(1);
        gray.DataType.Should().Be(ImageDataType.UInt8);
        var expected = (byte)Math.Round(0.114d * 10 + 0.587d * 20 + 0.299d * 30);
        Math.Abs(gray.GetValue<byte>(1, 1) - expected).Should().BeLessThanOrEqualTo(1);
    }

    [Fact]
    public void BgrToGray_Bgra_ShouldIgnoreAlpha()
    {
        using var source = new algocv_sharp.Image(1, 1, 4, ImageDataType.UInt8);
        source.SetValue(0, 0, 10, 0);
        source.SetValue(0, 0, 20, 1);
        source.SetValue(0, 0, 30, 2);
        source.SetValue(0, 0, 255, 3);

        using var gray = ImageProc.BgrToGray(source);

        gray.Channels.Should().Be(1);
        var expected = (byte)Math.Round(0.114d * 10 + 0.587d * 20 + 0.299d * 30);
        Math.Abs(gray.GetValue<byte>(0, 0) - expected).Should().BeLessThanOrEqualTo(1);
    }

    [Fact]
    public void GrayToBgr_SingleChannel_ShouldReplicateChannels()
    {
        using var source = new algocv_sharp.Image(2, 2, 1, ImageDataType.UInt8);
        source.SetValue(0, 1, (byte)40);

        using var bgr = ImageProc.GrayToBgr(source);

        bgr.Channels.Should().Be(3);
        bgr.DataType.Should().Be(source.DataType);
        bgr.GetValue<byte>(0, 1, 0).Should().Be(40);
        bgr.GetValue<byte>(0, 1, 1).Should().Be(40);
        bgr.GetValue<byte>(0, 1, 2).Should().Be(40);
    }

    [Fact]
    public void GrayToBgr_ThreeChannel_ShouldClone()
    {
        using var source = new algocv_sharp.Image(1, 1, 3, ImageDataType.UInt8);
        source.SetValue(0, 0, 1, 0);
        source.SetValue(0, 0, 2, 1);
        source.SetValue(0, 0, 3, 2);

        using var cloned = ImageProc.GrayToBgr(source);

        cloned.Channels.Should().Be(3);
        cloned.GetValue<byte>(0, 0, 0).Should().Be(1);
        cloned.GetValue<byte>(0, 0, 1).Should().Be(2);
        cloned.GetValue<byte>(0, 0, 2).Should().Be(3);
    }

    [Fact]
    public void Subtract16S_UInt16_ShouldKeepPositiveDifferenceAsInt16()
    {
        using var minuend = new algocv_sharp.Image(1, 1, 1, ImageDataType.UInt16);
        using var subtrahend = new algocv_sharp.Image(1, 1, 1, ImageDataType.UInt16);
        minuend.SetValue(0, 0, (ushort)40000);
        subtrahend.SetValue(0, 0, (ushort)10000);

        using var result = ImageProc.Subtract16S(minuend, subtrahend);

        result.DataType.Should().Be(ImageDataType.Int16);
        result.Channels.Should().Be(1);
        result.GetValue<short>(0, 0).Should().Be(30000);
    }

    [Fact]
    public void Subtract16S_UInt16_ShouldSaturateToInt16Max()
    {
        using var minuend = new algocv_sharp.Image(1, 1, 1, ImageDataType.UInt16);
        using var subtrahend = new algocv_sharp.Image(1, 1, 1, ImageDataType.UInt16);
        minuend.SetValue(0, 0, ushort.MaxValue);
        subtrahend.SetValue(0, 0, (ushort)0);

        using var result = ImageProc.Subtract16S(minuend, subtrahend);

        result.GetValue<short>(0, 0).Should().Be(short.MaxValue);
    }

    [Fact]
    public void Subtract16S_Int16_ShouldKeepNegativeDifference()
    {
        using var minuend = new algocv_sharp.Image(1, 1, 1, ImageDataType.Int16);
        using var subtrahend = new algocv_sharp.Image(1, 1, 1, ImageDataType.Int16);
        minuend.SetValue(0, 0, (short)100);
        subtrahend.SetValue(0, 0, (short)200);

        using var result = ImageProc.Subtract16S(minuend, subtrahend);

        result.DataType.Should().Be(ImageDataType.Int16);
        result.GetValue<short>(0, 0).Should().Be(-100);
    }

    [Fact]
    public void Subtract16S_SizeMismatch_ShouldThrow()
    {
        using var minuend = new algocv_sharp.Image(2, 1, 1, ImageDataType.UInt16);
        using var subtrahend = new algocv_sharp.Image(1, 1, 1, ImageDataType.UInt16);

        var act = () => ImageProc.Subtract16S(minuend, subtrahend);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void DrawCross_GrayInput_ShouldOutputBgrAndPaintArms()
    {
        using var source = new algocv_sharp.Image(9, 9, 1, ImageDataType.UInt8);
        using var drawn = ImageProc.DrawCross(source, 4, 4, 2, 1, [0d, 0d, 255d]);

        drawn.Channels.Should().Be(3);
        AssertBgr(drawn, 4, 4, 0, 0, 255);
        AssertBgr(drawn, 2, 4, 0, 0, 255);
        AssertBgr(drawn, 6, 4, 0, 0, 255);
        AssertBgr(drawn, 4, 2, 0, 0, 255);
        AssertBgr(drawn, 4, 6, 0, 0, 255);
        AssertBgr(drawn, 0, 0, 0, 0, 0);
    }

    [Fact]
    public void DrawRectangle_Outline_ShouldPaintBorderNotInterior()
    {
        using var source = new algocv_sharp.Image(10, 10, 1, ImageDataType.UInt8);
        using var drawn = ImageProc.DrawRectangle(source, 2, 2, 4, 3, 1, [0d, 255d, 0d]);

        drawn.Channels.Should().Be(3);
        AssertBgr(drawn, 2, 2, 0, 255, 0);
        AssertBgr(drawn, 5, 2, 0, 255, 0);
        AssertBgr(drawn, 2, 4, 0, 255, 0);
        AssertBgr(drawn, 5, 4, 0, 255, 0);
        AssertBgr(drawn, 3, 3, 0, 0, 0);
    }

    [Fact]
    public void DrawRectangle_NegativeThickness_ShouldFill()
    {
        using var source = new algocv_sharp.Image(10, 10, 1, ImageDataType.UInt8);
        using var drawn = ImageProc.DrawRectangle(source, 2, 2, 4, 3, -1, [0d, 255d, 0d]);

        drawn.Channels.Should().Be(3);
        AssertBgr(drawn, 3, 3, 0, 255, 0);
    }

    private static algocv_sharp.Image CreateUniqueUInt8(int width, int height)
    {
        var image = new algocv_sharp.Image(width, height, 1, ImageDataType.UInt8);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image.SetValue(x, y, (byte)(y * width + x + 1));
            }
        }

        return image;
    }

    private static void AssertSameLayout(algocv_sharp.Image expected, algocv_sharp.Image actual)
    {
        actual.Width.Should().Be(expected.Width);
        actual.Height.Should().Be(expected.Height);
        actual.Channels.Should().Be(expected.Channels);
        actual.DataType.Should().Be(expected.DataType);
    }

    private static void AssertSamePixels(algocv_sharp.Image expected, algocv_sharp.Image actual)
    {
        AssertSameLayout(expected, actual);
        for (var y = 0; y < actual.Height; y++)
        {
            for (var x = 0; x < actual.Width; x++)
            {
                actual.GetValue<byte>(x, y).Should().Be(expected.GetValue<byte>(x, y));
            }
        }
    }

    private static void AssertBgr(algocv_sharp.Image image, int x, int y, byte b, byte g, byte r)
    {
        image.GetValue<byte>(x, y, 0).Should().Be(b);
        image.GetValue<byte>(x, y, 1).Should().Be(g);
        image.GetValue<byte>(x, y, 2).Should().Be(r);
    }
}