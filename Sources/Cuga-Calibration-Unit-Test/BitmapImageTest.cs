using algocv_sharp;
using AwesomeAssertions;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Calibration;
using Net.Utilities.Graphics.Algorithms.Halcon;
using System.IO;

namespace CugaCalibrationUnitTest;

/// <summary>
/// BitmapImage 转 AlgoCV Image 测试：BitmapImage 默认为正向图，转换后不做转置，宽高不对调，
/// algo(r,c) == bitmap(r,c)，像素与 HALCON 逐点一致（位深语义保留 12/16bit）。
/// </summary>
public class BitmapImageTest
{
    [Fact]
    public void ToAlgoCVImage_With16BitRawBitmapImage_ShouldKeepUprightImage()
    {
        const string filePath = @"Assets\test.raw";
        var rawBytes = File.ReadAllBytes(filePath);

        using var hImage = RAWImageFactory.CreateImage(rawBytes, true);
        using var bitmapImage = hImage.ToBitmapImage(16);

        using var algoImage = bitmapImage.ToAlgoCVImage();

        // 正向图：宽高与 bitmap 一致，不对调
        algoImage.Width.Should().Be(bitmapImage.Width);
        algoImage.Height.Should().Be(bitmapImage.Height);
        algoImage.Channels.Should().Be(1);
        algoImage.DataType.Should().Be(ImageDataType.UInt16);
        algoImage.DataPtr.Should().NotBe(IntPtr.Zero);

        // 逐点对比：algo 行优先序列 == HALCON 正向图行优先序列（algo(r,c) == halcon(r,c)）
        var algoPixels = algoImage.ToSpan<ushort>().ToArray();
        var halconPixels = Array.ConvertAll(hImage.GetGrayValuesL(), value => (ushort)value);

        algoPixels.Should().BeEquivalentTo(halconPixels, options => options.WithStrictOrdering());
    }

    [Fact]
    public void ToAlgoCVImage_With12BitRawBitmapImage_ShouldKeepUprightImage()
    {
        const string filePath = @"Assets\test.raw";
        var rawBytes = File.ReadAllBytes(filePath);

        using var hImage = RAWImageFactory.CreateImage(rawBytes, false);
        using var bitmapImage = hImage.ToBitmapImage(12);

        using var algoImage = bitmapImage.ToAlgoCVImage();

        // 正向图：宽高与 bitmap 一致，不对调
        algoImage.Width.Should().Be(bitmapImage.Width);
        algoImage.Height.Should().Be(bitmapImage.Height);
        algoImage.Channels.Should().Be(1);
        algoImage.DataType.Should().Be(ImageDataType.UInt16);
        algoImage.DataPtr.Should().NotBe(IntPtr.Zero);

        // 逐点对比：algo 行优先序列 == HALCON 正向图行优先序列（algo(r,c) == halcon(r,c)）
        var algoPixels = algoImage.ToSpan<ushort>().ToArray();
        var halconPixels = Array.ConvertAll(hImage.GetGrayValuesL(), value => (ushort)value);

        algoPixels.Should().BeEquivalentTo(halconPixels, options => options.WithStrictOrdering());
    }

    [Fact]
    public void ToAlgoCVImage_With12BitRawBitmapImage_ShouldMatchBitmapBytes()
    {
        const string filePath = @"Assets\test.raw";
        var rawBytes = File.ReadAllBytes(filePath);

        using var hImage = RAWImageFactory.CreateImage(rawBytes, false);
        using var bitmapImage = hImage.ToBitmapImage(12);

        using var algoCVImage = bitmapImage.ToAlgoCVImage();
        algoCVImage.Save($@"{AppDomain.CurrentDomain.BaseDirectory}\BitmapImageToAlgoImage.raw");

        // 字节级逐点对比：正向图不重排，algo 字节序列与 bitmap 字节序列完全一致（Gray12 → UInt16，2 字节/像素）
        var algoBytes = algoCVImage.ToSpan().ToArray();
        var bitmapBytes = bitmapImage.GetPixelSpan().ToArray();

        algoBytes.SequenceEqual(bitmapBytes).Should().BeTrue();
    }
}