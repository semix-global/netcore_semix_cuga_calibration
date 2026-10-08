#define TemplateMatchInteractive

using algocv_sharp;
using AwesomeAssertions;
using CommunityToolkit.Diagnostics;
using Core.Algorithm.Implements;
using Core.Algorithm.Implements.AlgoCSharp;
using Core.Algorithm.Implements.Halcon;
using Core.Algorithm.Implements.Mock.AlgoCSharp;
using Core.Algorithm.Implements.Mock.Halcon;
using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.TemplateMatch;
using Core.Algorithm.Models.TemplateMatch;
using Core.Models;
using Core.Models.Enums.Algorithm;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Recipe.Services;
using Core.Services;
using CugaCalibration.Core;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using Local.SQL.Cache.Providers;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Local.SQL.DB.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Models;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Services;
using SourceGenerator.AssemblyMetadata;
using System.IO;
using Net.Utilities.Calibration;
using Xunit.Abstractions;
using AlgoImage = algocv_sharp.Image;
using CalibrationBitmapImage = Net.Utilities.Graphics.Primitives.Medias.Imaging.BitmapImage;
using Point = Net.Utilities.Models.Geometries.Point;
using Rect = Net.Utilities.Models.Geometries.Rect;

#if TemplateMatchInteractive
using CanvasViewer.View;
using Net.Utilities.WPF.Converters.SingleValue;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
#endif

namespace CugaCalibrationUnitTest.AlgoCVSharp.Template;

/// <summary>
/// algocv_sharp.TemplateMatchEngine 用法：
/// 1. <c>new TemplateMatchConfig(TmCcoeffNormed, pyramidLevels)</c>
/// 2. <c>using var engine = new TemplateMatchEngine(config)</c>
/// 3. <c>engine.SetTemplate(templ)</c>（内部 clone，模板图可随后释放）
/// 4. <c>var result = engine.Match(scene)</c>，<c>position_x/y</c> 为模板几何中心
/// 缩放/旋转暂未启用，<c>scale=1</c>、<c>angle=0</c>。单实例不可并发。
/// </summary>
public sealed class AlgoCVSharpTemplateMatchTest(ITestOutputHelper output) : IDisposable
{
    private const string TemplateAssetDirectory = @"Assets\Template";
    private const string BrightFieldImageFileName = "BF.jpg";
    private const string DarkFieldRawFileName = "DF.raw";
    private const string DarkFieldImageFileName = "DF.jpg";
    private const string BrightFieldTemplateImageFileName = "BF_Template.jpg";
    private const string DarkFieldTemplateImageFileName = "DF_Template.jpg";
    private const string BrightFieldMatchImageFileName = "BF_Match.jpg";
    private const string DarkFieldMatchImageFileName = "DF_Match.jpg";

#if TemplateMatchInteractive
    private static readonly Lock StaSync = new();
    private static Dispatcher? _staDispatcher;
    private static Thread? _staThread;
    private static int _staShutdownStarted;
    private static bool _imeHandlerRegistered;
    private static bool _exitHooksRegistered;

    private Application? _application;
    private IHost? _host;
#endif

#if TemplateMatchInteractive
    private void DisposeHost()
    {
        if (_host is null)
        {
            return;
        }

        HostApplication.GetRequiredService<IFreeSql>().Dispose();
        HostApplication.GetRequiredService<ICacheProvider>().Dispose();
        HostApplication.GetKeyedService<ICacheProvider>(CalibrationConstantsHelper.RecipeDbKey).Dispose();
        _host.Dispose();
        _host = null;
    }
#endif

    [Fact]
    public void TranslationExact_CcoeffNormed_ShouldHitTemplateCenter()
    {
        const int width = 640;
        const int height = 480;
        const int size = 64;
        const int tx = 123;
        const int ty = 87;

        using var templ = CreatePatternTemplate(size);
        using var image = new AlgoImage(width, height, 1, ImageDataType.UInt8);
        PastePattern(image, tx, ty, size);

        var result = Match(templ, image, TemplateMatchMethod.TmCcoeffNormed, pyramidLevels: 0);

        AssertExactHit(result, tx + size / 2.0, ty + size / 2.0, expectedScore: 1.0, scoreTolerance: 1e-6);
    }

    [Fact]
    public void TranslationExact_SqdiffNormed_ShouldHitTemplateCenterWithZeroScore()
    {
        const int size = 64;
        const int tx = 410;
        const int ty = 260;

        using var templ = CreatePatternTemplate(size);
        using var image = new AlgoImage(800, 600, 1, ImageDataType.UInt8);
        PastePattern(image, tx, ty, size);

        var result = Match(templ, image, TemplateMatchMethod.TmSqdiffNormed, pyramidLevels: 0);

        AssertExactHit(result, tx + size / 2.0, ty + size / 2.0, expectedScore: 0.0, scoreTolerance: 1e-6);
    }

    [Fact]
    public void TranslationExact_PyramidLevels3_ShouldHitSameCenter()
    {
        const int size = 64;
        const int tx = 123;
        const int ty = 87;

        using var templ = CreatePatternTemplate(size);
        using var image = new AlgoImage(640, 480, 1, ImageDataType.UInt8);
        PastePattern(image, tx, ty, size);

        var result = Match(templ, image, TemplateMatchMethod.TmCcoeffNormed, pyramidLevels: 3);

        AssertExactHit(result, tx + size / 2.0, ty + size / 2.0, expectedScore: 1.0, scoreTolerance: 1e-6);
    }

    [Fact]
    public void Match_TemplateLargerThanScene_ShouldThrow()
    {
        var act = () =>
        {
            using var image = new AlgoImage(32, 32, 1, ImageDataType.UInt8);
            using var templateImage = new AlgoImage(64, 64, 1, ImageDataType.UInt8);
            return Match(templateImage, image, TemplateMatchMethod.TmCcoeffNormed, pyramidLevels: 0);
        };

        act.Should().Throw<Exception>();
    }

    [Theory]
    [InlineData(AlgorithmEngineTypeEnum.AlgoCSharp)]
    [InlineData(AlgorithmEngineTypeEnum.HAlgorithm)]
    public void Engine_GenerateThenMatch_ShouldHitTemplateCenter(AlgorithmEngineTypeEnum engineType)
    {
        using var host = CreateTemplateMatchEngineHost(useMock: false);
        var engine = host.Services.GetRequiredService<IAlgorithmEngines>().TemplateMatch(engineType);
        ExecuteGenerateThenMatch(engine, expectPatternCenter: true, CreateEngineParams(engineType));
    }

    [Theory]
    [InlineData(AlgorithmEngineTypeEnum.AlgoCSharp)]
    [InlineData(AlgorithmEngineTypeEnum.HAlgorithm)]
    public void Engine_ReadThenMatch_ShouldHitTemplateCenter(AlgorithmEngineTypeEnum engineType)
    {
        using var host = CreateTemplateMatchEngineHost(useMock: false);
        var engine = host.Services.GetRequiredService<IAlgorithmEngines>().TemplateMatch(engineType);
        ExecuteReadThenMatch(engine, expectPatternCenter: true, CreateEngineParams(engineType));
    }

    [Theory]
    [InlineData(AlgorithmEngineTypeEnum.AlgoCSharp)]
    [InlineData(AlgorithmEngineTypeEnum.HAlgorithm)]
    public void MockEngine_GenerateThenMatch_ShouldReturnImageCenter(AlgorithmEngineTypeEnum engineType)
    {
        using var host = CreateTemplateMatchEngineHost(useMock: true);
        var engine = host.Services.GetRequiredService<IAlgorithmEngines>().TemplateMatch(engineType);
        ExecuteGenerateThenMatch(engine, expectPatternCenter: false, CreateEngineParams(engineType));
    }

    [Theory]
    [InlineData(AlgorithmEngineTypeEnum.AlgoCSharp)]
    [InlineData(AlgorithmEngineTypeEnum.HAlgorithm)]
    public void MockEngine_ReadThenMatch_ShouldReturnImageCenter(AlgorithmEngineTypeEnum engineType)
    {
        using var host = CreateTemplateMatchEngineHost(useMock: true);
        var engine = host.Services.GetRequiredService<IAlgorithmEngines>().TemplateMatch(engineType);
        ExecuteReadThenMatch(engine, expectPatternCenter: false, CreateEngineParams(engineType));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AlgoCSharpEngine_PyramidLevels3_GenerateThenMatch(bool useMock)
    {
        using var host = CreateTemplateMatchEngineHost(useMock);
        var engine = host.Services.GetRequiredService<IAlgorithmEngines>().TemplateMatch(AlgorithmEngineTypeEnum.AlgoCSharp);
        ExecuteGenerateThenMatch(
            engine,
            expectPatternCenter: useMock == false,
            new AlgoCvTemplateMatchParams { PyramidLevels = 3 });
    }

    [Fact]
    public void AlgoCSharpEngineMock_CustomMethod_GenerateThenMatch()
    {
        using var host = CreateTemplateMatchEngineHost(useMock: true);
        var engine = host.Services.GetRequiredService<IAlgorithmEngines>().TemplateMatch(AlgorithmEngineTypeEnum.AlgoCSharp);
        ExecuteGenerateThenMatch(
            engine,
            expectPatternCenter: false,
            new AlgoCvTemplateMatchParams { Method = TemplateMatchMethod.TmCcorrNormed, PyramidLevels = 2 });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Engine_WrongEngineParams_ShouldFail(bool useMock)
    {
        using var host = CreateTemplateMatchEngineHost(useMock);
        var engines = host.Services.GetRequiredService<IAlgorithmEngines>();
        var algoCv = engines.TemplateMatch(AlgorithmEngineTypeEnum.AlgoCSharp);
        var halcon = engines.TemplateMatch(AlgorithmEngineTypeEnum.HAlgorithm);

        using var scene = new AlgoImage(PatternSceneWidth, PatternSceneHeight, 1, ImageDataType.UInt8);
        PastePattern(scene, PatternOffsetX, PatternOffsetY, PatternSize);
        using var image = scene.ToBitmapImage();
        var templateFilePath = Path.Combine(Path.GetTempPath(), $"TemplateMatchWrongParams_{Guid.NewGuid():N}");

        var algoGenerate = algoCv.Generate(image, new GenerateTemplateParam
        {
            Roi = new Rect(PatternOffsetX, PatternOffsetY, PatternSize, PatternSize),
            TemplateFilePath = templateFilePath,
            EngineParams = new HalconTemplateMatchParams()
        }, Guid.NewGuid());
        algoGenerate.Success.Should().BeFalse();
        algoGenerate.ErrorMsg.Should().Contain(nameof(AlgoCvTemplateMatchParams));

        var algoRead = algoCv.Read(new ReadTemplateParam
        {
            TemplateFilePath = templateFilePath,
            EngineParams = new HalconTemplateMatchParams()
        }, Guid.NewGuid());
        algoRead.Success.Should().BeFalse();
        algoRead.ErrorMsg.Should().Contain(nameof(AlgoCvTemplateMatchParams));

        var halconGenerate = halcon.Generate(image, new GenerateTemplateParam
        {
            Roi = new Rect(PatternOffsetX, PatternOffsetY, PatternSize, PatternSize),
            TemplateFilePath = templateFilePath,
            EngineParams = new AlgoCvTemplateMatchParams { PyramidLevels = 3 }
        }, Guid.NewGuid());
        halconGenerate.Success.Should().BeFalse();
        halconGenerate.ErrorMsg.Should().Contain(nameof(HalconTemplateMatchParams));

        var halconRead = halcon.Read(new ReadTemplateParam
        {
            TemplateFilePath = templateFilePath,
            EngineParams = new AlgoCvTemplateMatchParams()
        }, Guid.NewGuid());
        halconRead.Success.Should().BeFalse();
        halconRead.ErrorMsg.Should().Contain(nameof(HalconTemplateMatchParams));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Engine_Match_ForeignModel_ShouldFail(bool useMock)
    {
        using var host = CreateTemplateMatchEngineHost(useMock);
        var engines = host.Services.GetRequiredService<IAlgorithmEngines>();
        var halcon = engines.TemplateMatch(AlgorithmEngineTypeEnum.HAlgorithm);
        var algoCv = engines.TemplateMatch(AlgorithmEngineTypeEnum.AlgoCSharp);

        using var scene = new AlgoImage(PatternSceneWidth, PatternSceneHeight, 1, ImageDataType.UInt8);
        PastePattern(scene, PatternOffsetX, PatternOffsetY, PatternSize);
        using var image = scene.ToBitmapImage();
        var templateFilePath = Path.Combine(Path.GetTempPath(), $"TemplateMatchForeign_{Guid.NewGuid():N}");

        try
        {
            var halconGenerate = halcon.Generate(image, new GenerateTemplateParam
            {
                Roi = new Rect(PatternOffsetX, PatternOffsetY, PatternSize, PatternSize),
                TemplateFilePath = templateFilePath
            }, Guid.NewGuid());
            halconGenerate.Success.Should().BeTrue(halconGenerate.ErrorMsg);
            AssertGenerateImages(templateFilePath);
#pragma warning disable IDISP007
            using var halconModel = halconGenerate.Data!;
#pragma warning restore IDISP007

            var algoMatch = algoCv.Match(image, new MatchTemplateParam
            {
                Model = halconModel
            }, Guid.NewGuid());
            algoMatch.Success.Should().BeFalse();
            algoMatch.ErrorMsg.Should().Contain("AlgoCvTemplateMatchModel");

            var algoGenerate = algoCv.Generate(image, new GenerateTemplateParam
            {
                Roi = new Rect(PatternOffsetX, PatternOffsetY, PatternSize, PatternSize),
                TemplateFilePath = templateFilePath + "_algo"
            }, Guid.NewGuid());
            algoGenerate.Success.Should().BeTrue(algoGenerate.ErrorMsg);
            AssertGenerateImages(templateFilePath + "_algo");
#pragma warning disable IDISP007
            using var algoModel = algoGenerate.Data!;
#pragma warning restore IDISP007

            var halconMatch = halcon.Match(image, new MatchTemplateParam
            {
                Model = algoModel
            }, Guid.NewGuid());
            halconMatch.Success.Should().BeFalse();
            halconMatch.ErrorMsg.Should().Contain("HalconTemplateMatchModel");
        }
        finally
        {
            DeleteTemplateArtifacts(templateFilePath);
            DeleteTemplateArtifacts(templateFilePath + "_algo");
        }
    }

#if TemplateMatchInteractive
    [Fact]
    public void BrightField_InteractiveRoi_ShouldSelfMatchNearRoiCenter()
    {
        RunOnSta(() =>
        {
            var imagePath = GetTemplateAssetPath(BrightFieldImageFileName);
            File.Exists(imagePath).Should().BeTrue($"sample image missing: {imagePath}");

            var roi = SelectRoi(imagePath);
            MatchCroppedRoiAndShow(
                imagePath,
                roi,
                GetTemplateAssetPath(BrightFieldTemplateImageFileName),
                GetTemplateAssetPath(BrightFieldMatchImageFileName),
                nameof(BrightField_InteractiveRoi_ShouldSelfMatchNearRoiCenter));
        });
    }

    [Fact]
    public void DarkField_InteractiveRoi_ShouldSelfMatchNearRoiCenter()
    {
        RunOnSta(() =>
        {
            var rawPath = GetTemplateAssetPath(DarkFieldRawFileName);
            File.Exists(rawPath).Should().BeTrue($"sample image missing: {rawPath}");

            var scenePath = GetTemplateAssetPath(DarkFieldImageFileName);
            using (var scene8 = LoadDarkFieldAsUInt8(rawPath))
            {
                scene8.Save(scenePath);
            }

            var roi = SelectRoi(scenePath);
            MatchCroppedRoiAndShow(
                scenePath,
                roi,
                GetTemplateAssetPath(DarkFieldTemplateImageFileName),
                GetTemplateAssetPath(DarkFieldMatchImageFileName),
                nameof(DarkField_InteractiveRoi_ShouldSelfMatchNearRoiCenter));
        });
    }

    [Theory]
    [InlineData(AlgorithmEngineTypeEnum.AlgoCSharp)]
    [InlineData(AlgorithmEngineTypeEnum.HAlgorithm)]
    public void BrightField_InteractiveRoi_EngineGenerateThenMatch(AlgorithmEngineTypeEnum engineType)
    {
        RunOnSta(() =>
        {
            var imagePath = GetTemplateAssetPath(BrightFieldImageFileName);
            File.Exists(imagePath).Should().BeTrue($"sample image missing: {imagePath}");

            var roi = SelectRoi(imagePath);
            MatchWithEngineAndShow(
                engineType,
                imagePath,
                roi,
                GetTemplateAssetPath($"BF_{engineType}_Template.jpg"),
                GetTemplateAssetPath($"BF_{engineType}_Match.jpg"),
                $"{nameof(BrightField_InteractiveRoi_EngineGenerateThenMatch)}_{engineType}");
        });
    }

    [Theory]
    [InlineData(AlgorithmEngineTypeEnum.AlgoCSharp)]
    [InlineData(AlgorithmEngineTypeEnum.HAlgorithm)]
    public void DarkField_InteractiveRoi_EngineGenerateThenMatch(AlgorithmEngineTypeEnum engineType)
    {
        RunOnSta(() =>
        {
            var rawPath = GetTemplateAssetPath(DarkFieldRawFileName);
            File.Exists(rawPath).Should().BeTrue($"sample image missing: {rawPath}");

            var scenePath = GetTemplateAssetPath(DarkFieldImageFileName);
            using (var scene8 = LoadDarkFieldAsUInt8(rawPath))
            {
                scene8.Save(scenePath);
            }

            var roi = SelectRoi(scenePath);
            MatchWithEngineAndShow(
                engineType,
                scenePath,
                roi,
                GetTemplateAssetPath($"DF_{engineType}_Template.jpg"),
                GetTemplateAssetPath($"DF_{engineType}_Match.jpg"),
                $"{nameof(DarkField_InteractiveRoi_EngineGenerateThenMatch)}_{engineType}");
        });
    }
#endif

    #region private

    private const int PatternSceneWidth = 640;
    private const int PatternSceneHeight = 480;
    private const int PatternSize = 64;
    private const int PatternOffsetX = 123;
    private const int PatternOffsetY = 87;

    private static IHost CreateTemplateMatchEngineHost(bool useMock)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureServices((_, services) =>
            {
                services.AddLogging();
                if (useMock)
                {
                    services.AddKeyedSingleton<ITemplateMatchEngine, HalconTemplateMatchEngineMock>(AlgorithmEngineTypeEnum.HAlgorithm);
                    services.AddKeyedSingleton<ITemplateMatchEngine, AlgoCvTemplateMatchEngineMock>(AlgorithmEngineTypeEnum.AlgoCSharp);
                }
                else
                {
                    services.AddKeyedSingleton<ITemplateMatchEngine, HalconTemplateMatchEngine>(AlgorithmEngineTypeEnum.HAlgorithm);
                    services.AddKeyedSingleton<ITemplateMatchEngine, AlgoCvTemplateMatchEngine>(AlgorithmEngineTypeEnum.AlgoCSharp);
                }

                services.AddSingleton<IAlgorithmEngines, AlgorithmEngines>();
            })
            .Build();
    }

    private static TemplateMatchEngineParams CreateEngineParams(AlgorithmEngineTypeEnum engineType, int pyramidLevels = 0) =>
        engineType switch
        {
            AlgorithmEngineTypeEnum.AlgoCSharp => new AlgoCvTemplateMatchParams { PyramidLevels = pyramidLevels },
            AlgorithmEngineTypeEnum.HAlgorithm => new HalconTemplateMatchParams { TemplateType = AlgorithmTemplateTypeEnum.Ncc },
            _ => throw new ArgumentOutOfRangeException(nameof(engineType), engineType, null)
        };

    private void ExecuteGenerateThenMatch(
        ITemplateMatchEngine engine,
        bool expectPatternCenter,
        TemplateMatchEngineParams? engineParams = null)
    {
        using var scene = new AlgoImage(PatternSceneWidth, PatternSceneHeight, 1, ImageDataType.UInt8);
        PastePattern(scene, PatternOffsetX, PatternOffsetY, PatternSize);
        using var bitmap = scene.ToBitmapImage();
        var templateFilePath = Path.Combine(Path.GetTempPath(), $"TemplateMatchEngine_{Guid.NewGuid():N}");

        try
        {
            var generate = engine.Generate(bitmap, new GenerateTemplateParam
            {
                Roi = new Rect(PatternOffsetX, PatternOffsetY, PatternSize, PatternSize),
                TemplateFilePath = templateFilePath,
                EngineParams = engineParams
            }, Guid.NewGuid());
            generate.Success.Should().BeTrue(generate.ErrorMsg);
            generate.Data.Should().NotBeNull();
            AssertGenerateImages(templateFilePath);
#pragma warning disable IDISP007
            using var model = generate.Data!;
#pragma warning restore IDISP007

            var match = engine.Match(bitmap, new MatchTemplateParam
            {
                Model = model
            }, Guid.NewGuid());
            AssertInterfaceMatch(match, bitmap, expectPatternCenter);
        }
        finally
        {
            DeleteTemplateArtifacts(templateFilePath);
        }
    }

    private void ExecuteReadThenMatch(
        ITemplateMatchEngine engine,
        bool expectPatternCenter,
        TemplateMatchEngineParams? engineParams = null)
    {
        using var scene = new AlgoImage(PatternSceneWidth, PatternSceneHeight, 1, ImageDataType.UInt8);
        PastePattern(scene, PatternOffsetX, PatternOffsetY, PatternSize);
        using var bitmap = scene.ToBitmapImage();
        var templateFilePath = Path.Combine(Path.GetTempPath(), $"TemplateMatchEngineRead_{Guid.NewGuid():N}");

        try
        {
            var generate = engine.Generate(bitmap, new GenerateTemplateParam
            {
                Roi = new Rect(PatternOffsetX, PatternOffsetY, PatternSize, PatternSize),
                TemplateFilePath = templateFilePath,
                EngineParams = engineParams
            }, Guid.NewGuid());
            generate.Success.Should().BeTrue(generate.ErrorMsg);
            generate.Data.Should().NotBeNull();
            AssertGenerateImages(templateFilePath);
            generate.Data!.Dispose();

            var read = engine.Read(new ReadTemplateParam
            {
                TemplateFilePath = templateFilePath,
                EngineParams = engineParams
            }, Guid.NewGuid());
            read.Success.Should().BeTrue(read.ErrorMsg);
            read.Data.Should().NotBeNull();
#pragma warning disable IDISP007
            using var model = read.Data!;
#pragma warning restore IDISP007

            var match = engine.Match(bitmap, new MatchTemplateParam
            {
                Model = model
            }, Guid.NewGuid());
            AssertInterfaceMatch(match, bitmap, expectPatternCenter);
        }
        finally
        {
            DeleteTemplateArtifacts(templateFilePath);
        }
    }

    private void AssertInterfaceMatch(AlgorithmResultRet<MatchTemplateResult> match, CalibrationBitmapImage image, bool expectPatternCenter)
    {
        match.Success.Should().BeTrue(match.ErrorMsg);
        match.Data.Should().NotBeNull();
        var data = match.Data!;
        data.EngineResult.Should().BeNull();

        var expectedX = expectPatternCenter ? PatternOffsetX + PatternSize / 2.0 : image.Width / 2d;
        var expectedY = expectPatternCenter ? PatternOffsetY + PatternSize / 2.0 : image.Height / 2d;
        var expectedOffsetX = expectedX - image.Width / 2d;
        var expectedOffsetY = -(expectedY - image.Height / 2d);

        output.WriteLine(
            $"engine match center=({data.MarkPoint.X:0.###}, {data.MarkPoint.Y:0.###}), score={data.Score:0.######}, offset=({data.Offset.X:0.###}, {data.Offset.Y:0.###})");

        data.MarkPoint.X.Should().BeApproximately(expectedX, 0.5);
        data.MarkPoint.Y.Should().BeApproximately(expectedY, 0.5);
        data.Offset.X.Should().BeApproximately(expectedOffsetX, 0.5);
        data.Offset.Y.Should().BeApproximately(expectedOffsetY, 0.5);
        if (expectPatternCenter)
        {
            data.Score.Should().BeGreaterThan(0.95);
            data.Angle.Should().BeApproximately(0.0, 1e-3);
        }
        else
        {
            data.Score.Should().BeApproximately(1.0, 1e-6);
            data.Angle.Should().Be(0.0);
        }
    }

    private static void AssertGenerateImages(string templateFilePath)
    {
        File.Exists(CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath)).Should().BeTrue();
        File.Exists(CalibrationConstantsHelper.TemplatePathToOriginImagePath(templateFilePath)).Should().BeTrue();
    }

    private static void DeleteTemplateArtifacts(string templateFilePath)
    {
        foreach (var path in new[]
                 {
                     CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath),
                     CalibrationConstantsHelper.TemplatePathToOriginImagePath(templateFilePath),
                     AlgorithmTemplateTypeEnum.Ncc.ToFullFilePath(templateFilePath),
                     AlgorithmTemplateTypeEnum.Sharpe.ToFullFilePath(templateFilePath)
                 })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static string GetTemplateAssetPath(string fileName)
    {
        var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TemplateAssetDirectory);
        Directory.CreateDirectory(directory);
        return Path.GetFullPath(Path.Combine(directory, fileName));
    }

    private static AlgoImage CreatePatternTemplate(int templSize)
    {
        var templ = new AlgoImage(templSize, templSize, 1, ImageDataType.UInt8);
        for (var y = 0; y < templSize; y++)
        {
            for (var x = 0; x < templSize; x++)
            {
                templ.SetValue(x, y, (byte)((x * 3 + y * 7) % 256));
            }
        }

        return templ;
    }

    private static void PastePattern(AlgoImage scene, int offsetX, int offsetY, int templSize)
    {
        for (var y = 0; y < templSize; y++)
        {
            for (var x = 0; x < templSize; x++)
            {
                scene.SetValue(offsetX + x, offsetY + y, (byte)((x * 3 + y * 7) % 256));
            }
        }
    }

    private static TemplateMatchResult Match(
        AlgoImage templ,
        AlgoImage scene,
        TemplateMatchMethod method,
        int pyramidLevels)
    {
        var config = new TemplateMatchConfig(method, pyramidLevels);
        using var engine = new TemplateMatchEngine(config);
        engine.SetTemplate(templ);
        return engine.Match(scene);
    }

    private void AssertExactHit(
        TemplateMatchResult result,
        double expectedCenterX,
        double expectedCenterY,
        double expectedScore,
        double scoreTolerance)
    {
        output.WriteLine(
            $"match center=({result.position_x:0.###}, {result.position_y:0.###}), score={result.score:0.######}, scale={result.scale}, angle={result.angle}");

        result.position_x.Should().BeApproximately(expectedCenterX, 0.5);
        result.position_y.Should().BeApproximately(expectedCenterY, 0.5);
        result.score.Should().BeApproximately(expectedScore, scoreTolerance);
        result.scale.Should().Be(1.0);
        result.angle.Should().Be(0.0);
    }

    private static AlgoImage LoadDarkFieldAsUInt8(string rawPath)
    {
        var rawBytes = File.ReadAllBytes(rawPath);
        using var raw = new AlgoImage(rawBytes);
        using var linear = algocv_sharp.ImageProc.LogToLinear(raw);
        return algocv_sharp.ImageProc.ScaleImageMax(linear, 255, ImageDataType.UInt8);
    }

    #endregion


#if TemplateMatchInteractive

    #region Display

    private Rect SelectRoi(string imageFilePath)
    {
        EnsureHost();
        var windowManagerService = HostApplication.GetRequiredService<IWindowManagerService>();
        var dialogSettings = new Dictionary<string, object?>
        {
            ["ShowInTaskbar"] = true,
            ["Topmost"] = true
        };

        var createROIWindowViewModel = HostApplication.GetRequiredService<CreateRoiWindowViewModel>();

        createROIWindowViewModel.ImageFilePath = imageFilePath;
        createROIWindowViewModel.AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum.Size128;

        output.WriteLine($"Show CreateROIWindow: {imageFilePath}");

        windowManagerService.ShowDialog(createROIWindowViewModel, dialogSettings, isSendFrontWindow: false);

        var roi = createROIWindowViewModel.Rect;
        output.WriteLine($"ROI={roi}, ImageFilePath={createROIWindowViewModel.ImageFilePath}");
        roi.Width.Should().BeGreaterThanOrEqualTo(32);
        roi.Height.Should().BeGreaterThanOrEqualTo(32);
        return roi;
    }

    private void MatchCroppedRoiAndShow(
        string imagePath,
        Rect roi,
        string croppedTemplateImagePath,
        string matchResultImagePath,
        string windowTitle)
    {
        using var algoImage = new AlgoImage(imagePath);
        var cropX = Convert.ToInt32(roi.X);
        var cropY = Convert.ToInt32(roi.Y);
        var cropWidth = Convert.ToInt32(roi.Width);
        var cropHeight = Convert.ToInt32(roi.Height);

        using var templateImage = algoImage.Crop(cropX, cropY, cropWidth, cropHeight);
        templateImage.Save(croppedTemplateImagePath);

        var result = Match(templateImage, algoImage, TemplateMatchMethod.TmCcoeffNormed, pyramidLevels: 3);
        var expectedCenterX = roi.X + roi.Width / 2.0;
        var expectedCenterY = roi.Y + roi.Height / 2.0;

        output.WriteLine(
            $"scene={algoImage.Width}x{algoImage.Height}c{algoImage.Channels}, ROI={roi}, match=({result.position_x:0.###}, {result.position_y:0.###}), score={result.score:0.###}");

        result.position_x.Should().BeApproximately(expectedCenterX, 2);
        result.position_y.Should().BeApproximately(expectedCenterY, 2);
        result.score.Should().BeGreaterThan(0.9);
        result.scale.Should().Be(1.0);
        result.angle.Should().Be(0.0);

        DrawMatchResultImage(algoImage, roi, new Point(result.position_x, result.position_y), matchResultImagePath);
        ShowMatchResultWindow(windowTitle, matchResultImagePath, roi, new Point(result.position_x, result.position_y), result.score, result.angle, result.scale);
    }

    private void MatchWithEngineAndShow(
        AlgorithmEngineTypeEnum engineType,
        string imagePath,
        Rect roi,
        string croppedTemplateImagePath,
        string matchResultImagePath,
        string windowTitle)
    {
        using var engineHost = CreateTemplateMatchEngineHost(useMock: false);
        var engine = engineHost.Services.GetRequiredService<IAlgorithmEngines>().TemplateMatch(engineType);

        using var algoImage = new AlgoImage(imagePath);
        using var bitmap = algoImage.ToBitmapImage();
        var templateFilePath = Path.Combine(
            Path.GetDirectoryName(croppedTemplateImagePath)!,
            Path.GetFileNameWithoutExtension(croppedTemplateImagePath).Replace("_Template", string.Empty));

        var generate = engine.Generate(bitmap, new GenerateTemplateParam
        {
            Roi = roi,
            TemplateFilePath = templateFilePath,
            EngineParams = CreateEngineParams(engineType)
        }, Guid.NewGuid());
        generate.Success.Should().BeTrue(generate.ErrorMsg);
        generate.Data.Should().NotBeNull();
        using (var cropped = algoImage.Crop((int)roi.X, (int)roi.Y, (int)roi.Width, (int)roi.Height))
        {
            cropped.Save(croppedTemplateImagePath);
        }

#pragma warning disable IDISP007
        using var model = generate.Data!;
#pragma warning restore IDISP007

        var match = engine.Match(bitmap, new MatchTemplateParam
        {
            Model = model
        }, Guid.NewGuid());
        match.Success.Should().BeTrue(match.ErrorMsg);
        match.Data.Should().NotBeNull();
        var data = match.Data!;

        var expectedCenterX = roi.X + roi.Width / 2.0;
        var expectedCenterY = roi.Y + roi.Height / 2.0;
        output.WriteLine(
            $"engine={engineType}, scene={algoImage.Width}x{algoImage.Height}c{algoImage.Channels}, ROI={roi}, match=({data.MarkPoint.X:0.###}, {data.MarkPoint.Y:0.###}), score={data.Score:0.###}, angle={data.Angle:0.###}");

        data.MarkPoint.X.Should().BeApproximately(expectedCenterX, 2);
        data.MarkPoint.Y.Should().BeApproximately(expectedCenterY, 2);
        data.Score.Should().BeGreaterThan(0.9);
        data.Angle.Should().BeApproximately(0.0, 1e-3);

        DrawMatchResultImage(algoImage, roi, data.MarkPoint, matchResultImagePath);
        ShowMatchResultWindow(windowTitle, matchResultImagePath, roi, data.MarkPoint, data.Score, data.Angle);
    }

    // 与生产 CIBViewModel 一致：BitmapImage.ToHImage 后 DrawRect + DrawCrossLine，再存盘。
    private static void DrawMatchResultImage(
        AlgoImage image,
        Rect roi,
        Point markPoint,
        string matchResultImagePath)
    {
        using var bitmapImage = image.ToBitmapImage();
        using var hImage = bitmapImage.ToHImage();
        using var withRoi = hImage.DrawRect(roi);
        using var withCross = withRoi.DrawCrossLine(markPoint);
        withCross.Save(matchResultImagePath);
    }

    // HALCON 画好 ROI/十字后，用 CanvasView.BackgroundImage 显示，属性与 Step1View 模板图一致。
    private static void ShowMatchResultWindow(
        string title,
        string matchResultImagePath,
        Rect roi,
        Point markPoint,
        double score,
        double angle,
        double scale = 1.0)
    {
        RunOnSta(() =>
        {
            var window = new Window
            {
                Title = title,
                WindowState = WindowState.Maximized,
                ShowInTaskbar = true,
                Topmost = true
            };
            InputMethod.SetIsInputMethodEnabled(window, false);

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var summary = new TextBlock
            {
                Margin = new Thickness(12),
                FontSize = 16,
                Text = $"ROI={roi}{Environment.NewLine}Match center=({markPoint.X:0.###}, {markPoint.Y:0.###})  score={score:0.####}  scale={scale:0.##}  angle={angle:0.##}{Environment.NewLine}{matchResultImagePath}"
            };
            Grid.SetRow(summary, 0);

            var canvasView = new CanvasView
            {
                Margin = new Thickness(5),
                BackgroundColor = Color.FromRgb(0x0A, 0x0A, 0x0A),
                Interactive = true,
                ShowAxes = false,
                ShowCursor = false,
                ShowGrid = false,
                BackgroundImage = StringToBitmapSourceConverter.Instance.Convert(
                    matchResultImagePath,
                    typeof(BitmapSource),
                    null,
                    CultureInfo.CurrentCulture) as BitmapSource
            };
            Grid.SetRow(canvasView, 1);

            grid.Children.Add(summary);
            grid.Children.Add(canvasView);
            window.Content = grid;
            window.Closed += (_, _) => canvasView.Dispose();
            window.ShowDialog();
        });
    }

    #endregion

    #region Initial&Host

    private void EnsureHost()
    {
        if (_host is not null)
        {
            return;
        }

        Guard.IsTrue(
            Thread.CurrentThread.GetApartmentState() == ApartmentState.STA,
            "CreateDarkImageTemplateWindow must create the Application and call ShowDialog on an STA thread");

        var application = EnsureWpfApplication();
        _application = application;

#pragma warning disable IDE0079
#pragma warning disable IDISP003
#pragma warning disable IDISP004

        _host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureServices((context, services) =>
            {
                services
                    .Configure<ApplicationSetting>(context.Configuration.GetSection(BaseApplicationSetting.AppSetting))
                    .AddMvvmService(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, CugaCalibrationUnitTestAssemblyMetadata.Version, application, context.HostingEnvironment)
                    .AddSqlDbContext(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value.SqlDbDataSource, context.HostingEnvironment)
                    .AddCacheContext(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value.NosqlDbDataSource, sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, context.HostingEnvironment)
                    .AddNetUtilitiesCalibrationService(context.HostingEnvironment)
                    .AddRecipeService(context.HostingEnvironment)
                    .AddKeyedCacheContext(CalibrationConstantsHelper.RecipeDbKey, sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, context.HostingEnvironment)
                    .AddCoreService(context.HostingEnvironment)
                    .AddApplication(context.HostingEnvironment);
            })
            .UseEnvironment(Environments.Development)
            .Build()
            .ConfigureHostApplication();

#pragma warning restore IDISP004
#pragma warning restore IDISP003
#pragma warning restore IDE0079
    }

    private static Application EnsureWpfApplication()
    {
        if (Application.Current is { } existing)
        {
            existing.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            if (existing.Dispatcher.CheckAccess() == false)
            {
                throw new InvalidOperationException(
                    "The WPF application has been created on another thread, so ShowDialog will be sent to that Dispatcher and deadlock. Please run this interactive test separately.。");
            }

            EnsureApplicationResources(existing);
            RegisterDisableImeOnWindowLoaded();
            return existing;
        }

        var app = new CugaCalibration.App();
        app.InitializeComponent();
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        RegisterDisableImeOnWindowLoaded();
        return app;
    }

    private static void RegisterDisableImeOnWindowLoaded()
    {
        if (_imeHandlerRegistered)
        {
            return;
        }

        _imeHandlerRegistered = true;
        EventManager.RegisterClassHandler(
            typeof(Window),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((_, args) =>
            {
                if (args.Source is Window window)
                {
                    InputMethod.SetIsInputMethodEnabled(window, false);
                }
            }));
    }

    private static void EnsureApplicationResources(Application application)
    {
        if (application.Resources.MergedDictionaries.Count > 0)
        {
            return;
        }

        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml", UriKind.Absolute)
        });
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/HandyControl;component/Themes/Theme.xaml", UriKind.Absolute)
        });
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Net.Utilities.WPF.Converters;component/Themes/allconverters.xaml", UriKind.Absolute)
        });
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Net.Utilities.WPF;component/UI/Themes/Geometries.xaml", UriKind.Absolute)
        });
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Net.Utilities.WPF;component/UI/Themes/DrawingImages.xaml", UriKind.Absolute)
        });
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Net.Utilities.WPF.HandyControl;component/Themes/Styles.xaml", UriKind.Absolute)
        });
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Net.Utilities.WPF.Converters.Calibration;component/Themes/ConvertersTest.xaml", UriKind.Absolute)
        });
    }

    #endregion

    #region STA

    private static void RunOnSta(Action action)
    {
        EnsureStaDispatcher();
        var dispatcher = _staDispatcher!;
        if (dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action, DispatcherPriority.Normal);
    }

    private static void EnsureStaDispatcher()
    {
        lock (StaSync)
        {
            if (_staDispatcher is { HasShutdownStarted: false, HasShutdownFinished: false })
            {
                return;
            }

            Dispatcher? dispatcher = null;
            using var ready = new ManualResetEventSlim(false);
            var thread = new Thread(() =>
            {
                ImmDisableIME(unchecked((uint)-1));
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
                dispatcher = Dispatcher.CurrentDispatcher;
                ready.Set();
                Dispatcher.Run();
            })
            {
                Name = "AlgoCVSharpTemplateMatch-STA",
                IsBackground = true
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            ready.Wait();

            _staDispatcher = dispatcher;
            _staThread = thread;
            _staShutdownStarted = 0;
            if (_exitHooksRegistered == false)
            {
                _exitHooksRegistered = true;
                AppDomain.CurrentDomain.ProcessExit += (_, _) => ShutdownStaDispatcher();
#if NETFRAMEWORK
                AppDomain.CurrentDomain.DomainUnload += (_, _) => ShutdownStaDispatcher();
#endif
            }
        }
    }

    private static void ShutdownStaDispatcher()
    {
        if (Interlocked.Exchange(ref _staShutdownStarted, 1) == 1)
        {
            return;
        }

        var dispatcher = _staDispatcher;
        if (dispatcher is null)
        {
            return;
        }

        try
        {
            if (dispatcher.HasShutdownStarted == false)
            {
                dispatcher.InvokeShutdown();
            }
        }
        catch (InvalidComObjectException)
        {
        }

        _staThread?.Join(TimeSpan.FromSeconds(2));
    }

    [DllImport("imm32.dll")]
    private static extern bool ImmDisableIME(uint idThread);

    #endregion

#endif

    public void Dispose()
    {
#if TemplateMatchInteractive
        if (_host is null)
        {
            return;
        }

        try
        {
            RunOnSta(DisposeHost);
        }
        catch (InvalidComObjectException)
        {
        }
        catch (TaskCanceledException)
        {
            DisposeHost();
        }
#endif
    }

}