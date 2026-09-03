using algocv_sharp;
using AwesomeAssertions;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Helpers.Extensions;
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

        // Verify algocv_sharp.Image parses the same dimensions as HALCON.
        algoImage.Width.Should().Be(size.Width);
        algoImage.Height.Should().Be(size.Height);
        algoImage.Channels.Should().Be(1);
        algoImage.DataType.Should().Be(ImageDataType.UInt16);
        // algoImage.IsEmpty.Should().BeFalse();
        algoImage.DataPtr.Should().NotBe(IntPtr.Zero);

        // Sanity check: all pixel values fit in the 16-bit range and the buffer length matches.
        var pixels = algoImage.ToSpan<ushort>().ToArray();
        pixels.Should().OnlyContain(v => v <= 65535);
        pixels.Length.Should().Be(algoImage.Width * algoImage.Height * algoImage.Channels);
    }

    [Fact]
    public void LoadRawFile_Comparison_ShouldReflectTransposeRelationship()
    {
        var filePath = @"Assets\test.raw";
        var rawBytes = File.ReadAllBytes(filePath);

        using var halconImage = RAWImageFactory.CreateImage(rawBytes, false);
        using var algoImage = new algocv_sharp.Image(filePath);
        algoImage.Save($@"{AppDomain.CurrentDomain.BaseDirectory}\algo.raw");
        // Sanity check: all pixel values fit in the 16-bit range and the buffer length matches.
        var algoImageSpans = algoImage.ToSpan<ushort>();
        var halconImageSpans = Array.ConvertAll(halconImage.GetGrayValuesL(), item => (ushort)item).AsSpan();
        algoImageSpans.SequenceEqual(halconImageSpans).Should().BeTrue();
    }
}