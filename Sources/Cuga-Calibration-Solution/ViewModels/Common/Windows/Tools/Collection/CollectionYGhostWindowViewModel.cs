using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Core.Utilities;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.View;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Point = Net.Utilities.Models.Geometries.Point;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Collection;

[IOCAppService(ServiceType = typeof(CollectionYGhostWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CollectionYGhostWindowViewModel(
    IServiceProvider serviceProvider,
    StageViewModel stageViewModel,
    StageWindowViewModel stageWindowViewModel,
    AfViewModel afViewModel,
    LaserViewModel laserViewModel,
    CIBViewModel cibViewModel,
    IOptions<ApplicationSetting> options,
    ICalibrationOpticsService calibrationOpticsService,
    ICalibrationStatusService calibrationStatusService,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    CreateRoiWindowViewModel createRoiWindowViewModel,
    ApplicationCookie applicationCookie,
    ICacheProvider cacheProvider,
    IDialogWindowProvider dialogWindowProvider,
    IWindowManagerService windowManagerService,
    ILogger<CollectionYGhostWindowViewModel> logger) : ViewModelBase
{
    public IReadOnlyList<int> AvailableChannelIds => ApplicationCookie.CIBInformationChannelIds;

    [ObservableProperty]
    private int _selectedChannelId = -1;

    private const string DSW = nameof(DSW);
    private const string Haze = nameof(Haze);

    public string Name => "Collection Y Ghost";
    public string ImageDirectory => Path.Combine(options.Value.AppHomeDirectory, "Images", nameof(CollectionYGhostWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public static string LogHtmlFileName => "Collection Y Ghost_Diagnosis";

    public string DiagnosisHtmlLogFileName => string.IsNullOrWhiteSpace(LogHtmlFileName) ? "Diagnosis" : $"Diagnosis-{FileHelper.RemoveInvalidFileName(LogHtmlFileName)}";

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private OpticsConfiguration _opticsConfiguration = new();

    [ObservableProperty]
    private Point _brightFieldPosition;

    [ObservableProperty]
    private int _imageWidthPixel = 1000;

    [ObservableProperty]
    private float _moveDownThreshold = -0.3f;

    [ObservableProperty]
    private string[] _pMT8Ch1ImagePath = new string[7];

    [ObservableProperty]
    private string[] _pMT8Ch2ImagePath = new string[7];

    [ObservableProperty]
    private string[] _pMT8Ch3ImagePath = new string[7];

    [ObservableProperty]
    private string[] _pMT8ChImagePath = new string[7];

    [ObservableProperty]
    public Point[] _yGhostListCH11 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH12 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH13 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH14 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH15 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH16 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH17 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH21 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH22 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH23 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH24 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH25 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH26 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH27 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH31 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH32 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH33 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH34 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH35 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH36 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH37 = [];

    [ObservableProperty]
    public Point[] _yGhostListCH = [];

    [ObservableProperty]
    private float _pMT8Ch1PixselCompare = 0;

    [ObservableProperty]
    private float _pMT8Ch2PixselCompare = 0;

    [ObservableProperty]
    private float _pMT8Ch3PixselCompare = 0;

    [ObservableProperty]
    private ObservableCollection<double> _yValueListCH = new();

    [ObservableProperty]
    private ObservableCollection<Point> _alignedYGhostListCH12 = new();

    [ObservableProperty]
    private ObservableCollection<Point> _alignedYGhostListCH13 = new();

    [ObservableProperty]
    private ObservableCollection<Point> _alignedYGhostListCH14 = new();

    [ObservableProperty]
    private ObservableCollection<Point> _alignedYGhostListCH15 = new();

    [ObservableProperty]
    private ObservableCollection<Point> _alignedYGhostListCH16 = new();

    [ObservableProperty]
    private ObservableCollection<Point> _alignedYGhostListCH17 = new();

    [ObservableProperty]
    private ObservableCollection<Point> _alignedYGhostListCH22 = new();

    [ObservableProperty]
    private ObservableCollection<Point> _alignedYGhostListCH23 = new();

    [ObservableProperty]
    private ObservableCollection<Point> _alignedYGhostListCH24 = new();

    [ObservableProperty]
    private ObservableCollection<Point> _alignedYGhostListCH25 = new();

    [ObservableProperty]
    private ObservableCollection<Point> _alignedYGhostListCH26 = new();

    [ObservableProperty]
    private ObservableCollection<Point> _alignedYGhostListCH27 = new();

    [ObservableProperty]
    private ObservableCollection<Point> _alignedYGhostListCH32 = new();

    [ObservableProperty]
    private ObservableCollection<Point> _alignedYGhostListCH33 = new();

    [ObservableProperty]
    private ObservableCollection<Point> _alignedYGhostListCH34 = new();

    [ObservableProperty]
    private ObservableCollection<Point> _alignedYGhostListCH35 = new();

    [ObservableProperty]
    private ObservableCollection<Point> _alignedYGhostListCH36 = new();

    [ObservableProperty]
    private ObservableCollection<Point> _alignedYGhostListCH37 = new();


    public ApplicationCookie ApplicationCookie => applicationCookie;

    public Guid HtmlLogUniqueId { get; private set; }

    [RelayCommand]
    private void Loaded()
    {
        HtmlLogUniqueId = Guid.NewGuid();
        logger.LogHtmlInformation("Result Params", HtmlHeaderLevelEnum.Header1, new HtmlBullet(new
        {
            BrightFieldPosition,
            ImageWidthPixel
        }), HtmlLogUniqueId.LoggingHtml());
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step00Async(CancellationToken cancellationToken)
    {
        BrightFieldPosition = stageViewModel.GetBrightFieldStagePosition();
        CalChipSiteModelEnum calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
        stageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(BrightFieldPosition, calChipSiteModelEnum);
        afViewModel.ToggleDarkFieldEnable(true);
        calibrationOpticsService.ClinderEXC(OpticsYGhostModeEnum.NI_Zoos, true);

        Point DarkFieldPosition = stageViewModel.GetDarkFieldStagePosition();
        Point DarkFieldPosition2 = stageWindowViewModel.StatusViewModel.DarkFieldPosition;

        logger.LogHtmlInformation("Get PMT of 8 Pictures", HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
        {
            BrightFieldPosition,
            DarkFieldPosition,
            ImageWidthPixel
        }), HtmlLogUniqueId.LoggingHtml());
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step01Async(CancellationToken cancellationToken)
    {
        calibrationOpticsService.ClinderEXC(OpticsYGhostModeEnum.NI_Zoos, true);
        {
            var allOpticsPaths1 = new string[7]; // 创建新数组
            var allOpticsPaths2 = new string[7]; // 创建新数组
            var allOpticsPaths3 = new string[7]; // 创建新数组

            for (int i = 1; i <= 7; i++)
            {
                Point DarkFieldPosition = stageViewModel.GetDarkFieldStagePosition();
                var CIBInfor = ApplicationCookie.CIBInformations.Single(x => x.PMTId == 8 && x.ChannelId == 1);
                using var darkFieldImage1 = await cibViewModel.GetPMTImageAsync(
                    ApplicationCookie.OILowProductivityInformation,
                    StageCoordinateSystemEnum.Dark,
                    DarkFieldPosition,
                    ImageWidthPixel,
                    CIBInfor,
                    (false, CalChipSiteModelEnum.DswModel),
                    (false, OpticsConfiguration),
                    (false, CIBConfiguration),
                    (false, LaserLightInformation),
                    false,
                    cancellationToken);

                var path = Path.Combine(ImageDirectory, "CH1", $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                darkFieldImage1.Image.SaveImage(path);
                allOpticsPaths1[i - 1] = path;

                if (i == 1)
                {
                    YGhostListCH11 = ProcessImageAndGetPoints(darkFieldImage1.Image).ToArray();
                }

                if (i == 2)
                {
                    YGhostListCH12 = ProcessImageAndGetPoints(darkFieldImage1.Image).ToArray();
                }

                if (i == 3)
                {
                    YGhostListCH13 = ProcessImageAndGetPoints(darkFieldImage1.Image).ToArray();
                }

                if (i == 4)
                {
                    YGhostListCH14 = ProcessImageAndGetPoints(darkFieldImage1.Image).ToArray();
                }

                if (i == 5)
                {
                    YGhostListCH15 = ProcessImageAndGetPoints(darkFieldImage1.Image).ToArray();
                }

                if (i == 6)
                {
                    YGhostListCH16 = ProcessImageAndGetPoints(darkFieldImage1.Image).ToArray();
                }

                if (i == 7)
                {
                    YGhostListCH17 = ProcessImageAndGetPoints(darkFieldImage1.Image).ToArray();
                }

                CIBInfor = ApplicationCookie.CIBInformations.Single(x => x.PMTId == 8 && x.ChannelId == 2);
                using var darkFieldImage2 = await cibViewModel.GetPMTImageAsync(
                    ApplicationCookie.OILowProductivityInformation,
                    StageCoordinateSystemEnum.Dark,
                    DarkFieldPosition,
                    ImageWidthPixel,
                    CIBInfor,
                    (false, CalChipSiteModelEnum.DswModel),
                    (false, OpticsConfiguration),
                    (false, CIBConfiguration),
                    (false, LaserLightInformation),
                    false,
                    cancellationToken);

                path = Path.Combine(ImageDirectory, "CH2", $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                darkFieldImage2.Image.SaveImage(path);
                allOpticsPaths2[i - 1] = path;

                if (i == 1)
                {
                    YGhostListCH21 = ProcessImageAndGetPoints(darkFieldImage2.Image).ToArray();
                }

                if (i == 2)
                {
                    YGhostListCH22 = ProcessImageAndGetPoints(darkFieldImage2.Image).ToArray();
                }

                if (i == 3)
                {
                    YGhostListCH23 = ProcessImageAndGetPoints(darkFieldImage2.Image).ToArray();
                }

                if (i == 4)
                {
                    YGhostListCH24 = ProcessImageAndGetPoints(darkFieldImage2.Image).ToArray();
                }

                if (i == 5)
                {
                    YGhostListCH25 = ProcessImageAndGetPoints(darkFieldImage2.Image).ToArray();
                }

                if (i == 6)
                {
                    YGhostListCH26 = ProcessImageAndGetPoints(darkFieldImage2.Image).ToArray();
                }

                if (i == 7)
                {
                    YGhostListCH27 = ProcessImageAndGetPoints(darkFieldImage2.Image).ToArray();
                }

                CIBInfor = ApplicationCookie.CIBInformations.Single(x => x.PMTId == 8 && x.ChannelId == 3);
                using var darkFieldImage3 = await cibViewModel.GetPMTImageAsync(
                    ApplicationCookie.OILowProductivityInformation,
                    StageCoordinateSystemEnum.Dark,
                    DarkFieldPosition,
                    ImageWidthPixel,
                    CIBInfor,
                    (false, CalChipSiteModelEnum.DswModel),
                    (false, OpticsConfiguration),
                    (false, CIBConfiguration),
                    (false, LaserLightInformation),
                    false,
                    cancellationToken);

                path = Path.Combine(ImageDirectory, "CH3", $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                darkFieldImage3.Image.SaveImage(path);
                allOpticsPaths3[i - 1] = path;

                if (i == 1)
                {
                    YGhostListCH31 = ProcessImageAndGetPoints(darkFieldImage3.Image).ToArray();
                }

                if (i == 2)
                {
                    YGhostListCH32 = ProcessImageAndGetPoints(darkFieldImage3.Image).ToArray();
                }

                if (i == 3)
                {
                    YGhostListCH33 = ProcessImageAndGetPoints(darkFieldImage3.Image).ToArray();
                }

                if (i == 4)
                {
                    YGhostListCH34 = ProcessImageAndGetPoints(darkFieldImage3.Image).ToArray();
                }

                if (i == 5)
                {
                    YGhostListCH35 = ProcessImageAndGetPoints(darkFieldImage3.Image).ToArray();
                }

                if (i == 6)
                {
                    YGhostListCH36 = ProcessImageAndGetPoints(darkFieldImage3.Image).ToArray();
                }

                if (i == 7)
                {
                    YGhostListCH37 = ProcessImageAndGetPoints(darkFieldImage3.Image).ToArray();
                }

                // 创建新的Point实例，Y坐标增加23
                Point newPosition = new Point(DarkFieldPosition.X, DarkFieldPosition.Y + 23);
                CalChipSiteModelEnum calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
                stageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(newPosition, calChipSiteModelEnum);
                afViewModel.ToggleDarkFieldEnable(true);
            }

            PMT8Ch1ImagePath = allOpticsPaths1;
            PMT8Ch2ImagePath = allOpticsPaths2;
            PMT8Ch3ImagePath = allOpticsPaths3;
        }

        logger.LogHtmlInformation("Get ALL PMT Pictures", HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
        {
            StagePosition = stageViewModel.GetMachineStagePosition(),
            DarkFieldPosition = stageViewModel.GetDarkFieldStagePosition(),
            ImageWidthPixel,
            HtmlTab1 = new HtmlTab(new
            {
                PMT8Ch1Image1 = new HtmlImage(PMT8Ch1ImagePath[0]),
                PMT8Ch1Image2 = new HtmlImage(PMT8Ch1ImagePath[1]),
                PMT8Ch1Image3 = new HtmlImage(PMT8Ch1ImagePath[2]),
                PMT8Ch1Image4 = new HtmlImage(PMT8Ch1ImagePath[3]),
                PMT8Ch1Image5 = new HtmlImage(PMT8Ch1ImagePath[4]),
                PMT8Ch1Image6 = new HtmlImage(PMT8Ch1ImagePath[5]),
                PMT8Ch1Image7 = new HtmlImage(PMT8Ch1ImagePath[6])
            }),
            HtmlTab2 = new HtmlTab(new
            {
                PMT8Ch2Image1 = new HtmlImage(PMT8Ch2ImagePath[0]),
                PMT8Ch2Image2 = new HtmlImage(PMT8Ch2ImagePath[1]),
                PMT8Ch2Image3 = new HtmlImage(PMT8Ch2ImagePath[2]),
                PMT8Ch2Image4 = new HtmlImage(PMT8Ch2ImagePath[3]),
                PMT8Ch2Image5 = new HtmlImage(PMT8Ch2ImagePath[4]),
                PMT8Ch2Image6 = new HtmlImage(PMT8Ch2ImagePath[5]),
                PMT8Ch2Image7 = new HtmlImage(PMT8Ch2ImagePath[6])
            }),
            HtmlTab3 = new HtmlTab(new
            {
                PMT8Ch3Image1 = new HtmlImage(PMT8Ch3ImagePath[0]),
                PMT8Ch3Image2 = new HtmlImage(PMT8Ch3ImagePath[1]),
                PMT8Ch3Image3 = new HtmlImage(PMT8Ch3ImagePath[2]),
                PMT8Ch3Image4 = new HtmlImage(PMT8Ch3ImagePath[3]),
                PMT8Ch3Image5 = new HtmlImage(PMT8Ch3ImagePath[4]),
                PMT8Ch3Image6 = new HtmlImage(PMT8Ch3ImagePath[5]),
                PMT8Ch3Image7 = new HtmlImage(PMT8Ch3ImagePath[6])
            })
        }), HtmlLogUniqueId.LoggingHtml());
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step02Async(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            LoadGhostData();

            logger.LogHtmlInformation("Get ALL Channel Curves", HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
            {
                PlotCh1 = new HtmlPlot2DLinesChart([
                    ("CH11", YGhostListCH11),
                    ("CH12", YGhostListCH12),
                    ("CH13", YGhostListCH13),
                    ("CH14", YGhostListCH14),
                    ("CH15", YGhostListCH15),
                    ("CH16", YGhostListCH16),
                    ("CH17", YGhostListCH17)
                ], "PlotCh1"),
                PlotCh2 = new HtmlPlot2DLinesChart([
                    ("CH21", YGhostListCH21),
                    ("CH22", YGhostListCH22),
                    ("CH23", YGhostListCH23),
                    ("CH24", YGhostListCH24),
                    ("CH25", YGhostListCH25),
                    ("CH26", YGhostListCH26),
                    ("CH27", YGhostListCH27)
                ], "PlotCh2"),
                PlotCh3 = new HtmlPlot2DLinesChart([
                    ("CH31", YGhostListCH31),
                    ("CH32", YGhostListCH32),
                    ("CH33", YGhostListCH33),
                    ("CH34", YGhostListCH34),
                    ("CH35", YGhostListCH35),
                    ("CH36", YGhostListCH36),
                    ("CH37", YGhostListCH37)
                ], "PlotCh3")
            }), HtmlLogUniqueId.LoggingHtml());

            logger.LogHtmlInformation(HtmlLogUniqueId.LoggingPeekHtml($"{DiagnosisHtmlLogFileName}_OK"));
            logger.LogHtmlInformation(HtmlLogUniqueId.LoggingClearHtml());
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private void Close()
    {
        try
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save cache");
        }

        CloseView(null);
    }

    partial void OnSelectedChannelIdChanged(int value)
    {
        var channelIds = ApplicationCookie.CIBInformationChannelIds;
        int index = channelIds.ToList().IndexOf(value);

        switch (index)
        {
            case 0:
                PMT8ChImagePath = PMT8Ch1ImagePath.ToArray();
                break;
            case 1:
                PMT8ChImagePath = PMT8Ch2ImagePath.ToArray();
                break;
            case 2:
                PMT8ChImagePath = PMT8Ch3ImagePath.ToArray();
                break;
            default:
                MessageBox.Show($"通道 {value} 超出支持范围", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
        }
    }

    int? FindXAtY(Point[] points, double targetY)
    {
        foreach (var p in points)
        {
            if (Math.Abs(p.Y - targetY) < 1e-6) // 避免浮点精度问题（如果 Y 是 double）
                return (int)p.X;
        }

        return null;
    }

    private int? FindClosestXAtY(Point[] points, double targetY)
    {
        if (points == null || points.Length == 0)
            return null;

        Point bestPoint = points[0];
        double minDiff = Math.Abs(points[0].Y - targetY);

        for (int i = 1; i < points.Length; i++)
        {
            double diff = Math.Abs(points[i].Y - targetY);
            if (diff < minDiff)
            {
                minDiff = diff;
                bestPoint = points[i];
            }
        }

        return (int)bestPoint.X;
    }

    public void LoadGhostData()
    {
        double targetY = MoveDownThreshold;
        {
            // 1. 获取基准 X（来自 CH11）
            var refX = FindClosestXAtY(YGhostListCH11, targetY);

            var x12 = FindClosestXAtY(YGhostListCH12, targetY);
            if (x12.HasValue)
            {
                int offset12 = x12.Value - refX.Value;
                YGhostListCH12 = YGhostListCH12.Select(p => new Point(p.X - offset12, p.Y)).ToArray();
            }

            var x13 = FindClosestXAtY(YGhostListCH13, targetY);
            if (x13.HasValue)
            {
                int offset13 = x13.Value - refX.Value;
                YGhostListCH13 = YGhostListCH13.Select(p => new Point(p.X - offset13, p.Y)).ToArray();
            }

            var x14 = FindClosestXAtY(YGhostListCH14, targetY);
            if (x14.HasValue)
            {
                int offset14 = x14.Value - refX.Value;
                YGhostListCH14 = YGhostListCH14.Select(p => new Point(p.X - offset14, p.Y)).ToArray();
            }

            var x15 = FindClosestXAtY(YGhostListCH15, targetY);
            if (x15.HasValue)
            {
                int offset15 = x15.Value - refX.Value;
                YGhostListCH15 = YGhostListCH15.Select(p => new Point(p.X - offset15, p.Y)).ToArray();
            }

            var x16 = FindClosestXAtY(YGhostListCH16, targetY);
            if (x16.HasValue)
            {
                int offset16 = x16.Value - refX.Value;
                YGhostListCH16 = YGhostListCH16.Select(p => new Point(p.X - offset16, p.Y)).ToArray();
            }

            var x17 = FindClosestXAtY(YGhostListCH17, targetY);
            if (x17.HasValue)
            {
                int offset17 = x17.Value - refX.Value;
                YGhostListCH17 = YGhostListCH17.Select(p => new Point(p.X - offset17, p.Y)).ToArray();
            }
        }

        {
            var refX = FindClosestXAtY(YGhostListCH21, targetY);

            var x22 = FindClosestXAtY(YGhostListCH22, targetY);
            if (x22.HasValue)
            {
                int offset22 = x22.Value - refX.Value;
                YGhostListCH22 = YGhostListCH22.Select(p => new Point(p.X - offset22, p.Y)).ToArray();
            }

            var x23 = FindClosestXAtY(YGhostListCH23, targetY);
            if (x23.HasValue)
            {
                int offset23 = x23.Value - refX.Value;
                YGhostListCH23 = YGhostListCH23.Select(p => new Point(p.X - offset23, p.Y)).ToArray();
            }

            var x24 = FindClosestXAtY(YGhostListCH24, targetY);
            if (x24.HasValue)
            {
                int offset24 = x24.Value - refX.Value;
                YGhostListCH24 = YGhostListCH24.Select(p => new Point(p.X - offset24, p.Y)).ToArray();
            }

            var x25 = FindClosestXAtY(YGhostListCH25, targetY);
            if (x25.HasValue)
            {
                int offset25 = x25.Value - refX.Value;
                YGhostListCH25 = YGhostListCH25.Select(p => new Point(p.X - offset25, p.Y)).ToArray();
            }

            var x26 = FindClosestXAtY(YGhostListCH26, targetY);
            if (x26.HasValue)
            {
                int offset26 = x26.Value - refX.Value;
                YGhostListCH26 = YGhostListCH26.Select(p => new Point(p.X - offset26, p.Y)).ToArray();
            }

            var x27 = FindClosestXAtY(YGhostListCH27, targetY);
            if (x27.HasValue)
            {
                int offset27 = x27.Value - refX.Value;
                YGhostListCH27 = YGhostListCH27.Select(p => new Point(p.X - offset27, p.Y)).ToArray();
            }
        }

        {
            var refX = FindClosestXAtY(YGhostListCH31, targetY);

            var x32 = FindClosestXAtY(YGhostListCH32, targetY);
            if (x32.HasValue)
            {
                int offset32 = x32.Value - refX.Value;
                YGhostListCH32 = YGhostListCH32.Select(p => new Point(p.X - offset32, p.Y)).ToArray();
            }

            var x33 = FindClosestXAtY(YGhostListCH33, targetY);
            if (x33.HasValue)
            {
                int offset33 = x33.Value - refX.Value;
                YGhostListCH33 = YGhostListCH33.Select(p => new Point(p.X - offset33, p.Y)).ToArray();
            }

            var x34 = FindClosestXAtY(YGhostListCH34, targetY);
            if (x34.HasValue)
            {
                int offset34 = x34.Value - refX.Value;
                YGhostListCH34 = YGhostListCH34.Select(p => new Point(p.X - offset34, p.Y)).ToArray();
            }

            var x35 = FindClosestXAtY(YGhostListCH35, targetY);
            if (x35.HasValue)
            {
                int offset35 = x35.Value - refX.Value;
                YGhostListCH35 = YGhostListCH35.Select(p => new Point(p.X - offset35, p.Y)).ToArray();
            }

            var x36 = FindClosestXAtY(YGhostListCH36, targetY);
            if (x36.HasValue)
            {
                int offset36 = x36.Value - refX.Value;
                YGhostListCH36 = YGhostListCH36.Select(p => new Point(p.X - offset36, p.Y)).ToArray();
            }

            var x37 = FindClosestXAtY(YGhostListCH37, targetY);
            if (x37.HasValue)
            {
                int offset37 = x37.Value - refX.Value;
                YGhostListCH37 = YGhostListCH37.Select(p => new Point(p.X - offset37, p.Y)).ToArray();
            }
        }
    }

    private ObservableCollection<Point> ProcessImageAndGetPoints(BitmapImage image0)
    {
        using var mirrorImage = calibrationAlgorithmService.RotateAndMirrorImage(image0);
        var yArray = calibrationAlgorithmService.GetImageGrayYProjectionsPixels(mirrorImage);
        var points = new ObservableCollection<Point>();

        for (var j = 0; j < yArray.Length; j++)
        {
            points.Add(new Point(j, yArray[j]));
        }

        return points;
    }

    private int FindFirstDropPoint(Point[] points)
    {
        if (points == null || points.Length == 0) return 0;
        for (int i = 1; i < points.Length; i++)
        {
            double dy = points[i].Y - points[i - 1].Y;
            if (dy < MoveDownThreshold) // 假设下降超过 0.1 就认为是“开始”
                return (int)points[i].X;
        }

        return 0;
    }

    private void AlignCurvesCh1()
    {
        var baseCurve = YGhostListCH11.ToList(); // 基准曲线
        var baseStartX = FindFirstDropPoint(baseCurve.ToArray());
        // 对其他曲线做平移
        var curves = new[]
        {
            (YGhostListCH12, "CH12"),
            (YGhostListCH13, "CH13"),
            (YGhostListCH14, "CH14"),
            (YGhostListCH15, "CH15"),
            (YGhostListCH16, "CH16"),
            (YGhostListCH17, "CH17")
        };

        foreach (var (curve, name) in curves)
        {
            if (curve == null || curve.Length == 0) continue;

            var start = FindFirstDropPoint(curve);
            var offset = start - baseStartX;

            var aligned = curve.Select(p => new Point(p.X - offset, p.Y)).ToList();

            // 根据名称绑定到对应属性
            switch (name)
            {
                case "CH12": AlignedYGhostListCH12 = new ObservableCollection<Point>(aligned); break;
                case "CH13": AlignedYGhostListCH13 = new ObservableCollection<Point>(aligned); break;
                case "CH14": AlignedYGhostListCH14 = new ObservableCollection<Point>(aligned); break;
                case "CH15": AlignedYGhostListCH15 = new ObservableCollection<Point>(aligned); break;
                case "CH16": AlignedYGhostListCH16 = new ObservableCollection<Point>(aligned); break;
                case "CH17": AlignedYGhostListCH17 = new ObservableCollection<Point>(aligned); break;
            }
        }
    }

    private void AlignCurvesCh2()
    {
        var baseCurve = YGhostListCH21.ToList(); // 基准曲线
        var baseStartX = FindFirstDropPoint(baseCurve.ToArray());
        // 对其他曲线做平移
        var curves = new[]
        {
            (YGhostListCH22, "CH22"),
            (YGhostListCH23, "CH23"),
            (YGhostListCH24, "CH24"),
            (YGhostListCH25, "CH25"),
            (YGhostListCH26, "CH26"),
            (YGhostListCH27, "CH27")
        };

        foreach (var (curve, name) in curves)
        {
            if (curve == null || curve.Length == 0) continue;

            var start = FindFirstDropPoint(curve);
            var offset = start - baseStartX;

            var aligned = curve.Select(p => new Point(p.X - offset, p.Y)).ToList();

            // 根据名称绑定到对应属性
            switch (name)
            {
                case "CH22": AlignedYGhostListCH22 = new ObservableCollection<Point>(aligned); break;
                case "CH23": AlignedYGhostListCH23 = new ObservableCollection<Point>(aligned); break;
                case "CH24": AlignedYGhostListCH24 = new ObservableCollection<Point>(aligned); break;
                case "CH25": AlignedYGhostListCH25 = new ObservableCollection<Point>(aligned); break;
                case "CH26": AlignedYGhostListCH26 = new ObservableCollection<Point>(aligned); break;
                case "CH27": AlignedYGhostListCH27 = new ObservableCollection<Point>(aligned); break;
            }
        }
    }

    private void AlignCurvesCh3()
    {
        var baseCurve = YGhostListCH31.ToList(); // 基准曲线
        var baseStartX = FindFirstDropPoint(baseCurve.ToArray());
        // 对其他曲线做平移
        var curves = new[]
        {
            (YGhostListCH32, "CH32"),
            (YGhostListCH33, "CH33"),
            (YGhostListCH34, "CH34"),
            (YGhostListCH35, "CH35"),
            (YGhostListCH36, "CH36"),
            (YGhostListCH37, "CH37")
        };

        foreach (var (curve, name) in curves)
        {
            if (curve == null || curve.Length == 0) continue;

            var start = FindFirstDropPoint(curve);
            var offset = start - baseStartX;

            var aligned = curve.Select(p => new Point(p.X - offset, p.Y)).ToList();

            // 根据名称绑定到对应属性
            switch (name)
            {
                case "CH32": AlignedYGhostListCH32 = new ObservableCollection<Point>(aligned); break;
                case "CH33": AlignedYGhostListCH33 = new ObservableCollection<Point>(aligned); break;
                case "CH34": AlignedYGhostListCH34 = new ObservableCollection<Point>(aligned); break;
                case "CH35": AlignedYGhostListCH35 = new ObservableCollection<Point>(aligned); break;
                case "CH36": AlignedYGhostListCH36 = new ObservableCollection<Point>(aligned); break;
                case "CH37": AlignedYGhostListCH37 = new ObservableCollection<Point>(aligned); break;
            }
        }
    }
}