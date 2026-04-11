// #define ImageTest

using AwesomeAssertions;
using HalconDotNet;
using HAlgorithm;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;

#if ImageTest
using System.Diagnostics;
using System.IO;
using Net.Utilities.Helpers.Helpers.Files;
#endif

namespace CugaCalibrationUnitTest;

public class ImageTest
{
    private static readonly Algorithm Algorithm = new();

    [Theory]
    [InlineData(@"YPixelSize\20260227_2486_0_0_1_short_001000_PMT13-CH3_13.raw")]
    [InlineData(@"VSharpTest\test_1_4_true.raw")]
    [InlineData(@"VSharpTest\test_1_6_true.raw")]
    [InlineData(@"VSharpTest\20260315_1116_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_1119_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_1107_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_1110_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_1098_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_1101_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_780_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_783_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_1122_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_1113_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_1104_0_0_1_short_001000_PMT08-CH3_8.raw")]
    [InlineData(@"VSharpTest\20260315_786_0_0_1_short_001000_PMT08-CH3_8.raw")]
    public void Test(string filePath)
    {
        using var originImage = RAWImageFactory.CreateImage(@$"Assets\{filePath}", true);

        using var imageFilePath = new HTuple(@$"Assets\{filePath}");
        Algorithm.AutoReadRawImage(out var autoReadRawImageHObject, imageFilePath);
        using var _0 = autoReadRawImageHObject;
        Algorithm.RotateAndMirror(autoReadRawImageHObject, out var rotateAndMirrorHObject);
        using var _1 = rotateAndMirrorHObject;
        Algorithm.InvertTransformPatchImage128(rotateAndMirrorHObject, out var expectedLinearImage);
        using var _2 = expectedLinearImage;

        HOperatorSet.GetRegionPoints(expectedLinearImage, out var expectedRowsHTuple, out var expectedColumnsHTuple);
        using var _3 = expectedRowsHTuple;
        using var _4 = expectedColumnsHTuple;
        HOperatorSet.GetGrayval(expectedLinearImage, expectedRowsHTuple, expectedColumnsHTuple, out var expectedGrayValHTuple);
        using var _5 = expectedGrayValHTuple;

        originImage.GetGrayValuesL().Should()
            .BeEquivalentTo(expectedGrayValHTuple.ToLArr(), options => options
                .WithStrictOrdering()
                .Using<long>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 1))
                .WhenTypeIs<long>());

#if ImageTest
        var imageFullPath = Path.GetFullPath($"{nameof(ImageTest)}.jpg");
        FileHelper.DeleteFileIfExists(imageFullPath);
        originImage.Save(imageFullPath);

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