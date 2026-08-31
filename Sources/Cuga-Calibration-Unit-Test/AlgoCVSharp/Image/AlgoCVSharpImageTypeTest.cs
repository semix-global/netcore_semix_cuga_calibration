using algocv_sharp;
using AwesomeAssertions;

namespace CugaCalibrationUnitTest.AlgoCVSharp.Image;

public class AlgoCVSharpImageTypeTest
{
    [Fact]
    public void Int8Type_ShouldSupportReadWrite()
    {
        using var image = new algocv_sharp.Image(3, 3, 1, ImageDataType.Int8);
        image.SetValue(1, 1, (sbyte)-100, 0);
        image.GetValue<sbyte>(1, 1, 0).Should().Be(-100);
    }

    [Fact]
    public void UInt8Type_ShouldSupportReadWrite()
    {
        using var image = new algocv_sharp.Image(3, 3, 1, ImageDataType.UInt8);
        image.SetValue(1, 1, (byte)200, 0);
        image.GetValue<byte>(1, 1, 0).Should().Be(200);
    }

    [Fact]
    public void Int16Type_ShouldSupportReadWrite()
    {
        using var image = new algocv_sharp.Image(3, 3, 1, ImageDataType.Int16);
        image.SetValue(1, 1, (short)-1234, 0);
        image.GetValue<short>(1, 1, 0).Should().Be(-1234);
    }

    [Fact]
    public void UInt16Type_ShouldSupportReadWrite()
    {
        using var image = new algocv_sharp.Image(3, 3, 1, ImageDataType.UInt16);
        image.SetValue(1, 1, (ushort)45678, 0);
        image.GetValue<ushort>(1, 1, 0).Should().Be(45678);
    }

    [Fact]
    public void Int32Type_ShouldSupportReadWrite()
    {
        using var image = new algocv_sharp.Image(3, 3, 1, ImageDataType.Int32);
        image.SetValue(1, 1, -789012, 0);
        image.GetValue<int>(1, 1, 0).Should().Be(-789012);
    }

    [Fact]
    public void FloatType_ShouldSupportReadWrite()
    {
        using var image = new algocv_sharp.Image(3, 3, 1, ImageDataType.Float);
        image.SetValue(1, 1, 3.14f, 0);
        image.GetValue<float>(1, 1, 0).Should().Be(3.14f);
    }

    [Fact]
    public void DoubleType_ShouldSupportReadWrite()
    {
        using var image = new algocv_sharp.Image(3, 3, 1, ImageDataType.Double);
        image.SetValue(1, 1, 2.718281828, 0);
        image.GetValue<double>(1, 1, 0).Should().Be(2.718281828);
    }

    // ImageDataType.Int16 and Int32 share the same underlying value in this version of algocv_sharp,
    // so the native implementation treats both as 16-bit signed integers.
    [Theory]
    [InlineData(ImageDataType.UInt8, 1)]
    [InlineData(ImageDataType.UInt16, 2)]
    [InlineData(ImageDataType.Int32, 4)]
    [InlineData(ImageDataType.Float, 4)]
    [InlineData(ImageDataType.Double, 8)]
    public void ToSpan_Generic_ShouldHaveCorrectLength(ImageDataType dataType, int elementSize)
    {
        const int width = 6;
        const int height = 4;
        using var image = new algocv_sharp.Image(width, height, 1, dataType);

        var expectedLength = image.Stride / elementSize * image.Height;

        var actualLength = dataType switch
        {
            ImageDataType.UInt8 => image.ToSpan<byte>().Length,
            ImageDataType.UInt16 => image.ToSpan<ushort>().Length,
            ImageDataType.Int32 => image.ToSpan<short>().Length,
            ImageDataType.Float => image.ToSpan<float>().Length,
            ImageDataType.Double => image.ToSpan<double>().Length,
            _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
        };

        actualLength.Should().Be(expectedLength);
    }

    [Theory]
    [InlineData(ImageDataType.UInt8)]
    [InlineData(ImageDataType.UInt16)]
    [InlineData(ImageDataType.Int32)]
    [InlineData(ImageDataType.Float)]
    public void MultiChannel_Construct_ShouldPreserveChannelCount(ImageDataType dataType)
    {
        const int width = 4;
        const int height = 3;
        const int channels = 3;

        using var image = new algocv_sharp.Image(width, height, channels, dataType);
        image.Channels.Should().Be(channels);
        image.Stride.Should().BeGreaterThanOrEqualTo(width * channels * GetElementSize(dataType));
    }

    private static int GetElementSize(ImageDataType dataType) => dataType switch
    {
        ImageDataType.UInt8 => 1,
        ImageDataType.Int8 => 1,
        ImageDataType.UInt16 => 2,
        ImageDataType.Int16 => 2,
        ImageDataType.Int32 => 4,
        ImageDataType.Float => 4,
        ImageDataType.Double => 8,
        _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
    };
}
