using algocv_sharp;
using AwesomeAssertions;

namespace CugaCalibrationUnitTest.AlgoCVSharp.Image;

public class AlgoCVSharpImagePixelTest
{
    [Fact]
    public void SetValue_GetValue_UInt8_ShouldBeConsistent()
    {
        const int width = 16;
        const int height = 8;
        using var image = new algocv_sharp.Image(width, height, 1, ImageDataType.UInt8);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var value = (byte)((x + y) % 256);
                image.SetValue(x, y, value);
            }
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var expected = (byte)((x + y) % 256);
                image.GetValue<byte>(x, y).Should().Be(expected);
            }
        }
    }

    [Fact]
    public void SetValue_GetValue_UInt16_ShouldBeConsistent()
    {
        const int width = 10;
        const int height = 6;
        using var image = new algocv_sharp.Image(width, height);

        ushort value = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image.SetValue(x, y, value);
                value++;
            }
        }

        value = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image.GetValue<ushort>(x, y).Should().Be(value);
                value++;
            }
        }
    }

    [Fact]
    public void SetValue_GetValue_MultiChannel_ShouldBeIndependent()
    {
        const int width = 6;
        const int height = 4;
        const int channels = 3;
        using var image = new algocv_sharp.Image(width, height, channels, ImageDataType.UInt8);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image.SetValue(x, y, (byte)(x + 1));
                image.SetValue(x, y, (byte)(y + 1), 1);
                image.SetValue(x, y, (byte)((x + y) % 256), 2);
            }
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image.GetValue<byte>(x, y).Should().Be((byte)(x + 1));
                image.GetValue<byte>(x, y, 1).Should().Be((byte)(y + 1));
                image.GetValue<byte>(x, y, 2).Should().Be((byte)((x + y) % 256));
            }
        }
    }

    [Fact]
    public void GetRow_ShouldMatchPerPixelValues()
    {
        const int width = 8;
        const int height = 5;
        using var image = new algocv_sharp.Image(width, height, 1, ImageDataType.UInt8);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image.SetValue(x, y, (byte)(y * width + x));
            }
        }

        for (var y = 0; y < height; y++)
        {
            var row = image.GetRow(y);
            row.Length.Should().BeGreaterThanOrEqualTo(width);

            for (var x = 0; x < width; x++)
            {
                row[x].Should().Be(image.GetValue<byte>(x, y));
            }
        }
    }

    [Fact]
    public void GetRow_Generic_ShouldMatchPerPixelValues()
    {
        const int width = 6;
        const int height = 4;
        using var image = new algocv_sharp.Image(width, height);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image.SetValue(x, y, (ushort)(y * width + x));
            }
        }

        for (var y = 0; y < height; y++)
        {
            var row = image.GetRow<ushort>(y);
            row.Length.Should().BeGreaterThanOrEqualTo(width);

            for (var x = 0; x < width; x++)
            {
                row[x].Should().Be(image.GetValue<ushort>(x, y));
            }
        }
    }

    [Fact]
    public void ToSpan_ShouldMatchPerPixelValues()
    {
        const int width = 5;
        const int height = 3;
        const int channels = 3;
        using var image = new algocv_sharp.Image(width, height, channels, ImageDataType.UInt8);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                for (var c = 0; c < channels; c++)
                {
                    image.SetValue(x, y, (byte)(c + 1), c);
                }
            }
        }

        var span = image.ToSpan();
        span.Length.Should().Be(image.Stride * image.Height);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                for (var c = 0; c < channels; c++)
                {
                    var index = y * image.Stride + x * channels + c;
                    span[index].Should().Be((byte)(c + 1));
                }
            }
        }
    }

    [Fact]
    public void ToSpan_Generic_UInt16_ShouldMatchPerPixelValues()
    {
        const int width = 4;
        const int height = 3;
        using var image = new algocv_sharp.Image(width, height);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image.SetValue(x, y, (ushort)(y * width + x));
            }
        }

        var span = image.ToSpan<ushort>();
        span.Length.Should().Be(image.Stride / sizeof(ushort) * image.Height);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = y * (image.Stride / sizeof(ushort)) + x;
                span[index].Should().Be((ushort)(y * width + x));
            }
        }
    }

    [Fact]
    public void Indexer_GetRowPointer_ShouldMatchDataPtrAndStride()
    {
        const int width = 6;
        const int height = 4;
        const int channels = 3;
        using var image = new algocv_sharp.Image(width, height, channels, ImageDataType.UInt8);

        for (var y = 0; y < height; y++)
        {
            var expected = image.DataPtr + y * image.Stride;
            image[y].Should().Be(expected);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void GetValue_InvalidChannel_ShouldThrowArgumentOutOfRangeException(int invalidChannel)
    {
        using var image = new algocv_sharp.Image(4, 4, 1, ImageDataType.UInt8);

        var act = () => image.GetValue<byte>(0, 0, invalidChannel);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void SetValue_InvalidChannel_ShouldThrowArgumentOutOfRangeException(int invalidChannel)
    {
        using var image = new algocv_sharp.Image(4, 4, 1, ImageDataType.UInt8);

        var act = () => image.SetValue(0, 0, (byte)42, invalidChannel);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void BoundaryPixels_ShouldBeAccessible()
    {
        const int width = 7;
        const int height = 5;
        using var image = new algocv_sharp.Image(width, height, 1, ImageDataType.UInt8);

        image.SetValue(0, 0, 11);
        image.SetValue(width - 1, height - 1, 22);

        image.GetValue<byte>(0, 0).Should().Be(11);
        image.GetValue<byte>(width - 1, height - 1).Should().Be(22);
    }
}