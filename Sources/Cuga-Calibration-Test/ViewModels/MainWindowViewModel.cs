using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Helper;
using Core.Models.Models.Ads.XGains;
using Core.Models.Models.Ads.YGains;
using Core.Models.Models.AOD.AODAlignment;
using Core.Models.Models.AOD.AODDelay;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Laser.Attenuator;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.IlluminationProfile;
using Core.Models.Models.Laser.OpticalPowerMeter;
using Core.Models.Models.Laser.XTCCalibration;
using Core.Models.Models.Laser.XYAstigmatism;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using CugaCalibration.ViewModels;
using CugaCalibration.ViewModels.Common;
using CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;
using CugaCalibration.ViewModels.Common.Windows.Tools.Collection;
using HalconDotNet;
using HAlgorithm;
using Local.NoSQL.DB.Providers.Interfaces;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using ScottPlot;
using ScottPlot.Palettes;
using ScottPlot.WPF;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows;
using Point = Net.Utilities.Models.Geometries.Point;

namespace CugaCalibrationTest.ViewModels;

[IOCAppService(ServiceType = typeof(MainWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MainWindowViewModel(
    IDialogWindowProvider dialogWindowProvider,
    SplitImageWindowViewModel splitImageWindowViewModel,
    StageMapWindowViewModel stageMapWindowViewModel,
    IWindowManagerService windowManagerService,
    CalibrationSetting calibrationSetting,
    ReviewViewModel reviewViewModel,
    ILogger<MainWindowViewModel> logger,
    ICacheProvider cacheProvider,
    [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
    ICacheProvider recipeCacheProvider,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    LoadingWindowViewModel loadingWindowViewModel) : ViewModelBase
{
    [ObservableProperty]
    private CalibrationSetting _calibrationSetting = calibrationSetting;

    [RelayCommand]
    private async Task LoadedAsync()
    {
        await HostApplication.GetRequiredService<LoadingWindowViewModel>().LoadedCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void StageMap()
    {
        windowManagerService.ShowWindow(stageMapWindowViewModel);
    }

    [RelayCommand]
    private void SplitImage()
    {
        windowManagerService.ShowWindow(splitImageWindowViewModel);
    }

    [RelayCommand]
    private async Task TestLog1Async()
    {
        await Task.Run(() =>
        {
            var guid = Guid.NewGuid();
            var random = new Random();
            logger.LogHtmlInformation("1. Microscope Focus Calibration", HtmlHeaderLevelEnum.Header1, guid.LoggingHtml());
            logger.LogHtmlInformation("Step3", HtmlHeaderLevelEnum.Header2, guid.LoggingHtml());

            var bytes = reviewViewModel.GetBrightFieldImageMemoryByteArray();

            var base64String = Convert.ToBase64String(bytes);
            var (imageTypeEnum, _) = ImageHelper.GetImageInfo(bytes);
            var dataUri = $"data:{EnumHelper.ToDescriptionString(imageTypeEnum)};base64,{base64String}";

            logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Image0 = new HtmlImage(dataUri, "test", [new HtmlImageCrossOverlay(true)]),
                EcsValue = random.NextDouble(),
                ContentAlignment.BottomCenter,
                FindFocusPosition = new Point(random.NextDouble() * 10000, random.NextDouble() * 10000),
                FindFocusLimit = random.NextDouble(),
                FindFocusInterval = random.NextDouble(),
                Plot = new HtmlPlot2DLinesChart([
                    ("test", Enumerable.Range(1, 100).Select(_ => random.NextDouble()).ToPoints()),
                    ("test", Enumerable.Range(1, 100).Select(_ => random.NextDouble()).ToPoints()),
                    ("test", Enumerable.Range(1, 100).Select(_ => random.NextDouble()).ToPoints())
                ], "test"),
                Image = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", "test", [new HtmlImageCrossOverlay(true)])
            }), guid.LoggingHtml());

            logger.LogHtmlInformation("Action Times1", HtmlHeaderLevelEnum.Header3, guid.LoggingHtml());

            var listAndImage = new List<object>();
            foreach (var i in Enumerable.Range(1, 100))
            {
                var item = new
                {
                    EcsValue = random.NextDouble(),
                    ContentAlignment.BottomCenter,
                    FindFocusPosition = new PointF((float)random.NextDouble() * 10000f, (float)random.NextDouble() * 10000f),
                    Array = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
                };
                listAndImage.Add(item);

                logger.LogHtmlInformation($"{i}. Find Focus time", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    EcsValue = random.NextDouble(),
                    ContentAlignment.BottomCenter,
                    FindFocusPosition = new Point(random.NextDouble() * 10000, random.NextDouble() * 10000),
                    FindFocusLimit = random.NextDouble(),
                    FindFocusInterval = random.NextDouble(),
                    Plot = new HtmlPlot2DLinesChart([
                        ("x", Enumerable.Range(1, 100).Select(_ => random.NextDouble()).ToPoints()),
                        ("y", Enumerable.Range(1, 100).Select(_ => random.NextDouble()).ToPoints()),
                        ("z", Enumerable.Range(1, 100).Select(_ => random.NextDouble()).ToPoints())
                    ], "test"),
                    Image = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", description: "test", htmlImageOverlays: [new HtmlImageCrossOverlay(random.NextDouble() > 0.5)])
                }), guid.LoggingHtml());
            }

            logger.LogHtmlInformation("Action Times2", HtmlHeaderLevelEnum.Header3, guid.LoggingHtml());

            var listAndImage1 = new List<object>();
            foreach (var i in Enumerable.Range(1, 100))
            {
                var item = new
                {
                    EcsValue = random.NextDouble(),
                    ContentAlignment.BottomCenter,
                    FindFocusPosition = new PointF((float)random.NextDouble() * 10000f, (float)random.NextDouble() * 10000f),
                    Array = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
                };
                listAndImage1.Add(item);

                logger.LogHtmlInformation($"{i}. Find Focus time", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    EcsValue = random.NextDouble(),
                    ContentAlignment.BottomCenter,
                    FindFocusPosition = new Point(random.NextDouble() * 10000, random.NextDouble() * 10000),
                    FindFocusLimit = random.NextDouble(),
                    FindFocusInterval = random.NextDouble(),
                    Plot = new HtmlPlot2DLinesChart([
                        ("x", Enumerable.Range(1, 100).Select(_ => random.NextDouble()).ToPoints(), 1),
                        ("y", Enumerable.Range(1, 100).Select(_ => random.NextDouble()).ToPoints(), 5),
                        ("z", Enumerable.Range(1, 100).Select(_ => random.NextDouble()).ToPoints(), 10),
                        ("s", Enumerable.Range(1, 100).Select(_ => random.NextDouble()).ToPoints(), 8),
                        ("a", Enumerable.Range(1, 100).Select(_ => random.NextDouble()).ToPoints(), 7)
                    ], "test"),
                    Image = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", description: "test", htmlImageOverlays: [new HtmlImageCrossOverlay(random.NextDouble() > 0.5)])
                }), guid.LoggingHtml());
            }

            logger.LogHtmlInformation("Table", HtmlHeaderLevelEnum.Header3, new HtmlTable(listAndImage1), guid.LoggingHtml());
            logger.LogHtmlInformation("Result", HtmlHeaderLevelEnum.Header3, new HtmlTable([.. listAndImage]), guid.LoggingHtml());

            logger.LogHtmlCritical(
                new InvalidOperationException(
                    "Do the best you can, until you know better. Then when you know better, do betterDo the best you can, until you know better. Then when you know better, do betterDo the best you can, until you know better. Then when you know better, do betterDo the best you can, until you know better. Then when you know better, do better"),
                "Exception", HtmlHeaderLevelEnum.Header3, guid.LoggingHtml());

            logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlComment("afsasdfasdfasdfasdfasdfasdfasdf"), guid.LoggingHtml());
            logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlContainer(
            [
                new HtmlComment("afsasdfasdfasdfasdfasdfasdfasdf"),
                new HtmlComment("afsasdfasdfasdfasdfasdfasdfasdf"),
                new HtmlComment("afsasdfasdfasdfasdfasdfasdfasdf"),
                new HtmlQuote(new
                {
                    EcsValue = random.NextDouble(),
                    ContentAlignment.BottomCenter,
                    FindFocusPosition = new Point(random.NextDouble() * 10000, random.NextDouble() * 10000),
                    FindFocusLimit = random.NextDouble(),
                    FindFocusInterval = random.NextDouble(),
                    Plot = new HtmlPlot2DLinesChart([
                        ("test", Enumerable.Range(1, 100).Select(_ => random.NextDouble()).ToPoints()),
                        ("test", Enumerable.Range(1, 100).Select(_ => random.NextDouble()).ToPoints()),
                        ("test", Enumerable.Range(1, 100).Select(_ => random.NextDouble()).ToPoints())
                    ], "test"),
                    Image = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", description: "test", htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                }),
                new HtmlBullet(new
                {
                    EcsValue = random.NextDouble(),
                    ContentAlignment.BottomCenter,
                    FindFocusPosition = new Point(random.NextDouble() * 10000, random.NextDouble() * 10000),
                    FindFocusLimit = random.NextDouble(),
                    FindFocusInterval = random.NextDouble(),
                    Plot = new HtmlPlot2DLinesChart([
                        ("test", Enumerable.Range(1, 100).Select(_ => random.NextDouble()).ToPoints()),
                        ("test", Enumerable.Range(1, 100).Select(_ => random.NextDouble()).ToPoints()),
                        ("test", Enumerable.Range(1, 100).Select(_ => random.NextDouble()).ToPoints())
                    ], "test"),
                    Image = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", description: "test", htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            ]), guid.LoggingHtml());

            try
            {
                var i = 1;
                // ReSharper disable once IntDivisionByZero
                _ = 1 / (i - i);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "test");
                logger.LogHtmlError(ex, "test", HtmlHeaderLevelEnum.Header3, guid.LoggingHtml());
            }

            var points = new List<(string Name, IReadOnlyList<Point> Points, double Position)>();
            var points0 = new List<(string Name, IReadOnlyList<Point> Points, double Position)>();
            var points1 = new List<(string Name, IReadOnlyList<Point> Points)>();

            for (var i = 0; i < 10; i++)
            {
                points0.Add(($"test{i}", Enumerable.Range(1, 100).Select(_ => new Point(random.NextDouble() * 10000, random.NextDouble() * 10000)).ToArray(), 1d));
                points.Add(($"test{i}", Enumerable.Range(1, 100).Select(_ => new Point(random.NextDouble() * 10000, random.NextDouble() * 10000)).ToArray(), i + 1d));
                points1.Add(($"test{i}", Enumerable.Range(1, 100).Select(_ => new Point(random.NextDouble() * 10000, random.NextDouble() * 10000)).ToArray()));
            }

            logger.LogHtmlInformation("test1", HtmlHeaderLevelEnum.Header1, new HtmlPlot2DLinesChart(points.ToArray(), "test1"), guid.LoggingHtml());
            logger.LogHtmlInformation("test2", HtmlHeaderLevelEnum.Header1, new HtmlPlot2DLinesChart(points0.ToArray(), "test2"), guid.LoggingHtml());
            logger.LogHtmlInformation("test3", HtmlHeaderLevelEnum.Header1, new HtmlPlot2DLinesChart(points1.ToArray(), "test3"), guid.LoggingHtml());

            logger.LogHtmlTrace("test", HtmlHeaderLevelEnum.Header3, guid.LoggedEndHtml("TestLog1"));
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private void TestLog2()
    {
        var guid = Guid.NewGuid();
        logger.LogHtmlInformation("1. Microscope Focus Calibration", HtmlHeaderLevelEnum.Header1, guid.LoggingHtml());

        logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            Image = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", "test")
        }), guid.LoggingHtml());

        logger.LogHtmlInformation("Tab", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            Download1 = new HtmlDownload(File.ReadAllBytes($"{AppDomain.CurrentDomain.BaseDirectory}\\NLog.config"), "NLog.config"),
            Download2 = new HtmlDownload(File.ReadAllBytes($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg"), "test.jpg"),
            HtmlTab = new HtmlTab(new
            {
                test0 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", description: "test"),
                test1 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test1.jpg", description: "test"),
                test2 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", description: "test", htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                test3 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test1.jpg", description: "test", htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                test4 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", htmlImageOverlays: []),
                test5 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test1.jpg", htmlImageOverlays: []),
                test6 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", htmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(2448 / 5d, 2048 / 5d)),
                    new HtmlImageRectangleOverlay(new Point(2448 / 5d, 2048 / 5d), new Net.Utilities.Models.Geometries.Size(100, 100))
                ]),
                test7 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test1.jpg", htmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(257 / 5d, 257 / 5d)),
                    new HtmlImageRectangleOverlay(new Point(257 / 5d, 257 / 5d), new Net.Utilities.Models.Geometries.Size(100, 100))
                ]),
                test8 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", htmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(2448 / 5d, 2048 / 5d), new Net.Utilities.Models.Geometries.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(2448 / 5d, 2048 / 5d), new Net.Utilities.Models.Geometries.Size(100, 100))
                ]),
                test9 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test1.jpg", htmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(257 / 5d, 257 / 5d), new Net.Utilities.Models.Geometries.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(257 / 5d, 257 / 5d), new Net.Utilities.Models.Geometries.Size(100, 100))
                ]),
                test10 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", htmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(0, 0), new Net.Utilities.Models.Geometries.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(0, 0), new Net.Utilities.Models.Geometries.Size(100, 100))
                ]),
                test11 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test1.jpg", htmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(0, 0), new Net.Utilities.Models.Geometries.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(0, 0), new Net.Utilities.Models.Geometries.Size(100, 100))
                ]),
                test12 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", htmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(2448 - 1, 2048 - 1), new Net.Utilities.Models.Geometries.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(2448 - 1, 2048 - 1), new Net.Utilities.Models.Geometries.Size(100, 100))
                ]),
                test13 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test1.jpg", htmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(257 - 1, 257 - 1), new Net.Utilities.Models.Geometries.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(257 - 1, 257 - 1), new Net.Utilities.Models.Geometries.Size(100, 100))
                ]),
                test14 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", htmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(2448 / 2d, 2048 / 2d), new Net.Utilities.Models.Geometries.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(2448 / 2d, 2048 / 2d), new Net.Utilities.Models.Geometries.Size(100, 100))
                ]),
                test15 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test1.jpg", htmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(257 / 2d, 257 / 2d), new Net.Utilities.Models.Geometries.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(257 / 2d, 257 / 2d), new Net.Utilities.Models.Geometries.Size(100, 100))
                ])
            })
        }), guid.LoggingHtml());
        logger.LogHtmlInformation("Tab", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            Download1 = new HtmlDownload(File.ReadAllBytes($"{AppDomain.CurrentDomain.BaseDirectory}\\NLog.config"), "NLog.config"),
            Download2 = new HtmlDownload(File.ReadAllBytes($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg"), "test.jpg"),
            HtmlTab = new HtmlTab(new
            {
                test0 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", description: "test"),
                test1 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test2.png", description: "test"),
                test2 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", description: "test", htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                test3 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test2.png", description: "test", htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                test4 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", htmlImageOverlays: []),
                test5 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test2.png", htmlImageOverlays: []),
                test6 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", htmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(2448 / 5d, 2048 / 5d)),
                    new HtmlImageRectangleOverlay(new Point(2448 / 5d, 2048 / 5d), new Net.Utilities.Models.Geometries.Size(100, 100))
                ]),
                test7 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test2.png", htmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(1009 / 5d, 1437 / 5d)),
                    new HtmlImageRectangleOverlay(new Point(1009 / 5d, 1437 / 5d), new Net.Utilities.Models.Geometries.Size(100, 100))
                ]),
                test8 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", htmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(2448 / 5d, 2048 / 5d), new Net.Utilities.Models.Geometries.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(2448 / 5d, 2048 / 5d), new Net.Utilities.Models.Geometries.Size(100, 100))
                ]),
                test9 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test2.png", htmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(1009 / 5d, 1437 / 5d), new Net.Utilities.Models.Geometries.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(1009 / 5d, 1437 / 5d), new Net.Utilities.Models.Geometries.Size(100, 100))
                ]),
                test10 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", htmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(0, 0), new Net.Utilities.Models.Geometries.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(0, 0), new Net.Utilities.Models.Geometries.Size(100, 100))
                ]),
                test11 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test2.png", htmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(0, 0), new Net.Utilities.Models.Geometries.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(0, 0), new Net.Utilities.Models.Geometries.Size(100, 100))
                ]),
                test12 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", htmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(2448 - 1, 2048 - 1), new Net.Utilities.Models.Geometries.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(2448 - 1, 2048 - 1), new Net.Utilities.Models.Geometries.Size(100, 100))
                ]),
                test13 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test2.png", htmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(1009 - 1, 1437 - 1), new Net.Utilities.Models.Geometries.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(1009 - 1, 1437 - 1), new Net.Utilities.Models.Geometries.Size(100, 100))
                ]),
                test14 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test.jpg", htmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(2448 / 2d, 2048 / 2d), new Net.Utilities.Models.Geometries.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(2448 / 2d, 2048 / 2d), new Net.Utilities.Models.Geometries.Size(100, 100))
                ]),
                test15 = new HtmlImage($"{AppDomain.CurrentDomain.BaseDirectory}\\Assets\\Data\\test2.png", htmlImageOverlays:
                [
                    new HtmlImageCrossOverlay(new Point(1009 / 2d, 1437 / 2d), new Net.Utilities.Models.Geometries.Size(100, 100)),
                    new HtmlImageRectangleOverlay(new Point(1009 / 2d, 1437 / 2d), new Net.Utilities.Models.Geometries.Size(100, 100))
                ])
            })
        }), guid.LoggingHtml());

        logger.LogHtmlTrace("test", HtmlHeaderLevelEnum.Header3, guid.LoggedEndHtml("TestLog2"));
    }

    [RelayCommand]
    private void AutomaticMPeak()
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Image File",
            Filter = "Image Files (*.jpg;*.raw)|*.jpg;*.raw|All Files (*.*)|*.*", // 允许选择JPG和RAW文件
            RestoreDirectory = true // 记住上次打开的目录
        };

        if (openFileDialog.ShowDialog() == false) return;

        double[] y;
        var extension = Path.GetExtension(openFileDialog.FileName);
        HImage image = HalconFactory.EmptyHImage;
        switch (extension)
        {
            case ".jpg":
                var imageToMatrix = ImageToMatrix(openFileDialog.FileName);
                // 将所有的行相加取平均值，并取反
                y = [.. imageToMatrix.RowSums().Divide(imageToMatrix.RowCount).Select(t => -t)];
                break;

            case ".raw":
                // 取反
                var (image1, matrix) = calibrationAlgorithmService.ToImageInfo(File.ReadAllBytes(openFileDialog.FileName));
                var convertToDoubleMatrix = Matrix<double>.Build.DenseOfArray(matrix);
                image = image1;
                y = [.. convertToDoubleMatrix.RowSums().Divide(convertToDoubleMatrix.RowCount).Select(t => -t)];
                break;

            default:
                throw new ArgumentOutOfRangeException(extension);
        }

        var x = Enumerable.Range(0, y.Length).Select(t => (double)t).ToArray();

        // 使用AMPD算法找出波峰
        var signal = Vector<double>.Build.DenseOfArray(y);
        var peaks = AutomaticMPeakDetection.Ampd(signal);

        var pixelY = string.Empty;
        try
        {
            var mean = peaks.Skip(1).Select((t, i) => (double)t - peaks[i]).Average();
            // 所有后一个减去前一个，得到差值, 然后取得均值
            pixelY = (10 / mean).ToString(CultureInfo.CurrentCulture);
        }
        catch (Exception)
        {
            Algorithm _algorithm = new();
            // _algorithm.DarkPixelSizeCalculate(image, new HTuple(10), out var pixelSize);
            pixelY = "error";
        }

        var category20 = new Category10();
        var wpfPlot = new WpfPlot();
        var crossHair = wpfPlot.Plot.Add.Crosshair(0, 0);
        crossHair.LineColor = Colors.Red;
        crossHair.TextColor = Colors.White;
        crossHair.TextBackgroundColor = Colors.Red;
        wpfPlot.MouseMove += (senderTemp, eTemp) =>
        {
            if (senderTemp is not WpfPlot tempWpfPlot) return;
            var position = eTemp.GetPosition(wpfPlot);
            var mousePixel = new Pixel(position.X, position.Y);
            var mouseCoordinates = tempWpfPlot.Plot.GetCoordinates(mousePixel);

            crossHair.Position = mouseCoordinates;
            crossHair.VerticalLine.Text = $"{mouseCoordinates.X:f3}";
            crossHair.HorizontalLine.Text = $"{mouseCoordinates.Y:f3}";
            wpfPlot.Refresh();
        };

        // 添加噪声信号
        var scatter = wpfPlot.Plot.Add.Scatter((double[])[.. x], [.. y], category20.GetColor(0));
        scatter.LegendText = "Noisy";

        var markers = wpfPlot.Plot.Add.Markers((double[])[.. peaks], [.. peaks.Select(t => y[t])], MarkerShape.FilledDiamond, 10, category20.GetColor(1));
        markers.LegendText = "Peaks";

        wpfPlot.Plot.Title("AutomaticMPeakDetection");
        wpfPlot.Plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);
        wpfPlot.Plot.Axes.AutoScale();
        wpfPlot.Refresh();

        var window = new Window
        {
            Title = $"AutomaticMPeakDetection {pixelY}",
            Content = wpfPlot,
            Width = 800,
            Height = 800,
            Padding = new Thickness(5, 5, 5, 5),
            WindowState = WindowState.Maximized,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        window.ShowDialog();
        return;

        static Matrix<double> ImageToMatrix(string imagePath)
        {
            // Load the image using System.Drawing
            var bitmap = new Bitmap(imagePath);

            // Initialize a matrix to store the image data
            var matrix = Matrix<double>.Build.Dense(bitmap.Height, bitmap.Width);

            // Iterate through each pixel in the image
            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    // Get the color of the pixel
                    var pixelColor = bitmap.GetPixel(x, y);

                    // Convert the color to grayscale
                    double grayscaleValue = pixelColor.R + pixelColor.G + pixelColor.B;

                    // Store the grayscale value in the matrix
                    matrix[y, x] = grayscaleValue;
                }
            }

            return matrix;
        }
    }

    [RelayCommand]
    private void SetLiteDbData()
    {
        cacheProvider.SetArray<AdsXGainsItemDto>([], CancellationToken.None);
        cacheProvider.SetArray<AdsYGainsItemDto>([], CancellationToken.None);
        cacheProvider.Set<ChuckGlobalScaleErrorDto>(new(), CancellationToken.None);
        cacheProvider.Set<LaserAutoFocusDto>(new(), CancellationToken.None);
        cacheProvider.Set<LaserBeamStabilizerObjDto>(new(), CancellationToken.None);
        cacheProvider.SetArray<LaserOpticalPowerMeterDto>([], CancellationToken.None);
        cacheProvider.SetArray<LaserAttenuatorDto>([], CancellationToken.None);
        cacheProvider.SetArray<AODDelayDto>([], CancellationToken.None);
        cacheProvider.SetArray<AODAlignmentDto>([], CancellationToken.None);
        cacheProvider.SetArray<LaserXYAstigmatismCalibrationItemDto>([], CancellationToken.None);
        cacheProvider.SetArray<LaserIlluminationProfileItemDto>([], CancellationToken.None);
        cacheProvider.SetArray<LaserXTCCalibrationItemDto>([], CancellationToken.None);
    }

    [RelayCommand]
    private void ReadRawImageProjectionY()
    {
        var tryShowSelectDirectoryPathDialog = dialogWindowProvider.TryShowSelectDirectoryPathDialog(out var directoryPath);
        if (tryShowSelectDirectoryPathDialog == false) return;

        var category20 = new Category10();
        var wpfPlot = new WpfPlot();
        var crossHair = wpfPlot.Plot.Add.Crosshair(0, 0);
        crossHair.LineColor = Colors.Red;
        crossHair.TextColor = Colors.White;
        crossHair.TextBackgroundColor = Colors.Red;
        wpfPlot.MouseMove += (senderTemp, eTemp) =>
        {
            if (senderTemp is not WpfPlot tempWpfPlot) return;
            var position = eTemp.GetPosition(wpfPlot);
            var mousePixel = new Pixel(position.X, position.Y);
            var mouseCoordinates = tempWpfPlot.Plot.GetCoordinates(mousePixel);

            crossHair.Position = mouseCoordinates;
            crossHair.VerticalLine.Text = $"{mouseCoordinates.X:f3}";
            crossHair.HorizontalLine.Text = $"{mouseCoordinates.Y:f3}";
            wpfPlot.Refresh();
        };

        foreach (var (index, file) in Directory.GetFiles(directoryPath).Select((t, i) => (i, t)))
        {
            if (Path.GetExtension(file) != ".raw") continue;

            var strings = file.Split(["PMT", "Channel"], StringSplitOptions.RemoveEmptyEntries);

            var (image, matrix) = calibrationAlgorithmService.ToImageInfo(File.ReadAllBytes(file));
            using var _ = image;
            var convertToDoubleMatrix = Matrix<double>.Build.DenseOfArray(matrix);

            double[] y = [.. convertToDoubleMatrix.RowSums().Divide(convertToDoubleMatrix.RowCount)];
            var x = Enumerable.Range(0, y.Length).Select(t => (double)t).ToArray();
            var scatter = wpfPlot.Plot.Add.Scatter((double[])[.. x], [.. y], category20.GetColor(int.TryParse(strings[1], out var result) ? result : index));
            scatter.LegendText = Path.GetFileName(file);
        }

        wpfPlot.Plot.Title("images");
        wpfPlot.Plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);
        wpfPlot.Plot.Axes.AutoScale();
        wpfPlot.Refresh();

        var window = new Window
        {
            Title = "images",
            Content = wpfPlot,
            Width = 800,
            Height = 800,
            Padding = new Thickness(5, 5, 5, 5),
            WindowState = WindowState.Maximized,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        window.Show();
    }

    [RelayCommand]
    private void PrescanAODWaveformElectrodeOffset()
    {
        /*var prescanAODWaveformUniformityWindowViewModel = HostApplication.GetRequiredService<PrescanAODWaveformElectrodeOffsetWindowViewModel>();

        windowManagerService.ShowWindow(prescanAODWaveformUniformityWindowViewModel);*/
    }

    [RelayCommand]
    public void ChirpAODWaveformElectrodeOffset()
    {
        var chirpAODWaveformUniformityWindowViewModel = HostApplication.GetRequiredService<ChirpAODWaveformElectrodeOffsetWindowViewModel>();
        windowManagerService.ShowWindow(chirpAODWaveformUniformityWindowViewModel);
    }

    [RelayCommand]
    public void CollectionFocusAlignOpticsFocus()
    {
        var collectionFocusAlignOpticsFocusWindowViewModel = HostApplication.GetRequiredService<CollectionFocusAlignOpticsFocusWindowViewModel>();
        windowManagerService.ShowWindow(collectionFocusAlignOpticsFocusWindowViewModel);
    }
}