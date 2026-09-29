using algocv_sharp;
using AwesomeAssertions;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Calibration;
using Net.Utilities.Graphics.Algorithms.Halcon;
using System.IO;
using Xunit.Abstractions;

namespace CugaCalibrationUnitTest;

/// <summary>
/// BitmapImage 与 AlgoCV Image 互转测试：raw 图入 BitmapImage 转 algoImage 需要转置（isRawSource: true，宽高互换、
/// algo(r,c) == bitmap(c,r) 转置重排），像素与 HALCON 逐点一致（位深语义保留 12/16bit）。
/// </summary>
public class BitmapImageTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void ToAlgoCVImage_With16BitRawBitmapImage_ShouldTransposeToProtocolImage()
    {
        const string filePath = @"Assets\test.raw";
        var rawBytes = File.ReadAllBytes(filePath);

        using var hImage = RAWImageFactory.CreateImage(rawBytes, true);
        using var bitmapImage = hImage.ToBitmapImage(16);

        using var algoImage = bitmapImage.ToAlgoCVImage(isRawSource: true);

        // raw 来源：桥转置 —— 宽高互换
        algoImage.Width.Should().Be(bitmapImage.Height);
        algoImage.Height.Should().Be(bitmapImage.Width);
        algoImage.Channels.Should().Be(1);
        algoImage.DataType.Should().Be(ImageDataType.UInt16);
        algoImage.DataPtr.Should().NotBe(IntPtr.Zero);

        // 转置重排：algo 行优先序列 = HALCON 物理正向序列的转置（algo(r,c) == halcon(c,r)）
        var algoPixels = algoImage.ToSpan<ushort>().ToArray();
        var halconPixels = Array.ConvertAll(hImage.GetGrayValuesL(), value => (ushort)value);
        var expectedPixels = new ushort[halconPixels.Length];
        for (var y = 0; y < bitmapImage.Height; y++)
        {
            for (var x = 0; x < bitmapImage.Width; x++)
            {
                expectedPixels[x * bitmapImage.Height + y] = halconPixels[y * bitmapImage.Width + x];
            }
        }

        algoPixels.Should().BeEquivalentTo(expectedPixels, options => options.WithStrictOrdering());
    }

    [Fact]
    public void ToAlgoCVImage_With12BitRawBitmapImage_ShouldTransposeToProtocolImage()
    {
        const string filePath = @"Assets\test.raw";
        var rawBytes = File.ReadAllBytes(filePath);

        using var hImage = RAWImageFactory.CreateImage(rawBytes, false);
        using var bitmapImage = hImage.ToBitmapImage(12);

        using var algoImage = bitmapImage.ToAlgoCVImage(isRawSource: true);

        // raw 来源：桥转置 —— 宽高互换
        algoImage.Width.Should().Be(bitmapImage.Height);
        algoImage.Height.Should().Be(bitmapImage.Width);
        algoImage.Channels.Should().Be(1);
        algoImage.DataType.Should().Be(ImageDataType.UInt16);
        algoImage.DataPtr.Should().NotBe(IntPtr.Zero);

        // 转置重排：algo 行优先序列 = HALCON 物理正向序列的转置（algo(r,c) == halcon(c,r)）
        var algoPixels = algoImage.ToSpan<ushort>().ToArray();
        var halconPixels = Array.ConvertAll(hImage.GetGrayValuesL(), value => (ushort)value);
        var expectedPixels = new ushort[halconPixels.Length];
        for (var y = 0; y < bitmapImage.Height; y++)
        {
            for (var x = 0; x < bitmapImage.Width; x++)
            {
                expectedPixels[x * bitmapImage.Height + y] = halconPixels[y * bitmapImage.Width + x];
            }
        }

        algoPixels.Should().BeEquivalentTo(expectedPixels, options => options.WithStrictOrdering());
    }

    [Fact]
    public void ToAlgoCVImage_With12BitRawBitmapImage_ShouldMatchTransposedBytes()
    {
        const string filePath = @"Assets\test.raw";
        var rawBytes = File.ReadAllBytes(filePath);

        using var hImage = RAWImageFactory.CreateImage(rawBytes, false);
        using var bitmapImage = hImage.ToBitmapImage(12);

        using var algoCVImage = bitmapImage.ToAlgoCVImage(isRawSource: true);
        algoCVImage.Save($@"{AppDomain.CurrentDomain.BaseDirectory}\BitmapImageToAlgoImage.raw");

        // 字节级转置重排对比（Gray12 → UInt16，2 字节/像素块整体搬移）
        var algoBytes = algoCVImage.ToSpan().ToArray();
        var bitmapBytes = bitmapImage.GetPixelSpan().ToArray();
        var expectedBytes = new byte[algoBytes.Length];
        for (var y = 0; y < bitmapImage.Height; y++)
        {
            for (var x = 0; x < bitmapImage.Width; x++)
            {
                var source = (y * bitmapImage.Width + x) * 2;
                var destination = (x * bitmapImage.Height + y) * 2;
                expectedBytes[destination] = bitmapBytes[source];
                expectedBytes[destination + 1] = bitmapBytes[source + 1];
            }
        }

        algoBytes.SequenceEqual(expectedBytes).Should().BeTrue();
    }
}
