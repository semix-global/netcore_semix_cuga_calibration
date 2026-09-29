using AwesomeAssertions;
using Core.Models.Enums.Algorithm;
using Core.Models.Models.Setting;
using Core.Services.Implements;
using Microsoft.Extensions.Logging.Abstractions;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Xunit.Abstractions;
using Size = Net.Utilities.Models.Geometries.Size;

namespace CugaCalibrationUnitTest.AlgoCVSharp.PixelSize;

/// <summary>
/// PixelSize 服务级测试：经 ICalibrationAlgorithmService 的 AlgoCSharp 分支（含 ToAlgoCVImage 桥）完整链路验证。
/// BF（MicroscopePixelSize）：jpg 为常规格式，桥直搬保持物理正向，与 BfPixelSizeTest 引擎直测值交叉验证。
/// DF（YPixelSize）：16bit tiff 经桥直搬保持正向喂入（引擎假定正向输入），与 DfPixelSizeTest 引擎直测值交叉验证。
/// </summary>
public class PixelSizeServiceTest(ITestOutputHelper testOutputHelper)
{
    /// <summary>
    /// BF 全倍率用例：倍率-网格尺寸对应 5x-100um / 10x-50um / 50x-25um / 100x-10um / 150x-10um（AlgorithmStandardMaskSquareSizeEnum）。
    /// 期望值 = 网格尺寸 / 引擎直测格距（tx）；服务按网格尺寸预估 period_half/n_max 扫描窗（400/400/500/600，见 GetAlgoCVSharpPixelSize）。
    /// </summary>
    [Theory]
    [InlineData(@"Assets\BFPixelSize\10-10-6-19_5X_20251014.jpg", 100, 0.6863)]
    [InlineData(@"Assets\BFPixelSize\10-10-6-19_10X_20251014.jpg", 50, 0.6861)]
    [InlineData(@"Assets\BFPixelSize\10-10-6-19_50X_20251014.jpg", 25, 0.0685)]
    [InlineData(@"Assets\BFPixelSize\10-10-6-19_100X_20251014.jpg", 10, 0.0347)]
    [InlineData(@"Assets\BFPixelSize\10-10-6-19_150X_20260402.jpg", 10, 0.0232)]
    public void GetPixelSize_AlgoCSharp_BfPixelSize_AllMagnifications_ShouldMatchMaskSizeOverPitch(string filePath, double maskSizeUm, double expectedUmPerPixel)
    {
        using var image = new BitmapImage(filePath);
        var service = new CalibrationAlgorithmServiceImpl(
            NullLogger<CalibrationAlgorithmServiceImpl>.Instance,
            new CalibrationSetting(),
            new AffineTransformation(NullLogger<AffineTransformation>.Instance));

        var pixelSize = service.GetPixelSize(image, new Size(maskSizeUm, maskSizeUm), Guid.NewGuid(), AlgorithmEngineTypeEnum.AlgoCSharp, out var drawingImage, out var angle);

        testOutputHelper.WriteLine($"BfPixelSize: X={pixelSize.Width:0.000000} um/px, Y={pixelSize.Height:0.000000} um/px, Angle={angle:0.###} deg");

        // 引擎直测格距：5x tx≈145.72 / 10x 72.88 / 50x 365.21 / 100x 288.64 / 150x 431.01 → 网格尺寸/格距，交叉验证 ±5%
        pixelSize.Width.Should().BeInRange(expectedUmPerPixel * 0.95, expectedUmPerPixel * 1.05);
        pixelSize.Height.Should().BeInRange(expectedUmPerPixel * 0.95, expectedUmPerPixel * 1.05);
        Math.Abs(angle).Should().BeLessThan(2);
    }

    [Fact]
    public void GetYPixelSize_AlgoCSharp_DfPixelSize_ShouldMatchEngineDirectResult()
    {
        const string filePath = @"Assets\DFPixelSize\test\OI-S10_20260613.tiff";

        using var hImage = new HalconDotNet.HImage(filePath);
        using var image = hImage.ToBitmapImage(16);
        var service = new CalibrationAlgorithmServiceImpl(
            NullLogger<CalibrationAlgorithmServiceImpl>.Instance,
            new CalibrationSetting(),
            new AffineTransformation(NullLogger<AffineTransformation>.Instance));

        var yPixelSize = service.GetYPixelSize(image, 10.0, Guid.NewGuid(), AlgorithmEngineTypeEnum.AlgoCSharp, out var drawingImage, out var yProjects, out var resultIndexes);

        testOutputHelper.WriteLine($"DfPixelSize: Y={yPixelSize:0.000000} um/px");

        // DfPixelSizeTest 引擎直测（正向图喂入）：OI-S10 帧 step_mean_c≈30.95 → 10/30.95 ≈ 0.3231 um/px，交叉验证 ±10%
        yPixelSize.Should().BeInRange(0.3231 * 0.9, 0.3231 * 1.1);

        // Y 投影曲线 = 每行一个投影点（正向图行号即物理 Y）；配对行位置 r0 对应 AMPD 峰位语义
        yProjects.Should().HaveCount(image.Height);
        resultIndexes.Should().NotBeEmpty();
        resultIndexes.Should().OnlyContain(t => t >= 0 && t < image.Height);
    }
}
