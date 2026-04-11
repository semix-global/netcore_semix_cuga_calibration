using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models;
using Core.Models.Models.Common.Fourier;
using Core.Models.Models.Fourier;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using HalconDotNet;
using HAlgorithm;
using Local.SQL.Cache.Providers.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using ScottPlot;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Point = Net.Utilities.Models.Geometries.Point;
using Rect = Net.Utilities.Models.Geometries.Rect;
using Size = Net.Utilities.Models.Geometries.Size;

namespace CugaCalibration.ViewModels.Flourier;

[IOCAppService(ServiceType = typeof(PupilCenterChannelFlexibleApertureViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class PupilCenterChannelFlexibleApertureViewModel(
    ICalibrationAlgorithmService calibrationAlgorithmService,
    CalibrationSetting calibrationSetting,
    ICalibrationFourierService calibrationFlourierService,
    ICalibrationLaserService calibrationLaserService) : CalibrationViewModelBase
{
    #region 界面相关

    private readonly Algorithm _algorithm = new();

    [ObservableProperty]
    private Point[] _circlePointX = new Point[4];

    [ObservableProperty]
    private Point[] _circlePointY = new Point[4];

    [ObservableProperty]
    private Point _resultPointX = new Point();

    [ObservableProperty]
    private Point _resultPointY = new Point();

    [ObservableProperty]
    private float _resultRadiusX = 0;

    [ObservableProperty]
    private float _resultRadiusY = 0;

    public enum PositionShowType
    {
        Position1Angle120,
        Position2Angle180,
        Position3Angle240,
        Position4Angle300
    };

    public enum ChartShowType
    {
        ID_0_Style,
        ID_1_Style,
        ID_3_Style,
        ID_4_Style
    };

    [ObservableProperty]
    private int _rodWidth = 100;

    [ObservableProperty]
    private int _rodHight = 50;

    [ObservableProperty]
    private int _xStartPixel = 100;

    [ObservableProperty]
    private float _ch3Angle = 120;

    [ObservableProperty]
    private int _ch3Angle60 = 60;

    [ObservableProperty]
    private float _ch3TurnX = 2.0f;

    [ObservableProperty]
    private float _ch3TurnY = 2.0f;

    [ObservableProperty]
    private float _ch3Push = 2.0f;

    [ObservableProperty]
    private int[] _rodHeightHorizal1 = new int[4] { 100, 100, 100, 100 };

    [ObservableProperty]
    private int[] _rodHeightHorizal2 = new int[4] { 100, 100, 100, 100 };

    [ObservableProperty]
    private Rect[] _rodRectHorizal1 = new Rect[4];

    [ObservableProperty]
    private Rect[] _rodRectHorizal2 = new Rect[4];

    [ObservableProperty]
    private int[] _rodWidthVertical1 = new int[4] { 100, 100, 100, 100 };

    [ObservableProperty]
    private int[] _rodWidthVertical2 = new int[4] { 100, 100, 100, 100 };

    [ObservableProperty]
    private Rect[] _rodRectVertical1 = new Rect[4];

    [ObservableProperty]
    private Rect[] _rodRectVertical2 = new Rect[4];

    [ObservableProperty]
    private int _rodWidthHeight = 0;

    [ObservableProperty]
    private int _pushWidthVertical1 = 0;

    [ObservableProperty]
    private int _pushWidthVertical2 = 0;

    [ObservableProperty]
    private Rect _pushRectVertical1 = new();

    [ObservableProperty]
    private Rect _pushRectVertical2 = new();

    public ChartShowType[] Ch3ShowTypeValues => Enum.GetValues(typeof(ChartShowType)).Cast<ChartShowType>().ToArray();

    public PositionShowType[] Ch3PositionTypeValues => Enum.GetValues(typeof(PositionShowType)).Cast<PositionShowType>().ToArray();

    [ObservableProperty]
    private ChartShowType _selectedCh3ShowType = (ChartShowType)(-1);

    [ObservableProperty]
    private PositionShowType _selectedCh3PositionType = (PositionShowType)(-1);

    [ObservableProperty]
    private Point _sxPos = new();

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Haze Wafer Position" },
        new() { StepName = "Horizal Rotate Motor,Gear Lever Calibration" },
        new() { StepName = "Vertical Rotate Motor,Gear Lever Calibration" },
        new() { StepName = "Horizal Push Lever Calibration" }
    ];

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private PupilCenterChannelFlexibleApertureDTO _resultDto = new();

    [ObservableProperty]
    private MicroscopeCalChipDTO _microscopeCalChip = new();

    [ObservableProperty]
    private PupilCameraAlignmentDTO _pupilCameraAlignmentValue = new();

    [ObservableProperty]
    private PupilCenterChannelFlexibleApertureCache _cache = new();

    [ObservableProperty]
    private PupilCenterChannelFlexibleApertureDTO _calibration = new();

    #endregion 缓存

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false)
            return false;

        MicroscopeCalChip = CalibrationStatusService.GetCalibration<MicroscopeCalChipDTO>();
        PupilCameraAlignmentValue = CalibrationStatusService.GetCalibration<PupilCameraAlignmentDTO>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<PupilCenterChannelFlexibleApertureCache>();
        Calibration = CacheProvider.GetOrDefault<PupilCenterChannelFlexibleApertureDTO>();
        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        Cache.OriginImageFilePathList12 = new ObservableCollection<string>(Enumerable.Repeat("123", 8));
        Cache.OriginImageFilePathList22 = new ObservableCollection<string>(Enumerable.Repeat("123", 8));
        Cache.OriginImageFilePathList32 = new ObservableCollection<string>(Enumerable.Repeat("123", 2));

        Cache.OriginImageFilePathList11 = new ObservableCollection<string>(Enumerable.Repeat("123", 4));
        Cache.OriginImageFilePathList21 = new ObservableCollection<string>(Enumerable.Repeat("123", 4));

        Cache.CgFFBoxWidthList1 = new ObservableCollection<double>(Enumerable.Repeat(123.0, 8));
        Cache.CgFFBoxHeightList2 = new ObservableCollection<double>(Enumerable.Repeat(123.0, 8));
        Cache.CgFFBoxWidthList3 = new ObservableCollection<double>(Enumerable.Repeat(123.0, 2));

        Cache.OriginImageAngleList11 = new ObservableCollection<double>(Enumerable.Repeat(123.0, 4));
        Cache.OriginImageAngleList21 = new ObservableCollection<double>(Enumerable.Repeat(123.0, 4));

        Cache.OriginImageFilePathList31.Clear();

        ResultDto.CgFFBoxTurnXAngleCh3.Add(0);
        ResultDto.CgFFBoxTurnXAngleCh3.Add(1);
        ResultDto.CgFFBoxTurnXAngleCh3.Add(2);
        ResultDto.CgFFBoxTurnXAngleCh3.Add(3);

        ResultDto.CgFFBoxTurnXWidthCh3.Add(0);
        ResultDto.CgFFBoxTurnXWidthCh3.Add(1);
        ResultDto.CgFFBoxTurnXWidthCh3.Add(2);
        ResultDto.CgFFBoxTurnXWidthCh3.Add(3);

        ResultDto.CgFFBoxTurnXMotorRelationCH3.Add(1);
        ResultDto.CgFFBoxTurnXMotorRelationCH3.Add(3);
        ResultDto.CgFFBoxTurnXMotorRelationCH3.Add(5);
        ResultDto.CgFFBoxTurnXMotorRelationCH3.Add(7);

        ResultDto.CgFFBoxTurnXMotorPositionCH3.Add(0);
        ResultDto.CgFFBoxTurnXMotorPositionCH3.Add(2);
        ResultDto.CgFFBoxTurnXMotorPositionCH3.Add(4);
        ResultDto.CgFFBoxTurnXMotorPositionCH3.Add(6);

        ResultDto.CgFFBoxTurnXRectPositionCH3.Add(new Rect());
        ResultDto.CgFFBoxTurnXRectPositionCH3.Add(new Rect());
        ResultDto.CgFFBoxTurnXRectPositionCH3.Add(new Rect());
        ResultDto.CgFFBoxTurnXRectPositionCH3.Add(new Rect());

        ResultDto.CgFFBoxTurnYAngleCh3.Add(0);
        ResultDto.CgFFBoxTurnYAngleCh3.Add(1);
        ResultDto.CgFFBoxTurnYAngleCh3.Add(2);
        ResultDto.CgFFBoxTurnYAngleCh3.Add(3);

        ResultDto.CgFFBoxTurnYWidthCh3.Add(0);
        ResultDto.CgFFBoxTurnYWidthCh3.Add(1);
        ResultDto.CgFFBoxTurnYWidthCh3.Add(2);
        ResultDto.CgFFBoxTurnYWidthCh3.Add(3);

        ResultDto.CgFFBoxTurnYMotorRelationCH3.Add(1);
        ResultDto.CgFFBoxTurnYMotorRelationCH3.Add(3);
        ResultDto.CgFFBoxTurnYMotorRelationCH3.Add(5);
        ResultDto.CgFFBoxTurnYMotorRelationCH3.Add(7);

        ResultDto.CgFFBoxTurnYMotorPositionCH3.Add(0);
        ResultDto.CgFFBoxTurnYMotorPositionCH3.Add(2);
        ResultDto.CgFFBoxTurnYMotorPositionCH3.Add(4);
        ResultDto.CgFFBoxTurnYMotorPositionCH3.Add(6);

        ResultDto.CgFFBoxTurnYRectPositionCH3.Add(new Rect());
        ResultDto.CgFFBoxTurnYRectPositionCH3.Add(new Rect());
        ResultDto.CgFFBoxTurnYRectPositionCH3.Add(new Rect());
        ResultDto.CgFFBoxTurnYRectPositionCH3.Add(new Rect());

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        switch (CalibrationStepIndex)
        {
            case 0:

                return true;

            case 1:

                return true;

            case 2:

                return true;

            case 3:

                Save(ResultDto, cancellationToken);
                IsCalibrated = true;

                return true;

            default:
                return true;
        }
    }

    protected override Task<bool> CancelingAsync()
    {
        return Task.FromResult(true);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0Async(CancellationToken cancellationToken)
    {
        calibrationFlourierService.SetFFHome(FFCH.Ch1);
        calibrationFlourierService.SetFFHome(FFCH.Ch2);
        calibrationFlourierService.SetFFHome(FFCH.Ch3_X);
        calibrationFlourierService.SetFFHome(FFCH.Ch3_Y);

        Cache.HazeWaferPosition = StageViewModel.GetBrightFieldStagePosition();
        AfViewModel.ToggleDarkFieldEnable(true);
        SxPos = new Point(Cache.HazeWaferPosition.X, Cache.HazeWaferPosition.Y);

        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                HazeWaferPosition = Cache.HazeWaferPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        if (Cache.Ch3Image1 != null)
        {
            ResultDto.CgFFBoxTurnXAngleCh3[0] = Cache.OriginImageAngleList11[0];
            ResultDto.CgFFBoxTurnXAngleCh3[1] = Cache.OriginImageAngleList11[1];
            ResultDto.CgFFBoxTurnXAngleCh3[2] = Cache.OriginImageAngleList11[2];
            ResultDto.CgFFBoxTurnXAngleCh3[3] = Cache.OriginImageAngleList11[3];

            ResultDto.CgFFBoxTurnXWidthCh3[0] = RodRectVertical1[0].Width;
            ResultDto.CgFFBoxTurnXWidthCh3[1] = RodRectVertical1[1].Width;
            ResultDto.CgFFBoxTurnXWidthCh3[2] = RodRectVertical1[2].Width;
            ResultDto.CgFFBoxTurnXWidthCh3[3] = RodRectVertical1[3].Width;

            ResultDto.CgFFBoxTurnXMotorRelationCH3[0] = (Math.Abs(RodWidthVertical2[0] - RodWidthVertical1[0]) / Math.Abs(Cache.CgFFBoxWidthList1[0] - Cache.CgFFBoxWidthList1[1]));
            ResultDto.CgFFBoxTurnXMotorRelationCH3[1] = (Math.Abs(RodWidthVertical2[1] - RodWidthVertical1[1]) / Math.Abs(Cache.CgFFBoxWidthList1[2] - Cache.CgFFBoxWidthList1[3]));
            ResultDto.CgFFBoxTurnXMotorRelationCH3[2] = (Math.Abs(RodWidthVertical2[2] - RodWidthVertical1[2]) / Math.Abs(Cache.CgFFBoxWidthList1[4] - Cache.CgFFBoxWidthList1[5]));
            ResultDto.CgFFBoxTurnXMotorRelationCH3[3] = (Math.Abs(RodWidthVertical2[3] - RodWidthVertical1[3]) / Math.Abs(Cache.CgFFBoxWidthList1[6] - Cache.CgFFBoxWidthList1[7]));

            ResultDto.CgFFBoxTurnXMotorPositionCH3[0] = (Cache.CgFFBoxWidthList1[0]);
            ResultDto.CgFFBoxTurnXMotorPositionCH3[1] = (Cache.CgFFBoxWidthList1[2]);
            ResultDto.CgFFBoxTurnXMotorPositionCH3[2] = (Cache.CgFFBoxWidthList1[4]);
            ResultDto.CgFFBoxTurnXMotorPositionCH3[3] = (Cache.CgFFBoxWidthList1[6]);

            ResultDto.CgFFBoxTurnXRectPositionCH3[0] = (RodRectVertical1[0]);
            ResultDto.CgFFBoxTurnXRectPositionCH3[1] = (RodRectVertical1[1]);
            ResultDto.CgFFBoxTurnXRectPositionCH3[2] = (RodRectVertical1[2]);
            ResultDto.CgFFBoxTurnXRectPositionCH3[3] = (RodRectVertical1[3]);

            ResultDto.CgFFBoxTurnXLightHoleCircleCenterCh3 = ResultPointX;
            ResultDto.CgFFBoxTurnXLightHoleCircleRadiusCh3 = ResultRadiusX;

            return InvokeCalibrateAsync(() =>
            {
                Logger.LogHtmlInformation("ResultImage", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    ResultPoint60X = ResultPointX,
                    ResultRadius60X = ResultRadiusX,

                    RodRectVertical120_Begin = RodRectVertical1[0],
                    RodRectVertical120_End = RodRectVertical2[0],
                    RodRectVertical120_BeginCenter = RodRectVertical1[0].Center,
                    RodRectVertical120_EndCenter = RodRectVertical2[0].Center,
                    RodWidthVertical120 = Math.Abs(RodWidthVertical2[0] - RodWidthVertical1[0]),
                    MotorMoveDistance120 = Math.Abs(Cache.CgFFBoxWidthList1[0] - Cache.CgFFBoxWidthList1[1]),
                    MotorBeginPosition120 = Cache.CgFFBoxWidthList1[0],
                    MotorEndPosition120 = Cache.CgFFBoxWidthList1[1],
                    RodWidthRelation120 = Math.Abs(RodWidthVertical2[0] - RodWidthVertical1[0]) / Math.Abs(Cache.CgFFBoxWidthList1[0] - Cache.CgFFBoxWidthList1[1]),

                    RodRectVertical180_Begin = RodRectVertical1[1],
                    RodRectVertical180_End = RodRectVertical2[1],
                    RodRectVertical180_BeginCenter = RodRectVertical1[1].Center,
                    RodRectVertical180_EndCenter = RodRectVertical2[1].Center,
                    RodWidthVertical180 = Math.Abs(RodWidthVertical2[1] - RodWidthVertical1[1]),
                    MotorMoveDistance180 = Math.Abs(Cache.CgFFBoxWidthList1[2] - Cache.CgFFBoxWidthList1[3]),
                    MotorBeginPosition180 = Cache.CgFFBoxWidthList1[2],
                    MotorEndPosition180 = Cache.CgFFBoxWidthList1[3],
                    RodWidthRelation180 = Math.Abs(RodWidthVertical2[1] - RodWidthVertical1[1]) / Math.Abs(Cache.CgFFBoxWidthList1[2] - Cache.CgFFBoxWidthList1[3]),

                    RodRectVertical240_Begin = RodRectVertical1[2],
                    RodRectVertical240_End = RodRectVertical2[2],
                    RodRectVertical240_BeginCenter = RodRectVertical1[2].Center,
                    RodRectVertical240_EndCenter = RodRectVertical2[2].Center,
                    RodWidthVertical240 = Math.Abs(RodWidthVertical2[2] - RodWidthVertical1[2]),
                    MotorMoveDistance240 = Math.Abs(Cache.CgFFBoxWidthList1[4] - Cache.CgFFBoxWidthList1[5]),
                    MotorBeginPosition240 = Cache.CgFFBoxWidthList1[4],
                    MotorEndPosition240 = Cache.CgFFBoxWidthList1[5],
                    RodWidthRelation240 = Math.Abs(RodWidthVertical2[2] - RodWidthVertical1[2]) / Math.Abs(Cache.CgFFBoxWidthList1[4] - Cache.CgFFBoxWidthList1[5]),


                    RodRectVertical300_Begin = RodRectVertical1[3],
                    RodRectVertical300_End = RodRectVertical2[3],
                    RodRectVertical300_BeginCenter = RodRectVertical1[3].Center,
                    RodRectVertical300_EndCenter = RodRectVertical2[3].Center,
                    RodWidthVertical300 = Math.Abs(RodWidthVertical2[3] - RodWidthVertical1[3]),
                    MotorMoveDistance300 = Math.Abs(Cache.CgFFBoxWidthList1[6] - Cache.CgFFBoxWidthList1[7]),
                    MotorBeginPosition300 = Cache.CgFFBoxWidthList1[6],
                    MotorEndPosition300 = Cache.CgFFBoxWidthList1[7],
                    RodWidthRelation300 = Math.Abs(RodWidthVertical2[3] - RodWidthVertical1[3]) / Math.Abs(Cache.CgFFBoxWidthList1[6] - Cache.CgFFBoxWidthList1[7])
                }), HtmlLogUniqueId.LoggingHtml());

                for (int j = 0; j < Cache.OriginImageFilePathList11.Count; j++)
                {
                    Logger.LogHtmlInformation("Ch3Image:Degree" + (j + 1), HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                    {
                        Ch3ImageAngle = Cache.OriginImageAngleList11[j],
                        Ch3ImageFilePath = Cache.OriginImageFilePathList11[j],
                        HtmlTab = new HtmlTab(new
                        {
                            Ch3Image = new HtmlImage(Cache.OriginImageFilePathList11[j])
                        })
                    }), HtmlLogUniqueId.LoggingHtml());
                }

                Logger.LogHtmlInformation("Ch3Image:TurnX" + "Position1", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    TurnXRectVertical1 = Environment.NewLine + RodRectVertical1[0],
                    TurnXRectVertical2 = Environment.NewLine + RodRectVertical2[0],
                    MotorBeginPosition1 = Cache.CgFFBoxWidthList1[0],
                    MotorEndPosition1 = Cache.CgFFBoxWidthList1[1],
                    Ch3ImageTurnX1FilePath = Cache.OriginImageFilePathList12[0],
                    Ch3ImageTurnX2FilePath = Cache.OriginImageFilePathList12[1],
                    HtmlTab = new HtmlTab(new
                    {
                        Ch3ImageTurnX1 = new HtmlImage(Cache.OriginImageFilePathList12[0], htmlImageOverlays: new[] { new HtmlImageRectangleOverlay(RodRectVertical1[0]) }),
                        Ch3ImageTurnX2 = new HtmlImage(Cache.OriginImageFilePathList12[1], htmlImageOverlays: new[] { new HtmlImageRectangleOverlay(RodRectVertical2[0]) })
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("Ch3Image:TurnX" + "Position2", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    TurnXRectVertical1 = Environment.NewLine + RodRectVertical1[1],
                    TurnXRectVertical2 = Environment.NewLine + RodRectVertical2[1],
                    MotorBeginPosition2 = Cache.CgFFBoxWidthList1[2],
                    MotorEndPosition2 = Cache.CgFFBoxWidthList1[3],
                    Ch3ImageTurnX1FilePath = Cache.OriginImageFilePathList12[2],
                    Ch3ImageTurnX2FilePath = Cache.OriginImageFilePathList12[3],
                    HtmlTab = new HtmlTab(new
                    {
                        Ch3ImageTurnX1 = new HtmlImage(Cache.OriginImageFilePathList12[2], htmlImageOverlays: new[] { new HtmlImageRectangleOverlay(RodRectVertical1[1]) }),
                        Ch3ImageTurnX2 = new HtmlImage(Cache.OriginImageFilePathList12[3], htmlImageOverlays: new[] { new HtmlImageRectangleOverlay(RodRectVertical2[1]) })
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("Ch3Image:TurnX" + "Position3", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    TurnXRectVertical1 = Environment.NewLine + RodRectVertical1[2],
                    TurnXRectVertical2 = Environment.NewLine + RodRectVertical2[2],
                    MotorBeginPosition3 = Cache.CgFFBoxWidthList1[4],
                    MotorEndPosition3 = Cache.CgFFBoxWidthList1[5],
                    Ch3ImageTurnX1FilePath = Cache.OriginImageFilePathList12[4],
                    Ch3ImageTurnX2FilePath = Cache.OriginImageFilePathList12[5],
                    HtmlTab = new HtmlTab(new
                    {
                        Ch3ImageTurnX1 = new HtmlImage(Cache.OriginImageFilePathList12[4], htmlImageOverlays: new[] { new HtmlImageRectangleOverlay(RodRectVertical1[2]) }),
                        Ch3ImageTurnX2 = new HtmlImage(Cache.OriginImageFilePathList12[5], htmlImageOverlays: new[] { new HtmlImageRectangleOverlay(RodRectVertical2[2]) })
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("Ch3Image:TurnX" + "Position4", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    TurnXRectVertical1 = Environment.NewLine + RodRectVertical1[3],
                    TurnXRectVertical2 = Environment.NewLine + RodRectVertical2[3],
                    MotorBeginPosition4 = Cache.CgFFBoxWidthList1[6],
                    MotorEndPosition4 = Cache.CgFFBoxWidthList1[7],
                    Ch3ImageTurnX1FilePath = Cache.OriginImageFilePathList12[6],
                    Ch3ImageTurnX2FilePath = Cache.OriginImageFilePathList12[7],
                    HtmlTab = new HtmlTab(new
                    {
                        Ch3ImageTurnX1 = new HtmlImage(Cache.OriginImageFilePathList12[6], htmlImageOverlays: new[] { new HtmlImageRectangleOverlay(RodRectVertical1[3]) }),
                        Ch3ImageTurnX2 = new HtmlImage(Cache.OriginImageFilePathList12[7], htmlImageOverlays: new[] { new HtmlImageRectangleOverlay(RodRectVertical2[3]) })
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                for (int j = 0; j < Cache.OriginImageFilePathList13.Count; j++)
                {
                    Logger.LogHtmlInformation("Ch3Image:LightHole" + (j + 1), HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                    {
                        Ch3ImageAngle = Cache.OriginImageAngleList13[j],
                        Ch3ImageFilePath = Cache.OriginImageFilePathList13[j],
                        HtmlTab = new HtmlTab(new
                        {
                            Ch3Image = new HtmlImage(Cache.OriginImageFilePathList13[j])
                        })
                    }), HtmlLogUniqueId.LoggingHtml());
                }

                return true;
            });
        }
        else
        {
            DialogWindowProvider.ShowDialog("Make sure you have open image of CH3 and save channel images first，then you can goto next step!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return Task.FromResult(false);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
    {
        if (Cache.Ch3Image2 != null)
        {
            ResultDto.CgFFBoxTurnYAngleCh3[0] = Cache.OriginImageAngleList21[0];
            ResultDto.CgFFBoxTurnYAngleCh3[1] = Cache.OriginImageAngleList21[1];
            ResultDto.CgFFBoxTurnYAngleCh3[2] = Cache.OriginImageAngleList21[2];
            ResultDto.CgFFBoxTurnYAngleCh3[3] = Cache.OriginImageAngleList21[3];

            ResultDto.CgFFBoxTurnYWidthCh3[0] = RodRectHorizal1[0].Height;
            ResultDto.CgFFBoxTurnYWidthCh3[1] = RodRectHorizal1[1].Height;
            ResultDto.CgFFBoxTurnYWidthCh3[2] = RodRectHorizal1[2].Height;
            ResultDto.CgFFBoxTurnYWidthCh3[3] = RodRectHorizal1[3].Height;

            ResultDto.CgFFBoxTurnYMotorRelationCH3[0] = (Math.Abs(RodHeightHorizal2[0] - RodHeightHorizal1[0]) / Math.Abs(Cache.CgFFBoxHeightList2[0] - Cache.CgFFBoxHeightList2[1]));
            ResultDto.CgFFBoxTurnYMotorRelationCH3[1] = (Math.Abs(RodHeightHorizal2[1] - RodHeightHorizal1[1]) / Math.Abs(Cache.CgFFBoxHeightList2[2] - Cache.CgFFBoxHeightList2[3]));
            ResultDto.CgFFBoxTurnYMotorRelationCH3[2] = (Math.Abs(RodHeightHorizal2[2] - RodHeightHorizal1[2]) / Math.Abs(Cache.CgFFBoxHeightList2[4] - Cache.CgFFBoxHeightList2[5]));
            ResultDto.CgFFBoxTurnYMotorRelationCH3[3] = (Math.Abs(RodHeightHorizal2[3] - RodHeightHorizal1[3]) / Math.Abs(Cache.CgFFBoxHeightList2[6] - Cache.CgFFBoxHeightList2[7]));

            ResultDto.CgFFBoxTurnYMotorPositionCH3[0] = (Cache.CgFFBoxHeightList2[0]);
            ResultDto.CgFFBoxTurnYMotorPositionCH3[1] = (Cache.CgFFBoxHeightList2[2]);
            ResultDto.CgFFBoxTurnYMotorPositionCH3[2] = (Cache.CgFFBoxHeightList2[4]);
            ResultDto.CgFFBoxTurnYMotorPositionCH3[3] = (Cache.CgFFBoxHeightList2[6]);

            ResultDto.CgFFBoxTurnYRectPositionCH3[0] = (RodRectHorizal1[0]);
            ResultDto.CgFFBoxTurnYRectPositionCH3[1] = (RodRectHorizal1[1]);
            ResultDto.CgFFBoxTurnYRectPositionCH3[2] = (RodRectHorizal1[2]);
            ResultDto.CgFFBoxTurnYRectPositionCH3[3] = (RodRectHorizal1[3]);

            ResultDto.CgFFBoxTurnYLightHoleCircleCenterCh3 = ResultPointY;
            ResultDto.CgFFBoxTurnYLightHoleCircleRadiusCh3 = ResultRadiusY;

            return InvokeCalibrateAsync(() =>
            {
                Logger.LogHtmlInformation("ResultImage", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    ResultPoint60Y = ResultPointY,
                    ResultRadius60Y = ResultRadiusY,

                    RodRectHorizal120_Begin = RodRectHorizal1[0],
                    RodRectHorizal120_End = RodRectHorizal2[0],
                    RodRectHorizal120_BeginCenter = RodRectHorizal1[0].Center,
                    RodRectHorizal120_EndCenter = RodRectHorizal2[0].Center,
                    RodHeightHorizal120 = Math.Abs(RodHeightHorizal2[0] - RodHeightHorizal1[0]),
                    MotorMoveDistance120 = Math.Abs(Cache.CgFFBoxHeightList2[0] - Cache.CgFFBoxHeightList2[1]),
                    MotorBeginPosition120 = Cache.CgFFBoxHeightList2[0],
                    MotorEndPosition120 = Cache.CgFFBoxHeightList2[1],
                    RodHeightRelation120 = Math.Abs(RodHeightHorizal2[0] - RodHeightHorizal1[0]) / Math.Abs(Cache.CgFFBoxHeightList2[0] - Cache.CgFFBoxHeightList2[1]),

                    RodRectHorizal180_Begin = RodRectHorizal1[1],
                    RodRectHorizal180_End = RodRectHorizal2[1],
                    RodRectHorizal180_BeginCenter = RodRectHorizal1[1].Center,
                    RodRectHorizal180_EndCenter = RodRectHorizal2[1].Center,
                    RodHeightHorizal180 = Math.Abs(RodHeightHorizal2[1] - RodHeightHorizal1[1]),
                    MotorMoveDistance180 = Math.Abs(Cache.CgFFBoxHeightList2[2] - Cache.CgFFBoxHeightList2[3]),
                    MotorBeginPosition180 = Cache.CgFFBoxHeightList2[2],
                    MotorEndPosition180 = Cache.CgFFBoxHeightList2[3],
                    RodHeightRelation180 = Math.Abs(RodHeightHorizal2[1] - RodHeightHorizal1[1]) / Math.Abs(Cache.CgFFBoxHeightList2[2] - Cache.CgFFBoxHeightList2[3]),

                    RodRectHorizal240_Begin = RodRectHorizal1[2],
                    RodRectHorizal240_End = RodRectHorizal2[2],
                    RodRectHorizal240_BeginCenter = RodRectHorizal1[2].Center,
                    RodRectHorizal240_EndCenter = RodRectHorizal2[2].Center,
                    RodHeightHorizal240 = Math.Abs(RodHeightHorizal2[2] - RodHeightHorizal1[2]),
                    MotorMoveDistance240 = Math.Abs(Cache.CgFFBoxHeightList2[4] - Cache.CgFFBoxHeightList2[5]),
                    MotorBeginPosition240 = Cache.CgFFBoxHeightList2[4],
                    MotorEndPosition240 = Cache.CgFFBoxHeightList2[5],
                    RodHeightRelation240 = Math.Abs(RodHeightHorizal2[2] - RodHeightHorizal1[2]) / Math.Abs(Cache.CgFFBoxHeightList2[4] - Cache.CgFFBoxHeightList2[5]),

                    RodRectHorizal300_Begin = RodRectHorizal1[3],
                    RodRectHorizal300_End = RodRectHorizal2[3],
                    RodRectHorizal300_BeginCenter = RodRectHorizal1[3].Center,
                    RodRectHorizal300_EndCenter = RodRectHorizal2[3].Center,
                    RodHeightHorizal300 = Math.Abs(RodHeightHorizal2[3] - RodHeightHorizal1[3]),
                    MotorMoveDistance300 = Math.Abs(Cache.CgFFBoxHeightList2[6] - Cache.CgFFBoxHeightList2[7]),
                    MotorBeginPosition300 = Cache.CgFFBoxHeightList2[6],
                    MotorEndPosition300 = Cache.CgFFBoxHeightList2[7],
                    RodHeightRelation300 = Math.Abs(RodHeightHorizal2[3] - RodHeightHorizal1[3]) / Math.Abs(Cache.CgFFBoxHeightList2[6] - Cache.CgFFBoxHeightList2[7])
                }), HtmlLogUniqueId.LoggingHtml());

                for (int j = 0; j < Cache.OriginImageFilePathList21.Count; j++)
                {
                    Logger.LogHtmlInformation("Ch3Image:Degree" + (j + 1), HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                    {
                        Ch3ImageAngle = Cache.OriginImageAngleList21[j],
                        Ch3ImageFilePath = Cache.OriginImageFilePathList21[j],
                        HtmlTab = new HtmlTab(new
                        {
                            Ch3Image = new HtmlImage(Cache.OriginImageFilePathList21[j])
                        })
                    }), HtmlLogUniqueId.LoggingHtml());
                }

                Logger.LogHtmlInformation("Ch3Image:TurnY" + "Position1", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    TurnYRectHorizal1 = Environment.NewLine + RodRectHorizal1[0],
                    TurnYRectHorizal2 = Environment.NewLine + RodRectHorizal2[0],
                    MotorBeginPosition1 = Cache.CgFFBoxHeightList2[0],
                    MotorEndPosition1 = Cache.CgFFBoxHeightList2[1],
                    Ch3ImageTurnY1FilePath = Cache.OriginImageFilePathList22[0],
                    Ch3ImageTurnY2FilePath = Cache.OriginImageFilePathList22[1],
                    HtmlTab = new HtmlTab(new
                    {
                        Ch3ImageTurnY1 = new HtmlImage(Cache.OriginImageFilePathList22[0], htmlImageOverlays: new[] { new HtmlImageRectangleOverlay(RodRectHorizal1[0]) }),
                        Ch3ImageTurnY2 = new HtmlImage(Cache.OriginImageFilePathList22[1], htmlImageOverlays: new[] { new HtmlImageRectangleOverlay(RodRectHorizal2[0]) })
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("Ch3Image:TurnY" + "Position2", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    TurnYRectHorizal1 = Environment.NewLine + RodRectHorizal1[1],
                    TurnYRectHorizal2 = Environment.NewLine + RodRectHorizal2[1],
                    MotorBeginPosition2 = Cache.CgFFBoxHeightList2[2],
                    MotorEndPosition2 = Cache.CgFFBoxHeightList2[3],
                    Ch3ImageTurnY1FilePath = Cache.OriginImageFilePathList22[2],
                    Ch3ImageTurnY2FilePath = Cache.OriginImageFilePathList22[3],
                    HtmlTab = new HtmlTab(new
                    {
                        Ch3ImageTurnY1 = new HtmlImage(Cache.OriginImageFilePathList22[2], htmlImageOverlays: new[] { new HtmlImageRectangleOverlay(RodRectHorizal1[1]) }),
                        Ch3ImageTurnY2 = new HtmlImage(Cache.OriginImageFilePathList22[3], htmlImageOverlays: new[] { new HtmlImageRectangleOverlay(RodRectHorizal2[1]) })
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("Ch3Image:TurnY" + "Position3", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    TurnYRectHorizal1 = Environment.NewLine + RodRectHorizal1[2],
                    TurnYRectHorizal2 = Environment.NewLine + RodRectHorizal2[2],
                    MotorBeginPosition3 = Cache.CgFFBoxHeightList2[4],
                    MotorEndPosition3 = Cache.CgFFBoxHeightList2[5],
                    Ch3ImageTurnY1FilePath = Cache.OriginImageFilePathList22[4],
                    Ch3ImageTurnY2FilePath = Cache.OriginImageFilePathList22[5],
                    HtmlTab = new HtmlTab(new
                    {
                        Ch3ImageTurnY1 = new HtmlImage(Cache.OriginImageFilePathList22[4], htmlImageOverlays: new[] { new HtmlImageRectangleOverlay(RodRectHorizal1[2]) }),
                        Ch3ImageTurnY2 = new HtmlImage(Cache.OriginImageFilePathList22[5], htmlImageOverlays: new[] { new HtmlImageRectangleOverlay(RodRectHorizal2[2]) })
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("Ch3Image:TurnY" + "Position4", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    TurnYRectHorizal1 = Environment.NewLine + RodRectHorizal1[3],
                    TurnYRectHorizal2 = Environment.NewLine + RodRectHorizal2[3],
                    MotorBeginPosition4 = Cache.CgFFBoxHeightList2[6],
                    MotorEndPosition4 = Cache.CgFFBoxHeightList2[7],
                    Ch3ImageTurnY1FilePath = Cache.OriginImageFilePathList22[6],
                    Ch3ImageTurnY2FilePath = Cache.OriginImageFilePathList22[7],
                    HtmlTab = new HtmlTab(new
                    {
                        Ch3ImageTurnY1 = new HtmlImage(Cache.OriginImageFilePathList22[6], htmlImageOverlays: new[] { new HtmlImageRectangleOverlay(RodRectHorizal1[3]) }),
                        Ch3ImageTurnY2 = new HtmlImage(Cache.OriginImageFilePathList22[7], htmlImageOverlays: new[] { new HtmlImageRectangleOverlay(RodRectHorizal2[3]) })
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                for (int j = 0; j < Cache.OriginImageFilePathList23.Count; j++)
                {
                    Logger.LogHtmlInformation("Ch3Image:LightHole" + (j + 1), HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                    {
                        Ch3ImageAngle = Cache.OriginImageAngleList23[j],
                        Ch3ImageFilePath = Cache.OriginImageFilePathList23[j],
                        HtmlTab = new HtmlTab(new
                        {
                            Ch3Image = new HtmlImage(Cache.OriginImageFilePathList23[j])
                        })
                    }), HtmlLogUniqueId.LoggingHtml());
                }

                return true;
            });
        }
        else
        {
            DialogWindowProvider.ShowDialog("Make sure you have open image of CH3 and save channel images first，then you can goto next step!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return Task.FromResult(false);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3Async(CancellationToken cancellationToken)
    {
        if (Cache.Ch3Image3 != null)
        {
            ResultDto.CgFFBoxPushXWidthCh3 = PushRectVertical1.Width;
            ResultDto.CgFFBoxPushXMotorRelationCH3 = Math.Abs(PushWidthVertical2 - PushWidthVertical1) / Math.Abs(Cache.CgFFBoxWidthList3[0] - Cache.CgFFBoxWidthList3[1]);
            ResultDto.CgFFBoxPushXMotorPositionCH3 = Cache.CgFFBoxWidthList3[0];
            ResultDto.CgFFBoxPushXRectPositionCH3 = PushRectVertical1;

            return InvokeCalibrateAsync(() =>
            {
                Logger.LogHtmlInformation("ResultImage", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    PushRectVertical1 = PushRectVertical1,
                    PushRectVertical2 = PushRectVertical2,
                    PushRectVertical1Center = PushRectVertical1.Center,
                    PushRectVertical2Center = PushRectVertical2.Center,
                    RodWidthVertical = Math.Abs(PushWidthVertical2 - PushWidthVertical1),
                    MotorMoveDistance1 = Math.Abs(Cache.CgFFBoxWidthList3[0] - Cache.CgFFBoxWidthList3[1]),
                    MotorBeginPosition1 = Cache.CgFFBoxWidthList3[0],
                    MotorEndPosition1 = Cache.CgFFBoxWidthList3[1],
                    RodWidthRelation = Math.Abs(PushWidthVertical2 - PushWidthVertical1) / Math.Abs(Cache.CgFFBoxWidthList3[0] - Cache.CgFFBoxWidthList3[1])
                }), HtmlLogUniqueId.LoggingHtml());

                for (int j = 0; j < Cache.OriginImageFilePathList31.Count; j++)
                {
                    Logger.LogHtmlInformation("Ch3Image:PushX" + (j + 1), HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                    {
                        Ch3ImageFilePath = Cache.OriginImageFilePathList31[j],
                        HtmlTab = new HtmlTab(new
                        {
                            Ch3Image = new HtmlImage(Cache.OriginImageFilePathList31[j])
                        })
                    }), HtmlLogUniqueId.LoggingHtml());
                }

                Logger.LogHtmlInformation("Ch3Image:PushXRect", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    PushXRectVertical1 = Environment.NewLine + PushRectVertical1,
                    PushXRectVertical2 = Environment.NewLine + PushRectVertical2,
                    MotorBeginPosition1 = Cache.CgFFBoxWidthList3[0],
                    MotorEndPosition1 = Cache.CgFFBoxWidthList3[1],
                    Ch3ImagePushX1FilePath = Cache.OriginImageFilePathList32[0],
                    Ch3ImagePushX2FilePath = Cache.OriginImageFilePathList32[1],
                    HtmlTab = new HtmlTab(new
                    {
                        Ch3ImagePushX1 = new HtmlImage(Cache.OriginImageFilePathList32[0], htmlImageOverlays: new[] { new HtmlImageRectangleOverlay(PushRectVertical1) }),
                        Ch3ImagePushX2 = new HtmlImage(Cache.OriginImageFilePathList32[1], htmlImageOverlays: new[] { new HtmlImageRectangleOverlay(PushRectVertical2) })
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                return true;
            });
        }
        else
        {
            DialogWindowProvider.ShowDialog("Make sure you have open image of CH3 and save channel images first，then you can goto next step!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return Task.FromResult(false);
        }
    }

    [RelayCommand]
    private async Task<bool> GetImageDegree11Async()
    {
        return await Task.Run(() =>
        {
            calibrationFlourierService.SetFFRPOS_CH3(FFCH.Ch3_X, Ch3Angle);
            calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_X, 0);
            calibrationFlourierService.SetFFPPOS_CH3(FFCH.Ch3_X, 0);

            calibrationFlourierService.SetFFRPOS_CH3(FFCH.Ch3_Y, 0);
            calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_Y, 0);

            var ret = calibrationFlourierService.GetFFReviewImgForTrigger(2, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
            if (ret.IsSuccess == true)
            {
                var originPicture0 = ret.Anything;
                if (originPicture0 == null)
                    return false;

                using var bitmap = BytesToBitmapImage(originPicture0);
                if (bitmap == null)
                    return false;

                var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh3Position.X, (int)PupilCameraAlignmentValue.RectCh3Position.Y, PupilCameraAlignmentValue.Ch3ImageWidth, PupilCameraAlignmentValue.Ch3ImageHeight);
                Cache.BitmapImageDrawable1.BitmapImage = croppedImage.ToBitmapImage();

                var OriginImageFilePath1 = Path.Combine(ImageFileDirectory, "CH3DegreeX", Ch3Angle + "_" + "0" + "_" + $"{Guid.NewGuid():N}.jpg");
                Cache.BitmapImageDrawable1.BitmapImage.Save(OriginImageFilePath1);

                Cache.Ch3Image1 = Cache.BitmapImageDrawable1.BitmapImage;
                if (SelectedCh3PositionType == PositionShowType.Position1Angle120)
                {
                    Cache.OriginImageFilePathList11[0] = OriginImageFilePath1;
                    Cache.OriginImageAngleList11[0] = Ch3Angle;
                }

                if (SelectedCh3PositionType == PositionShowType.Position2Angle180)
                {
                    Cache.OriginImageFilePathList11[1] = OriginImageFilePath1;
                    Cache.OriginImageAngleList11[1] = Ch3Angle;
                }

                if (SelectedCh3PositionType == PositionShowType.Position3Angle240)
                {
                    Cache.OriginImageFilePathList11[2] = OriginImageFilePath1;
                    Cache.OriginImageAngleList11[2] = Ch3Angle;
                }

                if (SelectedCh3PositionType == PositionShowType.Position4Angle300)
                {
                    Cache.OriginImageFilePathList11[3] = OriginImageFilePath1;
                    Cache.OriginImageAngleList11[3] = Ch3Angle;
                }
            }

            return true;
        });

        Application.Current.Dispatcher.Invoke(() =>
        {
            var newList = new ObservableCollection<RectROIDrawable>();
            Cache.RectROIDrawableList = newList;
        });
    }

    [RelayCommand]
    private async Task<bool> GetImageTurn_X11Async()
    {
        return await Task.Run(() =>
        {
            calibrationFlourierService.SetFFRPOS_CH3(FFCH.Ch3_X, Ch3Angle);
            calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_X, Ch3TurnX);
            calibrationFlourierService.SetFFPPOS_CH3(FFCH.Ch3_X, 0);

            calibrationFlourierService.SetFFRPOS_CH3(FFCH.Ch3_Y, 0);
            calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_Y, 0);

            var ret = calibrationFlourierService.GetFFReviewImgForTrigger(2, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
            if (ret.IsSuccess == true)
            {
                var originPicture0 = ret.Anything;
                if (originPicture0 == null)
                    return false;

                using var bitmap = BytesToBitmapImage(originPicture0);
                if (bitmap == null)
                    return false;
                var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh3Position.X, (int)PupilCameraAlignmentValue.RectCh3Position.Y, PupilCameraAlignmentValue.Ch3ImageWidth, PupilCameraAlignmentValue.Ch3ImageHeight);
                Cache.BitmapImageDrawable1.BitmapImage = croppedImage.ToBitmapImage();
            }

            return true;
        });
    }

    [RelayCommand]
    private async Task<bool> GetImageDegree21Async()
    {
        return await Task.Run(() =>
        {
            calibrationFlourierService.SetFFRPOS_CH3(FFCH.Ch3_X, 0);
            calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_X, 0);
            calibrationFlourierService.SetFFPPOS_CH3(FFCH.Ch3_X, 0);

            calibrationFlourierService.SetFFRPOS_CH3(FFCH.Ch3_Y, Ch3Angle);
            calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_Y, 0);

            var ret = calibrationFlourierService.GetFFReviewImgForTrigger(2, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
            if (ret.IsSuccess == true)
            {
                var originPicture0 = ret.Anything;
                if (originPicture0 == null)
                    return false;

                using var bitmap = BytesToBitmapImage(originPicture0);
                if (bitmap == null)
                    return false;

                var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh3Position.X, (int)PupilCameraAlignmentValue.RectCh3Position.Y, PupilCameraAlignmentValue.Ch3ImageWidth, PupilCameraAlignmentValue.Ch3ImageHeight);
                Cache.BitmapImageDrawable2.BitmapImage = croppedImage.ToBitmapImage();

                var OriginImageFilePath1 = Path.Combine(ImageFileDirectory, "CH3DegreeY", Ch3Angle + "_" + "0" + "_" + $"{Guid.NewGuid():N}.jpg");
                Cache.BitmapImageDrawable2.BitmapImage.Save(OriginImageFilePath1);

                Cache.Ch3Image2 = Cache.BitmapImageDrawable2.BitmapImage;
                if (SelectedCh3PositionType == PositionShowType.Position1Angle120)
                {
                    Cache.OriginImageFilePathList21[0] = OriginImageFilePath1;
                    Cache.OriginImageAngleList21[0] = Ch3Angle;
                }

                if (SelectedCh3PositionType == PositionShowType.Position2Angle180)
                {
                    Cache.OriginImageFilePathList21[1] = OriginImageFilePath1;
                    Cache.OriginImageAngleList21[1] = Ch3Angle;
                }

                if (SelectedCh3PositionType == PositionShowType.Position3Angle240)
                {
                    Cache.OriginImageFilePathList21[2] = OriginImageFilePath1;
                    Cache.OriginImageAngleList21[2] = Ch3Angle;
                }

                if (SelectedCh3PositionType == PositionShowType.Position4Angle300)
                {
                    Cache.OriginImageFilePathList21[3] = OriginImageFilePath1;
                    Cache.OriginImageAngleList21[3] = Ch3Angle;
                }
            }

            return true;
        });

        Application.Current.Dispatcher.Invoke(() =>
        {
            // 直接替换集合引用，绑定的依赖属性会收到 PropertyChanged 回调
            var newList = new ObservableCollection<RectROIDrawable>();
            Cache.RectROIDrawableList = newList;
        });
    }

    [RelayCommand]
    private async Task<bool> GetImageTurn_Y21Async()
    {
        return await Task.Run(() =>
        {
            calibrationFlourierService.SetFFRPOS_CH3(FFCH.Ch3_X, 0);
            calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_X, 0);
            calibrationFlourierService.SetFFPPOS_CH3(FFCH.Ch3_X, 0);

            calibrationFlourierService.SetFFRPOS_CH3(FFCH.Ch3_Y, Ch3Angle);
            calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_Y, Ch3TurnY);

            var ret = calibrationFlourierService.GetFFReviewImgForTrigger(2, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
            if (ret.IsSuccess == true)
            {
                var originPicture0 = ret.Anything;
                if (originPicture0 == null)
                    return false;

                using var bitmap = BytesToBitmapImage(originPicture0);
                if (bitmap == null)
                    return false;
                var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh3Position.X, (int)PupilCameraAlignmentValue.RectCh3Position.Y, PupilCameraAlignmentValue.Ch3ImageWidth, PupilCameraAlignmentValue.Ch3ImageHeight);
                Cache.BitmapImageDrawable2.BitmapImage = croppedImage.ToBitmapImage();
            }

            return true;
        });
    }

    [RelayCommand]
    private async Task<bool> GetImagePush_X31Async()
    {
        return await Task.Run(() =>
        {
            calibrationFlourierService.SetFFHome(FFCH.Ch1);
            calibrationFlourierService.SetFFHome(FFCH.Ch2);
            calibrationFlourierService.SetFFHome(FFCH.Ch3_X);
            calibrationFlourierService.SetFFHome(FFCH.Ch3_Y);

            calibrationFlourierService.SetFFRPOS_CH3(FFCH.Ch3_Y, 0);
            calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_Y, 0);
            calibrationFlourierService.SetFFRPOS_CH3(FFCH.Ch3_X, 0);
            calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_X, 0);
            calibrationFlourierService.SetFFPPOS_CH3(FFCH.Ch3_X, Ch3Push);

            var ret = calibrationFlourierService.GetFFReviewImgForTrigger(2, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
            if (ret.IsSuccess == true)
            {
                var originPicture0 = ret.Anything;
                if (originPicture0 == null)
                    return false;

                using var bitmap = BytesToBitmapImage(originPicture0);
                if (bitmap == null)
                    return false;
                var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh3Position.X, (int)PupilCameraAlignmentValue.RectCh3Position.Y, PupilCameraAlignmentValue.Ch3ImageWidth, PupilCameraAlignmentValue.Ch3ImageHeight);
                Cache.BitmapImageDrawable3.BitmapImage = croppedImage.ToBitmapImage();

                var OriginImageFilePath1 = Path.Combine(ImageFileDirectory, "CH3PushX", Ch3Angle + "_" + Ch3Push + "_" + $"{Guid.NewGuid():N}.jpg");
                Cache.BitmapImageDrawable3.BitmapImage.Save(OriginImageFilePath1);

                Cache.Ch3Image3 = Cache.BitmapImageDrawable3.BitmapImage;
                Cache.OriginImageFilePathList31.Add(OriginImageFilePath1);
            }

            return true;
        });
    }

    // 添加这个部分方法 - 当 SelectedCh3ShowType 改变时自动调用
    partial void OnSelectedCh3ShowTypeChanged(ChartShowType value)
    {
        // 执行与所选类型相关的业务逻辑
        ExecuteCh3ShowTypeLogic(value);
    }

    // 添加私有方法来处理不同的业务逻辑
    private void ExecuteCh3ShowTypeLogic(ChartShowType chartShowType)
    {
        switch (chartShowType)
        {
            case ChartShowType.ID_0_Style:
                // 执行 swath_all_id 相关的业务逻辑
                HandleId0();
                break;
            case ChartShowType.ID_1_Style:
                // 执行 swath_odd_even 相关的业务逻辑
                HandleId1();
                break;
            case ChartShowType.ID_3_Style:
                // 执行 swathid_pmtid 相关的业务逻辑
                HandleId3();
                break;
            case ChartShowType.ID_4_Style:
                // 执行 swath_all_id 相关的业务逻辑
                HandleId4();
                break;
            default:
                // 处理未定义的枚举值
                MessageBox.Show($"未处理的 ChartShowType: {chartShowType}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
        }
    }

    // 添加具体处理方法的实现（根据你的实际业务需求）
    private void HandleId0()
    {
        if (Cache.BitmapImageDrawable1?.BitmapImage != null)
        {
            // 示例逻辑：创建特定的矩形配置
            var newList = new ObservableCollection<RectROIDrawable>();
            var imageRect = new Rect(new Point(XStartPixel + 0 * RodWidth, 20), new Size(1900, RodHight + 150));
            var cartesianRect = Cache.BitmapImageDrawable1.ImageCoordinateToCartesianCoordinate(imageRect);
            newList.Add(new RectROIDrawable { Rect = cartesianRect });

            // 更新UI线程
            Application.Current.Dispatcher.Invoke(() => { Cache.RectROIDrawableList = newList; });
        }
    }

    private void HandleId1()
    {
        if (Cache.BitmapImageDrawable1?.BitmapImage != null)
        {
            // 示例逻辑：创建特定的矩形配置
            var newList = new ObservableCollection<RectROIDrawable>();
            var imageRect = new Rect(new Point(XStartPixel + 0 * RodWidth, 20), new Size(1900, RodHight + 250));
            var cartesianRect = Cache.BitmapImageDrawable1.ImageCoordinateToCartesianCoordinate(imageRect);
            newList.Add(new RectROIDrawable { Rect = cartesianRect });
            // 更新UI线程
            Application.Current.Dispatcher.Invoke(() => { Cache.RectROIDrawableList = newList; });
        }
    }

    private void HandleId3()
    {
        if (Cache.BitmapImageDrawable1?.BitmapImage != null)
        {
            // 示例逻辑：创建特定的矩形配置
            var newList = new ObservableCollection<RectROIDrawable>();
            // 根据 swath_all_id 业务逻辑创建矩形
            var imageRect = new Rect(new Point(XStartPixel + 850, 0), new Size(RodWidth + 150, 990));
            var cartesianRect = Cache.BitmapImageDrawable1.ImageCoordinateToCartesianCoordinate(imageRect);
            newList.Add(new RectROIDrawable { Rect = cartesianRect });
            // 更新UI线程
            Application.Current.Dispatcher.Invoke(() => { Cache.RectROIDrawableList = newList; });
        }
    }

    private void HandleId4()
    {
        if (Cache.BitmapImageDrawable1?.BitmapImage != null)
        {
            // 示例逻辑：创建特定的矩形配置
            var newList = new ObservableCollection<RectROIDrawable>();
            // 根据 swath_all_id 业务逻辑创建矩形
            var imageRect = new Rect(new Point(XStartPixel + 850, 0), new Size(RodWidth + 250, 990));
            var cartesianRect = Cache.BitmapImageDrawable1.ImageCoordinateToCartesianCoordinate(imageRect);
            newList.Add(new RectROIDrawable { Rect = cartesianRect });
            // 更新UI线程
            Application.Current.Dispatcher.Invoke(() => { Cache.RectROIDrawableList = newList; });
        }
    }

    private void HandleId6(double rx, double ry, double radius)
    {
        if (Cache.BitmapImageDrawable1?.BitmapImage == null) return;
        // 在图像像素坐标系中定义圆（可按需修改为 UI 可配置）
        double centerXImage = rx; // 圆心 X（像素）
        double centerYImage = ry; // 圆心 Y（像素）
        double radiusPx = radius; // 半径（像素）

        // 把像素坐标的圆心转换为笛卡尔世界坐标
        var centerWorld = Cache.BitmapImageDrawable1.ImageCoordinateToCartesianCoordinate(new Point(centerXImage, centerYImage));

        // 把圆周上一点（中心 + 半径）也转换为世界坐标，用以计算世界单位下的半径
        var radiusWorldPoint = Cache.BitmapImageDrawable1.ImageCoordinateToCartesianCoordinate(new Point(centerXImage + radiusPx, centerYImage));

        var dx = radiusWorldPoint.X - centerWorld.X;
        var dy = radiusWorldPoint.Y - centerWorld.Y;
        var worldRadius = Math.Sqrt(dx * dx + dy * dy);

        // 构造世界坐标系的 Circle，并创建 CircleROIDrawable
        var circle = new Net.Utilities.Models.Geometries.Circle(centerWorld, worldRadius);
        var circleDrawable = new CircleROIDrawable { Circle = circle };

        // 在 UI 线程赋值到 ViewModel 的 CircleROIDrawable（绑定到 Canvas 的依赖属性会更新画布）
        Application.Current.Dispatcher.Invoke(() =>
        {
            // ✅ 关键1：先清空 RectROIDrawableList（可选，根据需求）
            Cache.RectROIDrawableList = new ObservableCollection<RectROIDrawable>();

            // ✅ 关键2：显式触发 CircleROIDrawable 变化
            Cache.CircleROIDrawable = null; // 先设为 null，确保 PropertyChanged 触发
            Cache.CircleROIDrawable = circleDrawable; // 再赋新值
        });
    }

    [RelayCommand]
    private void GetVerticalRodRectData11()
    {
        var OriginImageFilePath1 = Path.Combine(ImageFileDirectory, "CH3TurnX", Ch3Angle + "_" + Ch3TurnX + "_" + $"{Guid.NewGuid():N}.jpg");
        Cache.BitmapImageDrawable1.BitmapImage.Save(OriginImageFilePath1);

        if (SelectedCh3PositionType == PositionShowType.Position1Angle120)
        {
            Cache.Ch3Image1 = Cache.BitmapImageDrawable1.BitmapImage;
            Cache.OriginImageFilePathList12[0] = OriginImageFilePath1;
            for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
            {
                var rectRoi = Cache.RectROIDrawableList[j];
                RodRectVertical1[0] = Cache.BitmapImageDrawable1.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                RodWidthVertical1[0] = (int)RodRectVertical1[0].X;
                RodWidthHeight = (int)RodRectVertical1[0].Width;
                Cache.CgFFBoxWidthList1[0] = Ch3TurnX;
                break;
            }
        }

        if (SelectedCh3PositionType == PositionShowType.Position2Angle180)
        {
            Cache.Ch3Image1 = Cache.BitmapImageDrawable1.BitmapImage;
            Cache.OriginImageFilePathList12[2] = OriginImageFilePath1;
            for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
            {
                var rectRoi = Cache.RectROIDrawableList[j];
                RodRectVertical1[1] = Cache.BitmapImageDrawable1.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                RodWidthVertical1[1] = (int)RodRectVertical1[1].X;
                RodWidthHeight = (int)RodRectVertical1[1].Width;
                Cache.CgFFBoxWidthList1[2] = Ch3TurnX;
                break;
            }
        }

        if (SelectedCh3PositionType == PositionShowType.Position3Angle240)
        {
            Cache.Ch3Image1 = Cache.BitmapImageDrawable1.BitmapImage;
            Cache.OriginImageFilePathList12[4] = OriginImageFilePath1;
            for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
            {
                var rectRoi = Cache.RectROIDrawableList[j];
                RodRectVertical1[2] = Cache.BitmapImageDrawable1.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                RodWidthVertical1[2] = (int)RodRectVertical1[2].X;
                RodWidthHeight = (int)RodRectVertical1[2].Width;
                Cache.CgFFBoxWidthList1[4] = Ch3TurnX;
                break;
            }
        }

        if (SelectedCh3PositionType == PositionShowType.Position4Angle300)
        {
            Cache.Ch3Image1 = Cache.BitmapImageDrawable1.BitmapImage;
            Cache.OriginImageFilePathList12[6] = OriginImageFilePath1;
            for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
            {
                var rectRoi = Cache.RectROIDrawableList[j];
                RodRectVertical1[3] = Cache.BitmapImageDrawable1.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                RodWidthVertical1[3] = (int)RodRectVertical1[3].X;
                RodWidthHeight = (int)RodRectVertical1[3].Width;
                Cache.CgFFBoxWidthList1[6] = Ch3TurnX;
                break;
            }
        }

        return;
    }

    [RelayCommand]
    private void GetVerticalRodRectData12()
    {
        var OriginImageFilePath1 = Path.Combine(ImageFileDirectory, "CH3TurnX", Ch3Angle + "_" + Ch3TurnX + "_" + $"{Guid.NewGuid():N}.jpg");
        Cache.BitmapImageDrawable1.BitmapImage.Save(OriginImageFilePath1);

        if (SelectedCh3PositionType == PositionShowType.Position1Angle120)
        {
            Cache.Ch3Image1 = Cache.BitmapImageDrawable1.BitmapImage;
            Cache.OriginImageFilePathList12[1] = OriginImageFilePath1;
            for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
            {
                var rectRoi = Cache.RectROIDrawableList[j];
                RodRectVertical2[0] = Cache.BitmapImageDrawable1.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                RodWidthVertical2[0] = (int)RodRectVertical2[0].X;
                RodWidthHeight = (int)RodRectVertical2[0].Width;
                Cache.CgFFBoxWidthList1[1] = Ch3TurnX;
                break;
            }
        }

        if (SelectedCh3PositionType == PositionShowType.Position2Angle180)
        {
            Cache.Ch3Image1 = Cache.BitmapImageDrawable1.BitmapImage;
            Cache.OriginImageFilePathList12[3] = OriginImageFilePath1;
            for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
            {
                var rectRoi = Cache.RectROIDrawableList[j];
                RodRectVertical2[1] = Cache.BitmapImageDrawable1.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                RodWidthVertical2[1] = (int)RodRectVertical2[1].X;
                RodWidthHeight = (int)RodRectVertical2[1].Width;
                Cache.CgFFBoxWidthList1[3] = Ch3TurnX;
                break;
            }
        }

        if (SelectedCh3PositionType == PositionShowType.Position3Angle240)
        {
            Cache.Ch3Image1 = Cache.BitmapImageDrawable1.BitmapImage;
            Cache.OriginImageFilePathList12[5] = OriginImageFilePath1;
            for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
            {
                var rectRoi = Cache.RectROIDrawableList[j];
                RodRectVertical2[2] = Cache.BitmapImageDrawable1.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                RodWidthVertical2[2] = (int)RodRectVertical2[2].X;
                RodWidthHeight = (int)RodRectVertical2[2].Width;
                Cache.CgFFBoxWidthList1[5] = Ch3TurnX;
                break;
            }
        }

        if (SelectedCh3PositionType == PositionShowType.Position4Angle300)
        {
            Cache.Ch3Image1 = Cache.BitmapImageDrawable1.BitmapImage;
            Cache.OriginImageFilePathList12[7] = OriginImageFilePath1;
            for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
            {
                var rectRoi = Cache.RectROIDrawableList[j];
                RodRectVertical2[3] = Cache.BitmapImageDrawable1.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                RodWidthVertical2[3] = (int)RodRectVertical2[3].X;
                RodWidthHeight = (int)RodRectVertical2[3].Width;
                Cache.CgFFBoxWidthList1[7] = Ch3TurnX;
                break;
            }
        }

        return;
    }

    [RelayCommand]
    private void GetHorizalRodRectData21()
    {
        var OriginImageFilePath1 = Path.Combine(ImageFileDirectory, "CH3TurnY", Ch3Angle + "_" + Ch3TurnY + "_" + $"{Guid.NewGuid():N}.jpg");
        Cache.BitmapImageDrawable2.BitmapImage.Save(OriginImageFilePath1);

        if (SelectedCh3PositionType == PositionShowType.Position1Angle120)
        {
            Cache.Ch3Image2 = Cache.BitmapImageDrawable2.BitmapImage;
            Cache.OriginImageFilePathList22[0] = OriginImageFilePath1;
            for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
            {
                var rectRoi = Cache.RectROIDrawableList[j];
                RodRectHorizal1[0] = Cache.BitmapImageDrawable2.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                RodHeightHorizal1[0] = (int)RodRectHorizal1[0].Y;
                RodWidthHeight = (int)RodRectHorizal1[0].Height;
                Cache.CgFFBoxHeightList2[0] = Ch3TurnY;
                break;
            }
        }

        if (SelectedCh3PositionType == PositionShowType.Position2Angle180)
        {
            Cache.Ch3Image2 = Cache.BitmapImageDrawable2.BitmapImage;
            Cache.OriginImageFilePathList22[2] = OriginImageFilePath1;
            for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
            {
                var rectRoi = Cache.RectROIDrawableList[j];
                RodRectHorizal1[1] = Cache.BitmapImageDrawable2.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                RodHeightHorizal1[1] = (int)RodRectHorizal1[1].Y;
                RodWidthHeight = (int)RodRectHorizal1[1].Height;
                Cache.CgFFBoxHeightList2[2] = Ch3TurnY;
                break;
            }
        }

        if (SelectedCh3PositionType == PositionShowType.Position3Angle240)
        {
            Cache.Ch3Image2 = Cache.BitmapImageDrawable2.BitmapImage;
            Cache.OriginImageFilePathList22[4] = OriginImageFilePath1;
            for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
            {
                var rectRoi = Cache.RectROIDrawableList[j];
                RodRectHorizal1[2] = Cache.BitmapImageDrawable2.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                RodHeightHorizal1[2] = (int)RodRectHorizal1[2].Y;
                RodWidthHeight = (int)RodRectHorizal1[2].Height;
                Cache.CgFFBoxHeightList2[4] = Ch3TurnY;
                break;
            }
        }

        if (SelectedCh3PositionType == PositionShowType.Position4Angle300)
        {
            Cache.Ch3Image2 = Cache.BitmapImageDrawable2.BitmapImage;
            Cache.OriginImageFilePathList22[6] = OriginImageFilePath1;
            for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
            {
                var rectRoi = Cache.RectROIDrawableList[j];
                RodRectHorizal1[3] = Cache.BitmapImageDrawable2.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                RodHeightHorizal1[3] = (int)RodRectHorizal1[3].Y;
                RodWidthHeight = (int)RodRectHorizal1[3].Height;
                Cache.CgFFBoxHeightList2[6] = Ch3TurnY;
                break;
            }
        }

        return;
    }

    [RelayCommand]
    private void GetHorizalRodRectData22()
    {
        var OriginImageFilePath1 = Path.Combine(ImageFileDirectory, "CH3TurnY", Ch3Angle + "_" + Ch3TurnY + "_" + $"{Guid.NewGuid():N}.jpg");
        Cache.BitmapImageDrawable2.BitmapImage.Save(OriginImageFilePath1);

        if (SelectedCh3PositionType == PositionShowType.Position1Angle120)
        {
            Cache.Ch3Image2 = Cache.BitmapImageDrawable2.BitmapImage;
            Cache.OriginImageFilePathList22[1] = OriginImageFilePath1;
            for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
            {
                var rectRoi = Cache.RectROIDrawableList[j];
                RodRectHorizal2[0] = Cache.BitmapImageDrawable2.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                RodHeightHorizal2[0] = (int)RodRectHorizal2[0].Y;
                RodWidthHeight = (int)RodRectHorizal2[0].Height;
                Cache.CgFFBoxHeightList2[1] = Ch3TurnY;
                break;
            }
        }

        if (SelectedCh3PositionType == PositionShowType.Position2Angle180)
        {
            Cache.Ch3Image2 = Cache.BitmapImageDrawable2.BitmapImage;
            Cache.OriginImageFilePathList22[3] = OriginImageFilePath1;
            for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
            {
                var rectRoi = Cache.RectROIDrawableList[j];
                RodRectHorizal2[1] = Cache.BitmapImageDrawable2.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                RodHeightHorizal2[1] = (int)RodRectHorizal2[1].Y;
                RodWidthHeight = (int)RodRectHorizal2[1].Height;
                Cache.CgFFBoxHeightList2[3] = Ch3TurnY;
                break;
            }
        }

        if (SelectedCh3PositionType == PositionShowType.Position3Angle240)
        {
            Cache.Ch3Image2 = Cache.BitmapImageDrawable2.BitmapImage;
            Cache.OriginImageFilePathList22[5] = OriginImageFilePath1;
            for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
            {
                var rectRoi = Cache.RectROIDrawableList[j];
                RodRectHorizal2[2] = Cache.BitmapImageDrawable2.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                RodHeightHorizal2[2] = (int)RodRectHorizal2[2].Y;
                RodWidthHeight = (int)RodRectHorizal2[2].Height;
                Cache.CgFFBoxHeightList2[5] = Ch3TurnY;
                break;
            }
        }

        if (SelectedCh3PositionType == PositionShowType.Position4Angle300)
        {
            Cache.Ch3Image2 = Cache.BitmapImageDrawable2.BitmapImage;
            Cache.OriginImageFilePathList22[7] = OriginImageFilePath1;
            for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
            {
                var rectRoi = Cache.RectROIDrawableList[j];
                RodRectHorizal2[3] = Cache.BitmapImageDrawable2.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                RodHeightHorizal2[3] = (int)RodRectHorizal2[3].Y;
                RodWidthHeight = (int)RodRectHorizal2[3].Height;
                Cache.CgFFBoxHeightList2[7] = Ch3TurnY;
                break;
            }
        }

        return;
    }

    [RelayCommand]
    private void GetVerticalRodRectData31()
    {
        var OriginImageFilePath1 = Path.Combine(ImageFileDirectory, "CH3PushX", "0" + "_" + Ch3Push + "_" + $"{Guid.NewGuid():N}.jpg");
        Cache.BitmapImageDrawable3.BitmapImage.Save(OriginImageFilePath1);

        Cache.Ch3Image3 = Cache.BitmapImageDrawable3.BitmapImage;
        Cache.OriginImageFilePathList32[0] = OriginImageFilePath1;
        for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
        {
            var rectRoi = Cache.RectROIDrawableList[j];
            PushRectVertical1 = Cache.BitmapImageDrawable3.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
            PushWidthVertical1 = (int)PushRectVertical1.X;
            RodWidthHeight = (int)PushRectVertical1.Width;
            Cache.CgFFBoxWidthList3[0] = Ch3Push;
            break;
        }

        return;
    }

    [RelayCommand]
    private void GetVerticalRodRectData32()
    {
        var OriginImageFilePath1 = Path.Combine(ImageFileDirectory, "CH3PushX", "0" + "_" + Ch3Push + "_" + $"{Guid.NewGuid():N}.jpg");
        Cache.BitmapImageDrawable3.BitmapImage.Save(OriginImageFilePath1);

        Cache.Ch3Image3 = Cache.BitmapImageDrawable3.BitmapImage;
        Cache.OriginImageFilePathList32[1] = OriginImageFilePath1;
        for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
        {
            var rectRoi = Cache.RectROIDrawableList[j];
            PushRectVertical2 = Cache.BitmapImageDrawable3.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
            PushWidthVertical2 = (int)PushRectVertical2.X;
            RodWidthHeight = (int)PushRectVertical2.Width;
            Cache.CgFFBoxWidthList3[1] = Ch3Push;
            break;
        }

        return;
    }

    [RelayCommand]
    private async Task<bool> GetLightHoleImage1Async()
    {
        return await Task.Run(() =>
        {
            calibrationFlourierService.SetFFRPOS_CH3(FFCH.Ch3_X, Ch3Angle60);
            calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_X, 0);
            calibrationFlourierService.SetFFPPOS_CH3(FFCH.Ch3_X, 0);

            calibrationFlourierService.SetFFRPOS_CH3(FFCH.Ch3_Y, 0);
            calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_Y, 0);

            var ret = calibrationFlourierService.GetFFReviewImgForTrigger(2, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
            if (ret.IsSuccess == true)
            {
                var originPicture0 = ret.Anything;
                if (originPicture0 == null)
                    return false;

                using var bitmap = BytesToBitmapImage(originPicture0);
                if (bitmap == null)
                    return false;

                var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh3Position.X, (int)PupilCameraAlignmentValue.RectCh3Position.Y, PupilCameraAlignmentValue.Ch3ImageWidth, PupilCameraAlignmentValue.Ch3ImageHeight);
                Cache.BitmapImageDrawable1.BitmapImage = croppedImage.ToBitmapImage();

                var OriginImageFilePath1 = Path.Combine(ImageFileDirectory, "Ch3Angle60X", Ch3Angle60 + "_" + "0" + "_" + $"{Guid.NewGuid():N}.jpg");
                Cache.BitmapImageDrawable1.BitmapImage.Save(OriginImageFilePath1);

                Cache.OriginImageFilePathList13.Add(OriginImageFilePath1);
                Cache.OriginImageAngleList13.Add(Ch3Angle60);
            }

            return true;
        });

        // 在后台构造集合实例（可选），然后在 UI 线程一次性替换绑定属性（触发 DP 回调）        
        Application.Current.Dispatcher.Invoke(() =>
        {
            // 直接替换集合引用，绑定的依赖属性会收到 PropertyChanged 回调
            var newList = new ObservableCollection<RectROIDrawable>();
            Cache.RectROIDrawableList = newList;
        });
    }

    [RelayCommand]
    private async Task<bool> GetLightHoleImage2Async()
    {
        return await Task.Run(() =>
        {
            calibrationFlourierService.SetFFRPOS_CH3(FFCH.Ch3_X, 0);
            calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_X, 0);
            calibrationFlourierService.SetFFPPOS_CH3(FFCH.Ch3_X, 0);

            calibrationFlourierService.SetFFRPOS_CH3(FFCH.Ch3_Y, Ch3Angle60);
            calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_Y, 0);

            var ret = calibrationFlourierService.GetFFReviewImgForTrigger(2, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
            if (ret.IsSuccess == true)
            {
                var originPicture0 = ret.Anything;
                if (originPicture0 == null)
                    return false;

                using var bitmap = BytesToBitmapImage(originPicture0);
                if (bitmap == null)
                    return false;

                var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh3Position.X, (int)PupilCameraAlignmentValue.RectCh3Position.Y, PupilCameraAlignmentValue.Ch3ImageWidth, PupilCameraAlignmentValue.Ch3ImageHeight);
                Cache.BitmapImageDrawable2.BitmapImage = croppedImage.ToBitmapImage();

                var OriginImageFilePath1 = Path.Combine(ImageFileDirectory, "Ch3Angle60Y", Ch3Angle60 + "_" + "0" + "_" + $"{Guid.NewGuid():N}.jpg");
                Cache.BitmapImageDrawable2.BitmapImage.Save(OriginImageFilePath1);

                Cache.OriginImageFilePathList23.Add(OriginImageFilePath1);
                Cache.OriginImageAngleList23.Add(Ch3Angle60);
            }

            return true;
        });

        // 在后台构造集合实例（可选），然后在 UI 线程一次性替换绑定属性（触发 DP 回调）        
        Application.Current.Dispatcher.Invoke(() =>
        {
            // 直接替换集合引用，绑定的依赖属性会收到 PropertyChanged 回调
            var newList = new ObservableCollection<RectROIDrawable>();
            Cache.RectROIDrawableList = newList;
        });
    }

    [RelayCommand]
    private async Task<bool> GetLightHoleData1Async()
    {
        return await Task.Run(() =>
        {
            List<double> yValues = new List<double>();
            List<double> xValues = new List<double>();
            for (int i = 0; i < CirclePointX.Count(); i++)
            {
                yValues.Add(CirclePointX[i].Y);
                xValues.Add(CirclePointX[i].X);
            }

            _algorithm.FindCircle(xValues, yValues, out var yValue, out var xValue, out var Radius);
            HandleId6(xValue, yValue, Radius);

            double x = xValue.ToDArr().ElementAt(0); // 或用 ToFArr() 然后转 double
            double y = yValue.ToDArr().ElementAt(0);
            ResultPointX = new Point(x, y);
            ResultRadiusX = (float)(Radius.ToDArr().ElementAt(0));

            return true;
        });
    }

    [RelayCommand]
    private async Task<bool> GetLightHoleData2Async()
    {
        return await Task.Run(() =>
        {
            List<double> yValues = new List<double>();
            List<double> xValues = new List<double>();
            for (int i = 0; i < CirclePointY.Count(); i++)
            {
                yValues.Add(CirclePointY[i].Y);
                xValues.Add(CirclePointY[i].X);
            }

            _algorithm.FindCircle(xValues, yValues, out var yValue, out var xValue, out var Radius);
            HandleId6(xValue, yValue, Radius);

            double x = xValue.ToDArr().ElementAt(0); // 或用 ToFArr() 然后转 double
            double y = yValue.ToDArr().ElementAt(0);
            ResultPointY = new Point(x, y);
            ResultRadiusY = (float)(Radius.ToDArr().ElementAt(0));

            return true;
        });
    }

    private HImage GetPictureRegion(HImage originImage, int startX, int startY, int width, int height)
    {
        // 3. 【关键】使用 CropPart 进行真实裁剪 // 参数: 原图, 起始列(Column), 起始行(Row), 宽度, 高度     
        return originImage.CropPart(startY, startX, width, height);
    }

    private bool Save(PupilCenterChannelFlexibleApertureDTO itemDto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        Calibration = itemDto.Clone();

        CacheProvider.Set(Calibration, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    public static BitmapImage BytesToBitmapImage(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return null;
        // Net.Utilities.Graphics.Primitives.Medias.Imaging.BitmapImage
        // 使用接受字节数组的构造函数（反编译源码显示有此构造函数）
        return new BitmapImage(bytes, isCopy: true);
    }
}