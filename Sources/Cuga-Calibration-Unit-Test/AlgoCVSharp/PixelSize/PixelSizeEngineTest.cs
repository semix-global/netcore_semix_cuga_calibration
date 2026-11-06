using AwesomeAssertions;
using Core.Algorithm.Implements;
using Core.Algorithm.Implements.AlgoCSharp;
using Core.Algorithm.Implements.Halcon;
using Core.Algorithm.Implements.Mock.AlgoCSharp;
using Core.Algorithm.Implements.Mock.Halcon;
using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.PixelSize;
using Core.Algorithm.Models.PixelSize;
using Core.Models.Enums.Algorithm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Net.Utilities.Calibration;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Xunit.Abstractions;

namespace CugaCalibrationUnitTest.AlgoCVSharp.PixelSize;

/// <summary>
/// PixelSize 引擎契约测试：经 IAlgorithmEngines.PixelSize(engine) 分发，双引擎对同一 BF jpg 执行 Excute。
/// 真实引擎断言结构有效性；AlgoCSharp 全倍率值锁定见本类 Theory；
/// Mock 引擎断言随机行为结构（原 CalibrationAlgorithmServiceMockImpl.GetPixelSize 行为）。
/// </summary>
public class PixelSizeEngineTest(ITestOutputHelper testOutputHelper)
{
    private const string FiveXFilePath = @"Assets\BFPixelSize\10-10-6-19_5X_20251014.jpg";

    [Theory]
    [InlineData(AlgorithmEngineTypeEnum.HAlgorithm)]
    [InlineData(AlgorithmEngineTypeEnum.AlgoCSharp)]
    public void Excute_BothEngines_ShouldReturnValidPixelSize(AlgorithmEngineTypeEnum engineType)
    {
        using var host = CreateEngineHost();
        var engine = host.Services.GetRequiredService<IAlgorithmEngines>().PixelSize(engineType);

        using var image = new BitmapImage(FiveXFilePath);
        var ret = engine.Excute(image, new PixelSizeEngineParam { StandardMaskSquareSize = AlgorithmStandardMaskSquareSizeEnum.Size100 }, Guid.NewGuid());

        ret.Success.Should().BeTrue(ret.ErrorMsg);
        using var drawingImage = ret.Data!.DrawingImage;

        testOutputHelper.WriteLine($"Engine: {engineType}, PixelSize: {ret.Data.PixelSize}, Angle: {ret.Data.Angle:0.###} deg");

        ret.Data.PixelSize.Width.Should().BeGreaterThan(0);
        ret.Data.PixelSize.Height.Should().BeGreaterThan(0);
        Math.Abs(ret.Data.Angle).Should().BeLessThan(2);
        drawingImage.Should().NotBeNull();
    }

    /// <summary>
    /// BF 全倍率用例：倍率-网格尺寸对应 5x-100um / 10x-50um / 50x-25um / 100x-10um / 150x-10um。
    /// 无 Setting 重载（EngineParams null → 默认实例 PeriodHalf/NMax=0=Auto 扫描窗）。
    /// </summary>
    [Theory]
    [InlineData(@"Assets\BFPixelSize\10-10-6-19_5X_20251014.jpg", AlgorithmStandardMaskSquareSizeEnum.Size100, 0.6863)]
    [InlineData(@"Assets\BFPixelSize\10-10-6-19_10X_20251014.jpg", AlgorithmStandardMaskSquareSizeEnum.Size50, 0.6861)]
    [InlineData(@"Assets\BFPixelSize\10-10-6-19_50X_20251014.jpg", AlgorithmStandardMaskSquareSizeEnum.Size25, 0.0685)]
    [InlineData(@"Assets\BFPixelSize\10-10-6-19_100X_20251014.jpg", AlgorithmStandardMaskSquareSizeEnum.Size10, 0.0347)]
    [InlineData(@"Assets\BFPixelSize\10-10-6-19_150X_20260402.jpg", AlgorithmStandardMaskSquareSizeEnum.Size10, 0.0232)]
    public void AlgoCvEngine_AllMagnifications_ShouldMatchMaskSizeOverPitch(string filePath, AlgorithmStandardMaskSquareSizeEnum maskSize, double expectedUmPerPixel)
    {
        var engine = new AlgoCvPixelSizeEngine(NullLogger<AlgoCvPixelSizeEngine>.Instance);
        using var image = new BitmapImage(filePath);
        var ret = engine.Excute(image, new PixelSizeEngineParam { StandardMaskSquareSize = maskSize }, Guid.NewGuid());

        ret.Success.Should().BeTrue(ret.ErrorMsg);
        using var drawingImage = ret.Data!.DrawingImage;

        testOutputHelper.WriteLine($"BfPixelSize: X={ret.Data.PixelSize.Width:0.000000} um/px, Y={ret.Data.PixelSize.Height:0.000000} um/px, Angle={ret.Data.Angle:0.###} deg");

        ret.Data.PixelSize.Width.Should().BeInRange(expectedUmPerPixel * 0.95, expectedUmPerPixel * 1.05);
        ret.Data.PixelSize.Height.Should().BeInRange(expectedUmPerPixel * 0.95, expectedUmPerPixel * 1.05);
        Math.Abs(ret.Data.Angle).Should().BeLessThan(2);
        drawingImage.Should().NotBeNull();
    }

    [Theory]
    [InlineData(AlgorithmEngineTypeEnum.HAlgorithm)]
    [InlineData(AlgorithmEngineTypeEnum.AlgoCSharp)]
    public void MockEngine_Excute_ShouldReturnRandomResult(AlgorithmEngineTypeEnum engineType)
    {
        using var host = CreateMockEngineHost();
        var engine = host.Services.GetRequiredService<IAlgorithmEngines>().PixelSize(engineType);

        using var image = new BitmapImage(FiveXFilePath);
        var ret = engine.Excute(image, new PixelSizeEngineParam { StandardMaskSquareSize = AlgorithmStandardMaskSquareSizeEnum.Size100 }, Guid.NewGuid());

        ret.Success.Should().BeTrue(ret.ErrorMsg);
        ret.Data!.DrawingImage.Should().NotBeNull();
        ret.Data.PixelSize.Width.Should().BeInRange(1, 9);
        ret.Data.PixelSize.Height.Should().BeInRange(1, 9);
    }

    private static IHost CreateEngineHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureServices((_, services) =>
            {
                services.AddLogging();
                services.AddKeyedSingleton<IPixelSizeEngine, HalconPixelSizeEngine>(AlgorithmEngineTypeEnum.HAlgorithm);
                services.AddKeyedSingleton<IPixelSizeEngine, AlgoCvPixelSizeEngine>(AlgorithmEngineTypeEnum.AlgoCSharp);
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
                services.AddKeyedSingleton<IPixelSizeEngine, HalconPixelSizeEngineMock>(AlgorithmEngineTypeEnum.HAlgorithm);
                services.AddKeyedSingleton<IPixelSizeEngine, AlgoCvPixelSizeEngineMock>(AlgorithmEngineTypeEnum.AlgoCSharp);
                services.AddSingleton<IAlgorithmEngines, AlgorithmEngines>();
            })
            .Build();
    }
}