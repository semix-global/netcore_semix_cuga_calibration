// #define YPixelSizeTest

using AwesomeAssertions;
using HalconDotNet;
using HAlgorithm;
using Net.Utilities.Algorithms.Halcon;

#if YPixelSizeTest
using System.IO;
using System.Diagnostics;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Algorithms.Halcon.Extensions;
#endif

namespace CugaCalibrationUnitTest;

public sealed class HAlgorithmTest
{
    private static readonly Algorithm Algorithm = new();

    [Fact]
    public void Test()
    {
        using var lineImage = RAWImageFactory.CreateImage(@"Assets\YPixelSize\20260227_2486_0_0_1_short_001000_PMT13-CH3_13.raw", true);
        using var expectedLinearImage = RAWImageFactoryV2.CreateImage(@"Assets\YPixelSize\20260227_2486_0_0_1_short_001000_PMT13-CH3_13.raw", true);

        using var saveResultImageHTuple = new HTuple(1);
        Algorithm.DarkPixSizeCal(expectedLinearImage, saveResultImageHTuple, out var expectedDrawImageHObject, out var expectedMeanTuple);
        using var _3 = expectedDrawImageHObject;
        using var _4 = expectedMeanTuple;

        Algorithm.DarkPixSizeCal(lineImage, saveResultImageHTuple, out var drawImageHObject, out var meanTuple);
        using var _5 = drawImageHObject;
        using var _6 = meanTuple;

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