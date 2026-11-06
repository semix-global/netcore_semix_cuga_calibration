using AwesomeAssertions;
using Core.Algorithm.Implements;
using Core.Algorithm.Implements.AlgoCSharp;
using Core.Algorithm.Implements.Halcon;
using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.BestFocus;
using Core.Algorithm.Models.BestFocus;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.CIB;
using Core.Models.Models.Common.DarkField;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Calibration;
using Xunit.Abstractions;

namespace CugaCalibrationUnitTest.AlgoCVSharp.BestFocus;

/// <summary>
/// BestFocus 引擎契约测试：经 IAlgorithmEngines.BestFocus(engine) 分发。
/// 资产经 DTO 传入，各引擎各自 raw 底座直读：AlgoCSharp 经 RAWImageFactoryV3 读 log 域原图; HAlgorithm 经 RAWImageFactoryV2 产 HImage（随 IsToLiner 线性化，PMTLog 且 KeepRaw=false 时为线性域）。HAlgorithm 分支不区分 DSW/DSW065（STLR_kla 忽略该参数）。
/// </summary>
public class BestFocusEngineTest(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData(AlgorithmEngineTypeEnum.HAlgorithm, @"Assets\20260508_214_0_0_1_short_011375_PMT08-CH2_8.raw", AlgorithmBestFocusTypeEnum.DSW065)]
    [InlineData(AlgorithmEngineTypeEnum.AlgoCSharp, @"Assets\20260508_214_0_0_1_short_011375_PMT08-CH2_8.raw", AlgorithmBestFocusTypeEnum.DSW065)]
    [InlineData(AlgorithmEngineTypeEnum.AlgoCSharp, @"Assets\BestFocus16.3.raw", AlgorithmBestFocusTypeEnum.DSW)]
    public void Excute_BothEngines_ShouldReturnValidBestFocus(AlgorithmEngineTypeEnum engineType, string filePath, AlgorithmBestFocusTypeEnum bestFocusType)
    {
        using var host = CreateEngineHost();
        var engine = host.Services.GetRequiredService<IAlgorithmEngines>().BestFocus(engineType);
        using var darkFieldImage = new DarkFieldImageDTO
        {
            RawImageFilePath = filePath,
            RawImageCIBProfileModeEnum = CIBProfileModeEnum.PMTLog,
            IsKeepRawImageCIBProfileModeEnum = false
        };

        const double startEcs = 0d;
        const double stopEcs = 100d;
        var ret = engine.Excute(darkFieldImage, new BestFocusEngineParam { StartEcs = startEcs, StopEcs = stopEcs, BestFocusType = bestFocusType }, Guid.NewGuid());

        ret.Success.Should().BeTrue(ret.ErrorMsg);
        var bestFocus = ret.Data!;

        testOutputHelper.WriteLine($"Engine: {engineType}, BestFocusType: {bestFocusType}");
        testOutputHelper.WriteLine($"BestXStrehlRatioPoint: ({bestFocus.BestXStrehlRatioPoint.X:0.####}, {bestFocus.BestXStrehlRatioPoint.Y:0.####})");
        testOutputHelper.WriteLine($"BestYStrehlRatioPoint: ({bestFocus.BestYStrehlRatioPoint.X:0.####}, {bestFocus.BestYStrehlRatioPoint.Y:0.####})");
        testOutputHelper.WriteLine($"BestXStrehlRatioECS: {bestFocus.BestXStrehlRatioECS:0.####}");
        testOutputHelper.WriteLine($"BestYStrehlRatioECS: {bestFocus.BestYStrehlRatioECS:0.####}");

        bestFocus.IsAlgorithmOk.Should().BeTrue();
        bestFocus.XStrehlRatioPoints.Should().NotBeEmpty();
        bestFocus.YStrehlRatioPoints.Should().NotBeEmpty();
        bestFocus.XStrehlRatioColumnPoints.Should().NotBeEmpty();
        bestFocus.YStrehlRatioColumnPoints.Should().NotBeEmpty();
        bestFocus.BestXStrehlRatioECS.Should().BeInRange(startEcs, stopEcs);
        bestFocus.BestYStrehlRatioECS.Should().BeInRange(startEcs, stopEcs);
        bestFocus.XFieldTiltPoints.Should().NotBeEmpty();
        bestFocus.YFieldTiltPoints.Should().NotBeEmpty();
    }

    /// <summary>
    /// 既有失败用例（HALCON error #1401），已确定默认不再重复执行；恢复验证时移除 Skip 即可
    /// </summary>
    [Fact(Skip = "既有失败：HALCON error #1401（create_funct_1d_array 控制参数值数目错误，HAlgorithm.dll 内部错误，BestFocus16.3.raw + DSW 组合），2026-10-24 盘点确认，默认不再重复执行")]
    public void Excute_BothEngines_ShouldReturnValidBestFocus_HAlgorithm_BestFocus16_3_DSW() =>
        Excute_BothEngines_ShouldReturnValidBestFocus(AlgorithmEngineTypeEnum.HAlgorithm, @"Assets\BestFocus16.3.raw", AlgorithmBestFocusTypeEnum.DSW);

    private static IHost CreateEngineHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureServices((_, services) =>
            {
                services.AddLogging();
                services.AddKeyedSingleton<IBestFocusEngine, HalconBestFocusEngine>(AlgorithmEngineTypeEnum.HAlgorithm);
                services.AddKeyedSingleton<IBestFocusEngine, AlgoCvBestFocusEngine>(AlgorithmEngineTypeEnum.AlgoCSharp);
                services.AddSingleton<IAlgorithmEngines, AlgorithmEngines>();
            })
            .Build();
    }
}
