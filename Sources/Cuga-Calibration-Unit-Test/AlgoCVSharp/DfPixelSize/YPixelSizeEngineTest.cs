using AwesomeAssertions;
using Core.Algorithm.Implements;
using Core.Algorithm.Implements.AlgoCSharp;
using Core.Algorithm.Implements.Halcon;
using Core.Algorithm.Implements.Mock.AlgoCSharp;
using Core.Algorithm.Implements.Mock.Halcon;
using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.YPixelSize;
using Core.Algorithm.Models.YPixelSize;
using Core.Models.Enums.Algorithm;
using Core.Models.Models.Common.DarkField;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.AlgoCSharp;
using Net.Utilities.Calibration;
using Net.Utilities.Graphics.Primitives.Enums.Medias.Imaging;
using Xunit.Abstractions;

namespace CugaCalibrationUnitTest.AlgoCVSharp.DfPixelSize;

/// <summary>
/// YPixelSize 引擎契约测试：经 IAlgorithmEngines.YPixelSize(engine) 分发，双引擎对同一 raw 资产经 DTO 双通道执行 Excute
/// （各引擎各自 raw 底座直读：HAlgorithm→RAWImageFactoryV2 HImage；AlgoCSharp→RAWImageFactoryV3 竖图）。
/// 真实引擎断言结构有效性（AlgoCv 分支的值/投影数量级断言见 PixelSizeServiceTest.YPixelSizeEngine_AlgoCv_DfPixelSize_ShouldMatchEngineDirectResult）；
/// Mock 引擎断言随机行为结构（原 CalibrationAlgorithmServiceMockImpl.GetYPixelSize 行为）。
/// </summary>
public class YPixelSizeEngineTest(ITestOutputHelper testOutputHelper)
{
    /// <summary>YPixelSize raw 资产（DTO 默认 IsToLiner=false，log 域 12bit）</summary>
    private const string RawFilePath = @"Assets\YPixelSize\20260227_2486_0_0_1_short_001000_PMT13-CH3_13.raw";

    [Theory]
    [InlineData(AlgorithmEngineTypeEnum.HAlgorithm)]
    [InlineData(AlgorithmEngineTypeEnum.AlgoCSharp)]
    public void Excute_BothEngines_ShouldReturnValidYPixelSize(AlgorithmEngineTypeEnum engineType)
    {
        using var host = CreateEngineHost();
        var engine = host.Services.GetRequiredService<IAlgorithmEngines>().YPixelSize(engineType);

        using var darkFieldImage = CreateDarkFieldImage();
        var ret = engine.Excute(darkFieldImage, new YPixelSizeEngineParam { StandardMaskSquareSize = AlgorithmStandardMaskSquareSizeEnum.Size10 }, Guid.NewGuid());

        ret.Success.Should().BeTrue(ret.ErrorMsg);
        using var drawingImage = ret.Data!.DrawingImage;

        testOutputHelper.WriteLine($"Engine: {engineType}, YPixelSize: {ret.Data.YPixelSize:0.000000} um/px");

        ret.Data.YPixelSize.Should().BeGreaterThan(0);
        ret.Data.YProjects.Should().NotBeEmpty();
        ret.Data.ResultIndexes.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(AlgorithmEngineTypeEnum.HAlgorithm)]
    [InlineData(AlgorithmEngineTypeEnum.AlgoCSharp)]
    public void MockEngine_Excute_ShouldReturnRandomResult(AlgorithmEngineTypeEnum engineType)
    {
        using var host = CreateMockEngineHost();
        var engine = host.Services.GetRequiredService<IAlgorithmEngines>().YPixelSize(engineType);

        using var darkFieldImage = CreateDarkFieldImage();
        var ret = engine.Excute(darkFieldImage, new YPixelSizeEngineParam { StandardMaskSquareSize = AlgorithmStandardMaskSquareSizeEnum.Size10 }, Guid.NewGuid());

        ret.Success.Should().BeTrue(ret.ErrorMsg);
        ret.Data!.DrawingImage.Should().NotBeNull();
        ret.Data.YProjects.Should().HaveCount(1064);
        ret.Data.ResultIndexes.Should().HaveCount(10);
    }

    /// <summary>半透明红线判定：红通道相对绿/蓝通道的差值下限</summary>
    private const int MarkedLineRedChannelDelta = 32;

    [Fact]
    public void AlgoCvEngine_Excute_DrawingImageShouldHaveMarkedLines()
    {
        using var host = CreateEngineHost();
        var engine = host.Services.GetRequiredService<IAlgorithmEngines>().YPixelSize(AlgorithmEngineTypeEnum.AlgoCSharp);

        using var darkFieldImage = CreateDarkFieldImage();
        using var image = darkFieldImage.GetImage();

        var ret = engine.Excute(darkFieldImage, new YPixelSizeEngineParam { StandardMaskSquareSize = AlgorithmStandardMaskSquareSizeEnum.Size10 }, Guid.NewGuid());

        ret.Success.Should().BeTrue(ret.ErrorMsg);
        ret.Data.Should().NotBeNull();
        var data = ret.Data;

        if (data is null) return;

        using var drawingImage = data.DrawingImage;

        drawingImage.Size.Width.Should().Be(image.Size.Width);
        drawingImage.Size.Height.Should().Be(image.Size.Height);
        drawingImage.ImageInfo.PixelFormatEnum.Should().Be(PixelFormatEnum.Bgr8888);

        using var drawingAlgoImage = drawingImage.ToAlgoCVImage();
        var markedRows = new HashSet<int>();
        for (var row = 0; row < drawingAlgoImage.Height; row++)
        {
            var blue = drawingAlgoImage.GetValue<byte>(0, row, 0);
            var green = drawingAlgoImage.GetValue<byte>(0, row, 1);
            var red = drawingAlgoImage.GetValue<byte>(0, row, 2);
            if (red - green < MarkedLineRedChannelDelta) continue;
            if (red - blue < MarkedLineRedChannelDelta) continue;

            markedRows.Add(row);
        }

        markedRows.Should().NotBeEmpty("投影峰位行应有红色标记线");
        testOutputHelper.WriteLine($"marked rows: {string.Join(", ", markedRows.OrderBy(t => t))}");
        testOutputHelper.WriteLine($"result indexes: {string.Join(", ", data.ResultIndexes)}");

        foreach (var index in data.ResultIndexes)
            markedRows.Any(t => Math.Abs(t - index) <= 5).Should().BeTrue($"ResultIndex {index} 附近应有画线");
    }

    /// <summary>构造 YPixelSize raw 资产 DTO（双通道：RawImageFilePath 供引擎各自底座直读，GetImage() 供显示参照）</summary>
    private static DarkFieldImageDTO CreateDarkFieldImage() => new()
    {
        RawImageFilePath = RawFilePath
    };

    private static IHost CreateEngineHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureServices((_, services) =>
            {
                services.AddLogging();
                services.AddKeyedSingleton<IYPixelSizeEngine, HalconYPixelSizeEngine>(AlgorithmEngineTypeEnum.HAlgorithm);
                services.AddKeyedSingleton<IYPixelSizeEngine, AlgoCvYPixelSizeEngine>(AlgorithmEngineTypeEnum.AlgoCSharp);
                services.AddSingleton<IAlgorithmEngines, AlgorithmEngines>();
            })
            .Build();
    }

    private static IHost CreateMockEngineHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureServices((_, services) =>
            {
                services.AddLogging();
                services.AddKeyedSingleton<IYPixelSizeEngine, HalconYPixelSizeEngineMock>(AlgorithmEngineTypeEnum.HAlgorithm);
                services.AddKeyedSingleton<IYPixelSizeEngine, AlgoCvYPixelSizeEngineMock>(AlgorithmEngineTypeEnum.AlgoCSharp);
                services.AddSingleton<IAlgorithmEngines, AlgorithmEngines>();
            })
            .Build();
    }
}
