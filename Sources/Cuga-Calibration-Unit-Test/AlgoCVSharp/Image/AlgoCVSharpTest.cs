using algocv_sharp;
using AwesomeAssertions;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using System.IO;
using Xunit.Abstractions;

namespace CugaCalibrationUnitTest.AlgoCVSharp.Image;

public class AlgoCVSharpTest(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void LoadRawFile_ShouldProduceValidUInt16Image()
    {
        var filePath = @"Assets\test.raw";
        var rawBytes = File.ReadAllBytes(filePath);

        using var algoImage = new algocv_sharp.Image(filePath);
        var (size, _, _) = RAWImageFactory.GetSize(rawBytes);

        // algocv_sharp 1.2.0 起 RAW 像素序改为行优先（1.0.13 为列优先），解析结果与 HALCON 互为转置：宽高互换。
        algoImage.Width.Should().Be(size.Height);
        algoImage.Height.Should().Be(size.Width);
        algoImage.Channels.Should().Be(1);
        algoImage.DataType.Should().Be(ImageDataType.UInt16);
        algoImage.DataPtr.Should().NotBe(IntPtr.Zero);

        // Sanity check: all pixel values fit in the 16-bit range and the buffer length matches.
        var pixels = algoImage.ToSpan<ushort>().ToArray();
        pixels.Should().OnlyContain(v => v <= 65535);
        pixels.Should().HaveCount(algoImage.Width * algoImage.Height * algoImage.Channels);
    }

    [Fact]
    public void LoadRawFile_Comparison_ShouldReflectTransposeRelationship()
    {
        var filePath = @"Assets\test.raw";
        var rawBytes = File.ReadAllBytes(filePath);

        using var halconImage = RAWImageFactory.CreateImage(rawBytes, false);
        using var algoImage = new algocv_sharp.Image(filePath);
        algoImage.Save($@"{AppDomain.CurrentDomain.BaseDirectory}\algo.raw");

        // algocv_sharp 1.2.0 起 RAW 像素序与 1.0.13（列优先）互为转置：algo(r, c) == halcon(c, r)。
        var algoImageSpans = algoImage.ToSpan<ushort>().ToArray();
        var halconImageSpans = Array.ConvertAll(halconImage.GetGrayValuesL(), item => (ushort)item);
        var (size, _, _) = RAWImageFactory.GetSize(rawBytes);

        var isTranspose = true;
        for (var r = 0; r < algoImage.Height && isTranspose; r++)
        {
            for (var c = 0; c < algoImage.Width && isTranspose; c++)
            {
                isTranspose = algoImageSpans[(r * algoImage.Width) + c] == halconImageSpans[(c * size.Width) + r];
            }
        }

        isTranspose.Should().BeTrue();
    }
}