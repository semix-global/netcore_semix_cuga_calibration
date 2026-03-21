// #define YPixelSizeTest

using AwesomeAssertions;
using Core.Utilities;
using HAlgorithm;
using Net.Utilities.Algorithms.Halcon;
using Xunit;

#if YPixelSizeTest
using HalconDotNet;
using System.IO;
using System.Diagnostics;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Algorithms.Halcon.Extensions;
#endif

namespace CugaCalibrationUnitTest;

public sealed class YPixelSizeTest
{
    private static readonly Algorithm Algorithm = new();

    [Fact]
    public void Test()
    {
        using var originImage = RawImageFactory.CreateImage(@"Assets\YPixelSize\20260227_2486_0_0_1_short_001000_PMT13-CH3_13.raw");

        Algorithm.InvertTransformPatchImage128(originImage, out var expectedLinearImage);
        using var _0 = originImage;

        using var lineImage = originImage.RAW12BitsPerPixelLogToLinear();

        Algorithm.DarkPixSizeCal(expectedLinearImage, 1, out var expectedDrawImageHObject, out var expectedMeanTuple);
        using var _1 = expectedDrawImageHObject;
        using var _2 = expectedMeanTuple;

        Algorithm.DarkPixSizeCal(lineImage, 1, out var drawImageHObject, out var meanTuple);
        using var _3 = drawImageHObject;
        using var _4 = meanTuple;

        meanTuple.D.Should().Be(expectedMeanTuple.D);

#if YPixelSizeTest
        var imageFullPath = Path.GetFullPath($"{nameof(YPixelSizeTest)}.jpg");
        FileHelper.DeleteFileIfExists(imageFullPath);
        using var drawImage = new HImage(drawImageHObject);
        drawImage.Save(imageFullPath);

        using var _ = Process.Start(new ProcessStartInfo
        {
            FileName = imageFullPath,
            UseShellExecute = true
        });
#endif
    }
}