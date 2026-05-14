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

    [Fact]
    public void Test1()
    {
        const string filePath = @"Assets\20260320_63_0_0_1_short_014282_PMT08-CH2_8.raw";

        using var lineImage = RAWImageFactory.CreateImage(filePath, true);

        #region 算法调用

        Algorithm.STLR_kla(
            lineImage,
            out var hvXListHTuple,
            out var hvXRatioMaxHTuple,
            out var hvXRatioMeanHTuple,
            out var hvXRatioMinHTuple,
            out var hvYRatioMaxHTuple,
            out var hvYRatioMeanHTuple,
            out var hvYRatioMinHTuple,
            out var hvXValuesHTuple,
            out var hvIndXHTuple,
            out var hvXMaxHTuple,
            out var hvYValuesHTuple,
            out var hvIndYHTuple,
            out var hvYMaxHTuple,
            out var hvPlotXHTuple,
            out var hvPlotYHTuple,
            out var xStrehlList,
            out var yStrehlList,
            out var hvRowBeginXHTuple,
            out var hvColBeginXHTuple,
            out var hvRowEndXHTuple,
            out var hvColEndXHTuple,
            out var hvKxHTuple,
            out var hvRowBeginYHTuple,
            out var hvColBeginYHTuple,
            out var hvRowEndYHTuple,
            out var hvColEndYHTuple,
            out var hvKyHTuple,
            out var hvPercentMeanHTuple);

        _ = xStrehlList;
        _ = yStrehlList;

        using var _0 = hvXListHTuple;
        using var _1 = hvXRatioMaxHTuple;
        using var _2 = hvXRatioMeanHTuple;
        using var _3 = hvXRatioMinHTuple;
        using var _4 = hvYRatioMaxHTuple;
        using var _5 = hvYRatioMeanHTuple;
        using var _6 = hvYRatioMinHTuple;
        using var _7 = hvXValuesHTuple;
        using var _8 = hvIndXHTuple;
        using var _9 = hvXMaxHTuple;
        using var _10 = hvYValuesHTuple;
        using var _11 = hvIndYHTuple;
        using var _12 = hvYMaxHTuple;
        using var _13 = hvPlotXHTuple;
        using var _14 = hvPlotYHTuple;
        using var _15 = hvRowBeginXHTuple;
        using var _16 = hvColBeginXHTuple;
        using var _17 = hvRowEndXHTuple;
        using var _18 = hvColEndXHTuple;
        using var _19 = hvKxHTuple;
        using var _20 = hvRowBeginYHTuple;
        using var _21 = hvColBeginYHTuple;
        using var _22 = hvRowEndYHTuple;
        using var _23 = hvColEndYHTuple;
        using var _24 = hvKyHTuple;
        using var _25 = hvPercentMeanHTuple;

        #endregion
    }
}