// #define ImageTest

using AwesomeAssertions;
using Core.Utilities;
using HalconDotNet;
using HAlgorithm;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Models;
using Xunit;

#if ImageTest
using System.Diagnostics;
using System.IO;
using Net.Utilities.Algorithms.Halcon.Extensions;
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
        using var originImage = RawImageFactory.CreateImage(@$"Assets\{filePath}");

        var (expectedMatrix, size) = RawImageFactory.ToMatrix(@$"Assets\{filePath}");

        var matrix = originImage.RAW16BitsPerPixelToMatrix();
        var row = originImage.RAW16BitsPerPixelGetRow((int)size.Height / 2);
        var column = originImage.RAW16BitsPerPixelGetColumn((int)size.Width / 2);

        matrix.Should()
            .BeEquivalentTo(expectedMatrix, options => options.WithStrictOrdering());
        row.Should()
            .BeEquivalentTo(MatrixUtils.Row(expectedMatrix, (int)size.Height / 2), options => options.WithStrictOrdering());
        column.Should()
            .BeEquivalentTo(MatrixUtils.Column(expectedMatrix, (int)size.Width / 2), options => options.WithStrictOrdering());

        Algorithm.InvertTransformPatchImage128(originImage, out var expectedLinearImage);
        using var _0 = originImage;

        HOperatorSet.GetRegionPoints(expectedLinearImage, out var expectedRowsHTuple, out var expectedColumnsHTuple);
        using var _1 = expectedRowsHTuple;
        using var _2 = expectedColumnsHTuple;
        HOperatorSet.GetGrayval(expectedLinearImage, expectedRowsHTuple, expectedColumnsHTuple, out var expectedGrayValHTuple);
        using var _3 = expectedGrayValHTuple;

        using var lineImage = originImage.RAW12BitsPerPixelLogToLinear();

        using var region = lineImage.GetDomain();
        region.GetRegionPoints(out var rowsHTuple, out var columnsHTuple);
        using var _4 = rowsHTuple;
        using var _5 = columnsHTuple;
        using var grayValHTuple = lineImage.GetGrayval(rowsHTuple, columnsHTuple);

        grayValHTuple.ToLArr().Should()
            .BeEquivalentTo(expectedGrayValHTuple.ToLArr(), options => options.WithStrictOrdering());

#if ImageTest
        var imageFullPath = Path.GetFullPath($"{nameof(ImageTest)}.jpg");
        FileHelper.DeleteFileIfExists(imageFullPath);
        lineImage.Save(imageFullPath);

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
        // Algorithm.AutoReadRawImage(out var image, filePath);
        // Algorithm.RotateAndMirror(image, out var image1);
        // Algorithm.InvertTransformPatchImage128(image1, out var lineImage);
        // using var _ = lineImage;

        using var originImage = RawImageFactory.CreateImage(filePath);
        using var lineImage = originImage.RAW12BitsPerPixelLogToLinear();

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