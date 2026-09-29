using algocv_sharp;
using AwesomeAssertions;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Calibration;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Xunit.Abstractions;
using AlgoCvImage = algocv_sharp.Image;

namespace CugaCalibrationUnitTest.AlgoCVSharp.BestFocus;

public class BestFocusTransposeArbitrationTest(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData(@"Assets\20260508_214_0_0_1_short_011375_PMT08-CH2_8.raw")]
    public void Process_TwoPaths_ShouldArbitrateTransposeSemantics(string filePath)
    {
        using var imageB = new AlgoCvImage(filePath);
        using var engineB = new BestFocusEngine(new BestFocusConfig());
        using var resultB = engineB.Process(imageB);

        using var hImage = RAWImageFactory.CreateImage(filePath, true);
        using var bitmapImage = hImage.ToBitmapImage();
        using var imageA = bitmapImage.ToAlgoCVImage(isRawSource: true);
        using var engineA = new BestFocusEngine(new BestFocusConfig());
        using var resultA = engineA.Process(imageA);

        testOutputHelper.WriteLine($"Image B (transposed): {imageB.Width} x {imageB.Height}");
        testOutputHelper.WriteLine($"Image A' (bridge raw transpose): {imageA.Width} x {imageA.Height}");
        Dump("B.y", resultB.metric_array_info_y);
        Dump("B.x", resultB.metric_array_info_x);
        Dump("A'.y", resultA.metric_array_info_y);
        Dump("A'.x", resultA.metric_array_info_x);

        resultB.metric_array_info_y.pixel_pos_per_col.Should().NotBeEmpty();
        resultB.metric_array_info_y.pixel_pos_per_row.Should().NotBeEmpty();
        resultA.metric_array_info_y.pixel_pos_per_col.Should().NotBeEmpty();
        resultA.metric_array_info_y.pixel_pos_per_row.Should().NotBeEmpty();

        SameShape(resultA.metric_array_info_y, resultB.metric_array_info_y).Should().BeTrue();
        SameShape(resultA.metric_array_info_x, resultB.metric_array_info_x).Should().BeTrue();
    }

    private void Dump(string tag, FocusMetricArrayInfo info)
    {
        testOutputHelper.WriteLine($"{tag}: rows={info.metric_array_rows}, cols={info.metric_array_cols}, per_col={info.pixel_pos_per_col.Count}, per_row={info.pixel_pos_per_row.Count}, ribbon k={info.mmetric_v_ribbon_k:0.######}, b={info.mmetric_v_ribbon_b:0.######}");
    }

    private static bool SameShape(FocusMetricArrayInfo a, FocusMetricArrayInfo b) =>
        a.metric_array_rows == b.metric_array_rows
        && a.metric_array_cols == b.metric_array_cols
        && a.pixel_pos_per_col.Count == b.pixel_pos_per_col.Count
        && a.pixel_pos_per_row.Count == b.pixel_pos_per_row.Count;
}
