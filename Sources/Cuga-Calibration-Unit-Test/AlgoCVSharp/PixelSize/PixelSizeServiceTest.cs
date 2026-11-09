using AwesomeAssertions;
using Core.Algorithm.Implements.AlgoCSharp;
using Core.Algorithm.Models.YPixelSize;
using Core.Models.Enums.Algorithm;
using Core.Models.Models.Common.DarkField;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace CugaCalibrationUnitTest.AlgoCVSharp.PixelSize;

/// <summary>
/// PixelSize 测试：
/// DF（YPixelSize）引擎级：经 AlgoCvYPixelSizeEngine（原服务 AlgoCSharp 分支已迁移为引擎），与 DfPixelSizeTest 引擎直测值交叉验证。
/// BF 服务级链路（GetPixelSize）已删除，BF 引擎直测见 BfPixelSizeTest。
/// </summary>
public class PixelSizeServiceTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void YPixelSizeEngine_AlgoCv_DfPixelSize_ShouldMatchEngineDirectResult()
    {
        const string rawFilePath = @"Assets\YPixelSize\20260227_2486_0_0_1_short_001000_PMT13-CH3_13.raw";

        var engine = new AlgoCvYPixelSizeEngine(NullLogger<AlgoCvYPixelSizeEngine>.Instance);

        // 引擎接口为 DTO 双通道（各引擎各自 raw 底座直读），tiff 资产不再适用，改用 YPixelSize raw 资产（log 域 12bit）
        using var darkFieldImage = new DarkFieldImageDTO { RawImageFilePath = rawFilePath };
        var ret = engine.Excute(darkFieldImage, new YPixelSizeEngineParam { StandardMaskSquareSize = AlgorithmStandardMaskSquareSizeEnum.Size10 }, Guid.NewGuid());
        ret.Success.Should().BeTrue(ret.ErrorMsg);

        using var drawingImage = ret.Data!.DrawingImage;
        var yPixelSize = ret.Data.YPixelSize;

        testOutputHelper.WriteLine($"DfPixelSize: Y={yPixelSize:0.000000} um/px");

        // raw 资产直测锁定（V3 log 域 12bit）：PMT13 帧 Y 向列向节距 step_mean_c≈51 → 10/51 ≈ 0.196 um/px（X 向 30px/格为运动向周期，非 Y 尺寸），锁定 ±5%
        yPixelSize.Should().BeInRange(0.196 * 0.95, 0.196 * 1.05);

        // Y 投影曲线 = 每行一个投影点（正向图行号即物理 Y）；ResultIndexes 为 AMPD 峰位（对齐 HAlgorithm 版语义）
        ret.Data.YProjects.Should().HaveCount(drawingImage.Size.Height);
        ret.Data.ResultIndexes.Should().NotBeEmpty();
        ret.Data.ResultIndexes.Should().OnlyContain(t => t >= 0 && t < drawingImage.Size.Height);
    }

}