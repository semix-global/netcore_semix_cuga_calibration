#define CIBXTCTest

using AwesomeAssertions;
using Core.Models.Models.AOD.Uniformity;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Xunit.Abstractions;

#if CIBXTCTest
using System.IO;
using System.Windows;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.WPF.V2;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
#endif

namespace CugaCalibrationUnitTest;

/// <summary>
/// XTC 水平投影最小值单元测试: 读 Assets\XTC 的 raw, 分别用 Log 图和转线性图调用 WindowItem.CalculateHorizontalProjectMinPixel.
/// 打开顶部 define 后可用 ScottPlot 查看原始投影、平滑曲线与最小像素竖线.
/// </summary>
public class CIBXTCTest(ITestOutputHelper testOutputHelper)
{
    private const int SegmentCount = 10;

    [Theory]
    [InlineData(@"Assets\XTC\20261021_149_0_0_1_short_001000_PMT07-CH1_7.raw")]
    [InlineData(@"Assets\XTC\20261021_150_0_0_1_short_001000_PMT07-CH2_7.raw")]
    [InlineData(@"Assets\XTC\20261021_148_0_0_1_short_001000_PMT07-CH3_7.raw")]
    public void CalculateHorizontalProjectMinPixel(string filePath) => Calculate(filePath, false);

    /// <summary>
    /// 转线性图后再算水平投影最小值.
    /// </summary>
    [Theory]
    [InlineData(@"Assets\XTC\20261021_149_0_0_1_short_001000_PMT07-CH1_7.raw")]
    [InlineData(@"Assets\XTC\20261021_150_0_0_1_short_001000_PMT07-CH2_7.raw")]
    [InlineData(@"Assets\XTC\20261021_148_0_0_1_short_001000_PMT07-CH3_7.raw")]
    public void CalculateHorizontalProjectMinPixel_ToLinear(string filePath) => Calculate(filePath, true);

    private void Calculate(string filePath, bool isToLinear)
    {
        using var image = RAWImageFactory.CreateImage(filePath, isToLinear);
        var item = new AODUniformityDTO.WindowItem
        {
            ImageHorizontalProjects = image.GetHorizontalProjects(),
            RawImageFilePath = filePath
        };
        item.CalculateHorizontalProjectMinPixel(SegmentCount);

        item.ImageHorizontalProjects.Count.Should().BeGreaterThan(51);
        item.SmoothImageHorizontalProjects.Count.Should().Be(item.ImageHorizontalProjects.Count);
        item.HorizontalProjectMinPixel.Should().BeInRange(0, item.ImageHorizontalProjects.Count - 1);

        testOutputHelper.WriteLine($"{filePath} isToLinear={isToLinear} MinPixel={item.HorizontalProjectMinPixel}");

#if CIBXTCTest
        var pmtValueTitle = isToLinear ? "Linear" : "Log";
        var thread = new Thread(() =>
        {
            var window = new Window
            {
                Title = $"{nameof(CIBXTCTest)}_{Path.GetFileNameWithoutExtension(filePath)}_{pmtValueTitle}",
                WindowState = WindowState.Maximized
            };

            var plotControl = new PlotControl();
            window.Content = plotControl;

            var plotDataSource = new PlotDataSource();
            plotControl.DataSource = plotDataSource;
            plotDataSource.Configure(new Columns());
            plotDataSource.SetTitle(0, $"{Path.GetFileName(filePath)} MinPixel: {item.HorizontalProjectMinPixel} (Y: PMT Value({pmtValueTitle}) - X: px)");

            plotDataSource.GetOrAddScatterLine(0, "Origin", [.. item.ImageHorizontalProjects.ToPoints()], Colors.Blue);
            plotDataSource.GetOrAddScatterLine(0, "Smooth", [.. item.SmoothImageHorizontalProjects.ToPoints()], Colors.DarkBlue);
            plotDataSource.GetOrAddXLine(0, "Min Pixel", item.HorizontalProjectMinPixel, Colors.Red);

            plotDataSource.AutoScaleRefresh();
            window.ShowDialog();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
#endif
    }
}