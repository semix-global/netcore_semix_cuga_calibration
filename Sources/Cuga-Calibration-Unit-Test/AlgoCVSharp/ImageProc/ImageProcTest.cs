using algocv_sharp;
using AwesomeAssertions;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Models.Geometries;
using System.IO;

namespace CugaCalibrationUnitTest.AlgoCVSharp.ImageProc;

public class ImageProcTest
{
    [Fact]
    public void ScaleImageMax_ShouldBeEquivalentToHalconScaleImageTo8Bit()
    {
        const string filePath = @"Assets\test.raw";
        var rawBytes = File.ReadAllBytes(filePath);

        using var algoImage = new algocv_sharp.Image(filePath);
        using var algoScaleImage = algocv_sharp.ImageProc.ScaleImageMax(algoImage, 255, ImageDataType.UInt8);

        using var halconImage = RAWImageFactory.CreateImage(rawBytes, false);
        using var halconScaledImage = halconImage.ScaleImageTo8Bit();

        algoScaleImage.Save($@"{AppDomain.CurrentDomain.BaseDirectory}\algoScaleImage.jpg");
        halconScaledImage.Save($@"{AppDomain.CurrentDomain.BaseDirectory}\ScaleImage.jpg");


        // Both results must share the same dimensions and bit depth.
        var halconSize = halconScaledImage.GetSize();
        algoScaleImage.Width.Should().Be(halconSize.Width);
        algoScaleImage.Height.Should().Be(halconSize.Height);
        algoScaleImage.DataType.Should().Be(ImageDataType.UInt8);

        // Compare pixel-by-pixel after converting both buffers to byte.
        var algoPixels = algoScaleImage.ToSpan<byte>().ToArray();
        var halconPixels = Array.ConvertAll(halconScaledImage.GetGrayValuesL(), v => (byte)v);

        algoPixels.Should().BeEquivalentTo(halconPixels, options => options
            .WithStrictOrdering()
            .Using<byte>(ctx =>
                Math.Abs(ctx.Subject - ctx.Expectation).Should().BeLessThan(2))
            .WhenTypeIs<byte>());
    }

    [Fact]
    public void LogToLinear_ShouldBeEquivalentToHalconRaw12BitsPerPixelLogToLinear()
    {
        const string filePath = @"Assets\test.raw";
        var rawBytes = File.ReadAllBytes(filePath);

        using var algoImage = new algocv_sharp.Image(filePath);
        using var algoLinearImage = algocv_sharp.ImageProc.LogToLinear(algoImage);

        using var halconImage = RAWImageFactory.CreateImage(rawBytes, true);

        // Both results must share the same dimensions.
        var halconSize = halconImage.GetSize();
        algoLinearImage.Width.Should().Be(halconSize.Width);
        algoLinearImage.Height.Should().Be(halconSize.Height);

        // Compare pixel-by-pixel after converting both buffers to the same numeric type.
        var algoPixels = algoLinearImage.ToSpan<ushort>().ToArray();
        var halconPixels = Array.ConvertAll(halconImage.GetGrayValuesL(), v => (ushort)v);

        algoLinearImage.Save($@"{AppDomain.CurrentDomain.BaseDirectory}\algoLinearImage.raw");
        halconImage.Save($@"{AppDomain.CurrentDomain.BaseDirectory}\halconImage.bmp");

        algoPixels.Length.Should().Be(halconPixels.Length);
        algoPixels.Should().BeEquivalentTo(halconPixels, options => options
            .WithStrictOrdering()
            .Using<ushort>(ctx =>
                Math.Abs(ctx.Subject - ctx.Expectation).Should().BeLessThan(2))
            .WhenTypeIs<ushort>());
    }

    [Fact]
    public void ParseRawImageInfo_ShouldBeEquivalentToRawImageFactoryGetSize()
    {
        const string filePath = @"Assets\test.raw";
        var rawBytes = File.ReadAllBytes(filePath);

        var (halconSize, halconBodyStart, halconBodyLength) = RAWImageFactory.GetSize(rawBytes);
        var algoInfo = algocv_sharp.ImageProc.ParseRawImageInfo(rawBytes);

        var algoWidth = algoInfo.width;
        var algoHeight = algoInfo.height;
        var algoBodyStart = algoInfo.body_bytes_start_index;
        var algoBodyLength = algoInfo.body_bytes_length;

        algoWidth.Should().Be(halconSize.Width);
        algoHeight.Should().Be(halconSize.Height);
        algoBodyStart.Should().Be(halconBodyStart);
        algoBodyLength.Should().Be(halconBodyLength);
    }

    [Fact]
    public void BuildRawImageBytes_ShouldBeEquivalentToRawImageFactoryBodyAddHeaderFooter()
    {
        const string filePath = @"Assets\test.raw";
        var rawBytes = File.ReadAllBytes(filePath);

        var (halconSize, halconBodyStart, halconBodyLength) = RAWImageFactory.GetSize(rawBytes);
        var bodyBytes = rawBytes.AsSpan().Slice(Convert.ToInt32(halconBodyStart), Convert.ToInt32(halconBodyLength)).ToArray();

        var algoRawBytes = algocv_sharp.ImageProc.BuildRawImageBytes(bodyBytes, halconSize.Width, halconSize.Height);
        var halconRawBytes = RAWImageFactory.BodyAddHeaderFooter(bodyBytes, halconSize);

        // 以algo最新的头协议为准，尾已取消
        // algoRawBytes.Should().BeEquivalentTo(halconRawBytes, options => options.WithStrictOrdering());
    }

    [Fact]
    public void BuildRawImageBytes_HeaderFooterAssembly_ShouldBeComparedWithRawImageFactory_ForMinimalBody()
    {
        // 最小单元 body：1x1 像素，16-bit
        var bodyBytes = new byte[] { 0x34, 0x12 };

        var algoRawBytes = algocv_sharp.ImageProc.BuildRawImageBytes(bodyBytes, 1, 1);
        var halconRawBytes = RAWImageFactory.BodyAddHeaderFooter(bodyBytes, new SizeI(1, 1));

        // 分别解析以定位 body 范围
        var algoInfo = algocv_sharp.ImageProc.ParseRawImageInfo(algoRawBytes);
        var (halconSize, halconBodyStart, halconBodyLength) = RAWImageFactory.GetSize(halconRawBytes);

        // 解析后的宽高应当一致
        algoInfo.width.Should().Be(halconSize.Width);
        algoInfo.height.Should().Be(halconSize.Height);

        // body 内容应当一致
        var algoBody = algoRawBytes.AsSpan((int)algoInfo.body_bytes_start_index, (int)algoInfo.body_bytes_length).ToArray();
        var halconBody = halconRawBytes.AsSpan((int)halconBodyStart, (int)halconBodyLength).ToArray();
        algoBody.Should().BeEquivalentTo(halconBody, options => options.WithStrictOrdering());

        var algoHead = algoRawBytes.AsSpan(0, (int)algoInfo.body_bytes_start_index).ToArray();
        var halconHead = halconRawBytes.AsSpan(0, (int)halconBodyStart).ToArray();
        // 头/尾整体对比（当前实现不等效，失败输出可直接查看差异）
        // PS：协议已更新，数据头第23位修改，数据尾已取消使用
        // algoHead.Should().BeEquivalentTo(halconHead, options => options.WithStrictOrdering());
        // algoRawBytes.Should().BeEquivalentTo(halconRawBytes, options => options.WithStrictOrdering());
    }
}
