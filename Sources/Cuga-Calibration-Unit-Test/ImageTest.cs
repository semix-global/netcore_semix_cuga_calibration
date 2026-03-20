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
    public void TestImage(string filePath)
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
        var imageFullPath = Path.GetFullPath($"{nameof(TestImage)}.jpg");
        FileHelper.DeleteFileIfExists(imageFullPath);
        lineImage.Save(imageFullPath);

        using var _ = Process.Start(new ProcessStartInfo
        {
            FileName = imageFullPath,
            UseShellExecute = true
        });
#endif
    }
}