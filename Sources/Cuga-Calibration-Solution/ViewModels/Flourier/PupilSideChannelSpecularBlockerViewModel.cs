using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Fourier.CameraAlignment;
using Core.Models.Models.Fourier.SideChannelFlexibleAperture;
using Core.Models.Models.Fourier.SideChannelSpecularBlocker;
using Core.Models.Models.Microscope.CalChip;
using Core.Utilities.SourceGenerators.Attributes;
using HalconDotNet;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using FFCH = Core.Models.Models.Common.Fourier.FFCH;
using Point = Net.Utilities.Models.Geometries.Point;
using Rect = Net.Utilities.Models.Geometries.Rect;
using Size = Net.Utilities.Models.Geometries.Size;

namespace CugaCalibration.ViewModels.Flourier;

[IOCAppService(ServiceType = typeof(PupilSideChannelSpecularBlockerViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class PupilSideChannelSpecularBlockerViewModel : CalibrationViewModelBase
{
    #region 界面相关

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private int _rodNum = 23;

    [ObservableProperty]
    private int _rodWidth = 35;

    [ObservableProperty]
    private int _rodHight = 350;

    [ObservableProperty]
    private int _xStartPixel = 100;

    [ObservableProperty]
    private double _ch12Percentage = 0.5;

    [ObservableProperty]
    private int _imageWidthPixel = 1000;

    [ObservableProperty]
    private bool _isSyncingFromDrag;

    [ObservableProperty]
    private HTuple _meanGrayOld1 = new(), _meanGrayOld2 = new(), _meanGrayNew1 = new(), _meanGrayNew2 = new();

    // 当 RodNum/ RodWidth/ XStartPixel 改变时自动重建矩形集合
    partial void OnRodNumChanged(int value)
    {
        _ = value; // 消除 "未使用" 警告
        RebuildRectRoiDrawableList();
    }

    partial void OnRodWidthChanged(int value)
    {
        _ = value; // 消除 "未使用" 警告
        RebuildRectRoiDrawableList();
    }

    partial void OnRodHightChanged(int value)
    {
        _ = value; // 消除 "未使用" 警告
        RebuildRectRoiDrawableList();
    }

    [ObservableProperty]
    private int _imageWidth = 2048;

    [ObservableProperty]
    private int _imageHeight = 2048;

    [ObservableProperty]
    private int _imageX;

    [ObservableProperty]
    private int _imageY;

    [ObservableProperty]
    private Point _sxPos;

    [ObservableProperty]
    private ObservableCollection<Rect> _currentImageAxis = new();

    [ObservableProperty]
    private ObservableCollection<Rect> _currentImageRectListCh1 = new();

    [ObservableProperty]
    private ObservableCollection<Rect> _currentImageRectListCh2 = new();

    [ObservableProperty]
    private string[] _reviewImageShowPath = new string[2];

    [ObservableProperty]
    private string[] _reviewImageHidePath = new string[2];

    [ObservableProperty]
    private double[] _reviewImageGrayCh1 = new double[2];

    [ObservableProperty]
    private double[] _reviewImageGrayCh2 = new double[2];

    [ObservableProperty]
    private double _reviewImageCompareCh1;

    [ObservableProperty]
    private double _reviewImageCompareCh2;

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Shiny Wafer Position And Productivity Information" },
        new() { StepName = "Pupil Side Channel Specular Blocker Calibration CH1" },
        new() { StepName = "Pupil Side Channel Specular Blocker Calibration CH2" }
    ];

    [ObservableProperty]
    private ObservableCollection<RodInformation> _setAllRods = new ObservableCollection<RodInformation>
    {
        new RodInformation("Rod1", 0), new RodInformation("Rod2", 0), new RodInformation("Rod3", 0), new RodInformation("Rod4", 0), new RodInformation("Rod5", 0), new RodInformation("Rod6", 0),
        new RodInformation("Rod7", 0), new RodInformation("Rod8", 0), new RodInformation("Rod9", 0), new RodInformation("Rod10", 0), new RodInformation("Rod11", 0), new RodInformation("Rod12", 0),
        new RodInformation("Rod13", 0), new RodInformation("Rod14", 0), new RodInformation("Rod15", 0), new RodInformation("Rod16", 0), new RodInformation("Rod17", 0), new RodInformation("Rod18", 0),
        new RodInformation("Rod19", 0), new RodInformation("Rod20", 0), new RodInformation("Rod21", 0), new RodInformation("Rod22", 0), new RodInformation("Rod23", 0), new RodInformation("Rod24", 0),
        new RodInformation("Rod25", 0), new RodInformation("Rod26", 0), new RodInformation("Rod27", 0), new RodInformation("Rod28", 0), new RodInformation("Rod29", 0), new RodInformation("Rod30", 0),
        new RodInformation("Rod31", 0), new RodInformation("Rod32", 0), new RodInformation("Rod33", 0), new RodInformation("Rod34", 0), new RodInformation("Rod35", 0), new RodInformation("Rod36", 0),
        new RodInformation("Rod37", 0), new RodInformation("Rod38", 0), new RodInformation("Rod39", 0), new RodInformation("Rod40", 0), new RodInformation("Rod41", 0), new RodInformation("Rod42", 0),
        new RodInformation("Rod43", 0), new RodInformation("Rod44", 0), new RodInformation("Rod45", 0), new RodInformation("Rod46", 0)
    };

    #region Calibrate

    [ObservableProperty]
    private ObservableCollection<PupilSideChannelSpecularBlockerDTO> _resultPupilSideChannelSpecularBlockerDTOList = [];

    [ObservableProperty]
    private IReadOnlyList<OpticsIlluminationModeAndProductivityInformationStatus> _calibrationStatuses = [];

    #endregion Calibrate

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private PupilSideChannelSpecularBlockerDTO _resultDto = new();

    [ObservableProperty]
    private MicroscopeCalChipDTO _microscopeCalChip = new();

    [ObservableProperty]
    private PupilCameraAlignmentDTO _pupilCameraAlignmentValue = new();

    [ObservableProperty]
    private PupilSideChannelFlexibleApertureDTO _pupilSideChannelFlexibleApertureValue = new();

    [RecipeCache]
    [ObservableProperty]
    private PupilSideChannelSpecularBlockerCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private PupilSideChannelSpecularBlockerDTO[] _calibrations = [];

    #endregion 缓存

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        SelectedTabIndex = 0;
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false)
            return false;

        MicroscopeCalChip = CalibrationStatusService.GetCalibration<MicroscopeCalChipDTO>();
        PupilCameraAlignmentValue = CalibrationStatusService.GetCalibration<PupilCameraAlignmentDTO>();
        PupilSideChannelFlexibleApertureValue = CalibrationStatusService.GetCalibration<PupilSideChannelFlexibleApertureDTO>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<PupilSideChannelSpecularBlockerCache>();

        Calibrations = CacheProvider.GetOrDefaultArray<PupilSideChannelSpecularBlockerDTO>();

        CalibrationStatuses =
        [
            ..ApplicationCookie.OpticsIlluminationModeEnums
                .Select(t => new OpticsIlluminationModeAndProductivityInformationStatus
                {
                    SelectedItem = t,
                    ProductivityInformationStatusList = [.. ApplicationCookie.GetProductivityInformations(t).Select(tt => new ProductivityInformationStatus { SelectedItem = tt, IsCalibrated = false })]
                })
        ];

        foreach (var calibrationStatus in Calibrations)
        {
            var opticsIlluminationModeEnumStatus = CalibrationStatuses.Single(t => t.SelectedItem == calibrationStatus.OpticsIlluminationMode);
            var status = opticsIlluminationModeEnumStatus
                .ProductivityInformationStatusList
                .SingleOrDefault(t => t.SelectedItem == calibrationStatus.ProductivityInformation);
            if (status is not null) status.IsCalibrated = calibrationStatus.IsCalibrated;
        }

        if (!isHasCache) RecipeCacheProvider.Set(Cache, cancellationToken);

        ClearCalibrationTemp();

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        /*Cache.FindPosition = Cache.FindPosition.ToOriginLength >= Cache.ChuckRadius
            ? new Point(0, 0)
            : Cache.FindPosition;
        MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationInfo);
        StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);*/
        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        /*ReviewList =
        [
            .. Calibrations
                .Select(t => t.Clone())
                .OrderBy(t => t.OpticsMagTypeEnum)
                .ThenBy(t => t.StageSpeedEnum)
                .ThenBy(t => t.PmtId)
        ];

        if (ReviewList.All(t => t.IsCalibrated == false))
            return false;

        MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationInfo);
        StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);*/

        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        //var enum1 = Cache.OpticsMagTypeEnum;
        switch (CalibrationStepIndex)
        {
            case 1:
                SelectedTabIndex = 1;
                return true;

            case 2:
                SelectedTabIndex = 0;

                ResultDto.ProductivityInformation = Cache.ProductivityInformation;
                ResultDto.OpticsIlluminationMode = Cache.OpticsIlluminationModeEnum;
                ResultDto.CgFFBoxBeginPositionCh1 = Cache.Item.CgFFBoxBeginPositionCh1;
                ResultDto.CgFFBoxBeginAndEndNumberCh1 = Cache.Item.CgFFBoxBeginAndEndNumberCh1;
                ResultDto.CgFFBoxMoveDownPercentListCh1 = Cache.Item.CgFFBoxMoveDownPercentListCh1;

                ResultDto.CgFFBoxBeginPositionCh2 = Cache.Item.CgFFBoxBeginPositionCh2;
                ResultDto.CgFFBoxBeginAndEndNumberCh2 = Cache.Item.CgFFBoxBeginAndEndNumberCh2;
                ResultDto.CgFFBoxMoveDownPercentListCh2 = Cache.Item.CgFFBoxMoveDownPercentListCh2;

                SynchronizationContextProvider.Send(() => ResultPupilSideChannelSpecularBlockerDTOList.Add(ResultDto.Clone()));

                if (ResultPupilSideChannelSpecularBlockerDTOList.Count <= 0)
                {
                    DialogWindowProvider.TryShowDialog("Please find Offset!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    Calibrations = [.. Calibrations.ToList().Where(t => !(t.ProductivityInformation == Cache.ProductivityInformation))];
                    foreach (var (index, pupilSideChannelSpecularBlockerDTO) in ResultPupilSideChannelSpecularBlockerDTOList.Select((dto, i) => (i, dto)))
                    {
                        pupilSideChannelSpecularBlockerDTO.IsCalibrated = true;
                        if (Save(pupilSideChannelSpecularBlockerDTO, cancellationToken, index == ResultPupilSideChannelSpecularBlockerDTOList.Count - 1)) continue;

                        pupilSideChannelSpecularBlockerDTO.IsCalibrated = false;
                        Logger.LogError("{@Name} Error: Save Failed!", Name);
                        return false;
                    }
                }

                CalibrationStatuses.Single(t => t.SelectedItem == Cache.OpticsIlluminationModeEnum)
                    .ProductivityInformationStatusList
                    .Single(t => t.SelectedItem == Cache.ProductivityInformation).IsCalibrated = true;

                IsCalibrated = CalibrationStatuses.All(s => s.IsCalibrated);
                if (!IsCalibrated) CalibrationStepIndex = -1;

                return true;

            default:

                return true;
        }
    }

    protected override Task<bool> CancelingAsync()
    {
        //StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);
        return Task.FromResult(true);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0Async(CancellationToken cancellationToken)
    {
        FourierViewModel.SetFFHome(FFCH.Ch1);
        FourierViewModel.SetFFHome(FFCH.Ch2);
        FourierViewModel.SetFFHome(FFCH.Ch3_X);
        FourierViewModel.SetFFHome(FFCH.Ch3_Y);

        Cache.OpticsIlluminationModeEnum = Cache.ProductivityInformation.OpticsIlluminationModeEnum;
        //Cache.Item.ShinyWaferPosition = StageViewModel.GetBrightFieldStagePosition();

        Cache.Item.ShinyWaferPosition = Guard.IsNotNullAndReturn(MicroscopeCalChip.ShinyWaferItem).BrightFieldMachinePosition;
        Cache.Item.ShinyWaferPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.ShinyWaferPosition);

        AfViewModel.ToggleDarkFieldEnable(true);
        SxPos = new Point(Cache.Item.ShinyWaferPosition.X, Cache.Item.ShinyWaferPosition.Y);

        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Item.ShinyWaferPosition,
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        if (Cache.Ch1Image != null)
        {
            Cache.Item.ImageGrayCompareCh1 = $"Ch1:Image Gray Compare(old/new) value is: {MeanGrayOld1.D / MeanGrayNew1.D:F4}";
            return InvokeCalibrateAsync(() =>
            {
                Logger.LogHtmlInformation("ResultImage", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    ImageGrayCompareCh1 = $"Ch1:Image Gray Compare(old/new) value is: {MeanGrayOld1.D / MeanGrayNew1.D:F4}",
                    InitialImageFilePath1 = Cache.Item.OriginImageFilePathOld1,
                    ProcessImageFilePath1 = Cache.Item.OriginImageFilePathNew1,
                    ImageGrayOldCh1 = MeanGrayOld1.D,
                    ImageGrayNewCh1 = MeanGrayNew1.D,
                    HtmlTabCh1 = new HtmlTab(new
                    {
                        InitialImageCh1 = new HtmlImage(Cache.Item.OriginImageFilePathOld1, htmlImageOverlays:
                            [
                                .. CurrentImageRectListCh1
                                    .Skip(Cache.Item.CgFFBoxBeginAndEndNumberCh1.FirstOrDefault())
                                    .Take(Cache.Item.CgFFBoxBeginAndEndNumberCh1.LastOrDefault() - Cache.Item.CgFFBoxBeginAndEndNumberCh1.FirstOrDefault() + 1).Select(rect => new HtmlImageRectangleOverlay(rect)),
                                .. CurrentImageRectListCh1
                                    .Index()
                                    .Skip(Cache.Item.CgFFBoxBeginAndEndNumberCh1.FirstOrDefault())
                                    .Take(Cache.Item.CgFFBoxBeginAndEndNumberCh1.LastOrDefault() - Cache.Item.CgFFBoxBeginAndEndNumberCh1.FirstOrDefault() + 1).Select(t => new HtmlImageTextOverlay(t.Item.Center, t.Index.ToString()))
                            ]
                        ),
                        ProcessImageCh1 = new HtmlImage(Cache.Item.OriginImageFilePathNew1, htmlImageOverlays:
                            [
                                .. CurrentImageRectListCh1
                                    .Skip(Cache.Item.CgFFBoxBeginAndEndNumberCh1.FirstOrDefault())
                                    .Take(Cache.Item.CgFFBoxBeginAndEndNumberCh1.LastOrDefault() - Cache.Item.CgFFBoxBeginAndEndNumberCh1.FirstOrDefault() + 1).Select(rect => new HtmlImageRectangleOverlay(rect)),
                                .. CurrentImageRectListCh1
                                    .Index()
                                    .Skip(Cache.Item.CgFFBoxBeginAndEndNumberCh1.FirstOrDefault())
                                    .Take(Cache.Item.CgFFBoxBeginAndEndNumberCh1.LastOrDefault() - Cache.Item.CgFFBoxBeginAndEndNumberCh1.FirstOrDefault() + 1).Select(t => new HtmlImageTextOverlay(t.Item.Center, t.Index.ToString()))
                            ]
                        )
                    })
                }), HtmlLogUniqueId.LoggingHtml());
                return true;
            });
        }

        DialogWindowProvider.ShowDialog("Make sure you have open image of CH1、CH2、CH3 and save all channel images first，then you can goto next step!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        return Task.FromResult(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
    {
        if (Cache.Ch2Image != null)
        {
            Cache.Item.ImageGrayCompareCh2 = $"Ch2:Image Gray Compare(old/new) value is: {MeanGrayOld2.D / MeanGrayNew2.D:F4}";
            return InvokeCalibrateAsync(() =>
            {
                Cache.Item.CgFFBoxBeginAndEndNumberCh1 = [];
                for (int k = 0; k < Cache.Item.CgFFBoxMoveDownPercentListCh1.Count(); k++)
                {
                    if (Cache.Item.CgFFBoxMoveDownPercentListCh1[k] > 0)
                        Cache.Item.CgFFBoxBeginAndEndNumberCh1 = [.. Cache.Item.CgFFBoxBeginAndEndNumberCh1, k + 1];
                }

                Cache.Item.CgFFBoxBeginAndEndNumberCh2 = [];
                for (int k = 0; k < Cache.Item.CgFFBoxMoveDownPercentListCh2.Count(); k++)
                {
                    if (Cache.Item.CgFFBoxMoveDownPercentListCh2[k] > 0)
                        Cache.Item.CgFFBoxBeginAndEndNumberCh2 = [.. Cache.Item.CgFFBoxBeginAndEndNumberCh2, k + 1];
                }

                Logger.LogHtmlInformation("ResultImage", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    ImageGrayCompareCh2 = $"Ch2:Image Gray Compare(old/new) value is: {MeanGrayOld2.D / MeanGrayNew2.D:F4}",
                    InitialImageFilePath2 = Cache.Item.OriginImageFilePathOld2,
                    ProcessImageFilePath2 = Cache.Item.OriginImageFilePathNew2,
                    ImageGrayOldCh2 = MeanGrayOld2.D,
                    ImageGrayNewCh2 = MeanGrayNew2.D,
                    HtmlTabCh2 = new HtmlTab(new
                    {
                        InitialImageCh2 = new HtmlImage(Cache.Item.OriginImageFilePathOld2, htmlImageOverlays:
                            [
                                .. CurrentImageRectListCh2
                                    .Skip(Cache.Item.CgFFBoxBeginAndEndNumberCh2.FirstOrDefault())
                                    .Take(Cache.Item.CgFFBoxBeginAndEndNumberCh2.LastOrDefault() - Cache.Item.CgFFBoxBeginAndEndNumberCh2.FirstOrDefault() + 1).Select(rect => new HtmlImageRectangleOverlay(rect)),
                                .. CurrentImageRectListCh2
                                    .Index()
                                    .Skip(Cache.Item.CgFFBoxBeginAndEndNumberCh2.FirstOrDefault())
                                    .Take(Cache.Item.CgFFBoxBeginAndEndNumberCh2.LastOrDefault() - Cache.Item.CgFFBoxBeginAndEndNumberCh2.FirstOrDefault() + 1).Select(t => new HtmlImageTextOverlay(t.Item.Center, t.Index.ToString()))
                            ]
                        ),
                        ProcessImageCh2 = new HtmlImage(Cache.Item.OriginImageFilePathNew2, htmlImageOverlays:
                            [
                                .. CurrentImageRectListCh2
                                    .Skip(Cache.Item.CgFFBoxBeginAndEndNumberCh2.FirstOrDefault())
                                    .Take(Cache.Item.CgFFBoxBeginAndEndNumberCh2.LastOrDefault() - Cache.Item.CgFFBoxBeginAndEndNumberCh2.FirstOrDefault() + 1).Select(rect => new HtmlImageRectangleOverlay(rect)),
                                .. CurrentImageRectListCh2
                                    .Index()
                                    .Skip(Cache.Item.CgFFBoxBeginAndEndNumberCh2.FirstOrDefault())
                                    .Take(Cache.Item.CgFFBoxBeginAndEndNumberCh2.LastOrDefault() - Cache.Item.CgFFBoxBeginAndEndNumberCh2.FirstOrDefault() + 1).Select(t => new HtmlImageTextOverlay(t.Item.Center, t.Index.ToString()))
                            ]
                        )
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("CalibrationResult", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Cache.Item.CgFFBoxBeginPositionCh1,
                    Cache.Item.CgFFBoxBeginAndEndNumberCh1,
                    Cache.Item.CgFFBoxMoveDownPercentListCh1,
                    Cache.Item.CgFFBoxBeginPositionCh2,
                    Cache.Item.CgFFBoxBeginAndEndNumberCh2,
                    Cache.Item.CgFFBoxMoveDownPercentListCh2
                }), HtmlLogUniqueId.LoggingHtml());
                return true;
            });
        }

        DialogWindowProvider.ShowDialog("Make sure you have open image of CH1、CH2、CH3 and save all channel images first，then you can goto next step!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        return Task.FromResult(false);
    }

    // 关键：当第一个矩形被拖动时，更新 XStartPixel（但不重建！）
    private void OnFirstRectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        {
            if (e.PropertyName == nameof(RectROIDrawable.Rect) && sender is RectROIDrawable firstRect && Cache.BitmapImageDrawableCh11.BitmapImage != null)
            {
                var imageRect = Cache.BitmapImageDrawableCh11.CartesianCoordinateToImageCoordinate(firstRect.Rect);
                var newStartX = (int)Math.Round(imageRect.X);

                if (newStartX != XStartPixel)
                {
                    IsSyncingFromDrag = true;
                    try
                    {
                        XStartPixel = newStartX; // 这会触发 UI 更新，但不会重建（见下）
                    }
                    finally
                    {
                        IsSyncingFromDrag = false;
                    }
                }
            }
        }
    }

    // 阻止因 XStartPixel 变化而重建（仅当非拖动时才重建）
    partial void OnXStartPixelChanged(int value)
    {
        _ = value; // 消除 "未使用" 警告
        if (!IsSyncingFromDrag)
        {
            RebuildRectRoiDrawableList();
        }
    }

    // 在 RebuildRectRoiDrawableList() 创建新列表后，订阅第一个矩形
    private void RebuildRectRoiDrawableList()
    {
        {
            if (Cache.BitmapImageDrawableCh11.BitmapImage is null)
            {
                if (Cache.RectROIDrawableListCh11.FirstOrDefault() is { } previousFirst)
                    previousFirst.PropertyChanged -= OnFirstRectPropertyChanged;
                return;
            }

            if (SelectedTabIndex == 0)
            {
                var currentImageRectListFirstCh1 = PupilSideChannelFlexibleApertureValue.CurrentImageRectListFirstCh1;
                var newList = new ObservableCollection<RectROIDrawable>();
                for (int i = 0; i < currentImageRectListFirstCh1.Count(); i++)
                {
                    var imageRect = new Rect(new Point(currentImageRectListFirstCh1[i].X, currentImageRectListFirstCh1[i].Y), new Size(currentImageRectListFirstCh1[i].Width, 20));
                    var cartesianRect = Cache.BitmapImageDrawableCh11.ImageCoordinateToCartesianCoordinate(imageRect);
                    newList.Add(new RectROIDrawable { Rect = cartesianRect, Label = (i + 1).ToString() });
                }

                if (Cache.RectROIDrawableListCh11.FirstOrDefault() is { } oldFirst)
                    oldFirst.PropertyChanged -= OnFirstRectPropertyChanged;

                Cache.RectROIDrawableListCh11 = newList;
            }

            if (SelectedTabIndex == 1)
            {
                var currentImageRectListFirstCh2 = PupilSideChannelFlexibleApertureValue.CurrentImageRectListFirstCh2;
                var newList = new ObservableCollection<RectROIDrawable>();
                for (int i = 0; i < currentImageRectListFirstCh2.Count(); i++)
                {
                    var imageRect = new Rect(new Point(currentImageRectListFirstCh2[i].X, currentImageRectListFirstCh2[i].Y), new Size(currentImageRectListFirstCh2[i].Width, 20));
                    var cartesianRect = Cache.BitmapImageDrawableCh21.ImageCoordinateToCartesianCoordinate(imageRect);
                    newList.Add(new RectROIDrawable { Rect = cartesianRect, Label = (i + 1).ToString() });
                }

                if (Cache.RectROIDrawableListCh11.FirstOrDefault() is { } oldFirst)
                    oldFirst.PropertyChanged -= OnFirstRectPropertyChanged;

                Cache.RectROIDrawableListCh11 = newList;
            }

            // 订阅新的第一个矩形
            if (Cache.RectROIDrawableListCh11.FirstOrDefault() is { } newFirst)
                newFirst.PropertyChanged += OnFirstRectPropertyChanged;
        }
    }

    [RelayCommand]
    private async Task OpenImageFileCh12Async()
    {
        await Task.Run(() =>
        {
            int rodNum;
            var ch12List = new List<(int rodnumber, double rodpos)>();
            var result = FourierViewModel.GetFourierConfig();
            {
                rodNum = result.RodNum;
            }
            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, 0));
            }

            var ret = FourierViewModel.GetFFReviewImgForTrigger(0, Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Level, SxPos, 100);
            if (SelectedTabIndex == 0)
            {
                FourierViewModel.FF_Move_CH12(FFCH.Ch1, ch12List);
                ret = FourierViewModel.GetFFReviewImgForTrigger(0, Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Level, SxPos, 100);
            }
            else if (SelectedTabIndex == 1)
            {
                FourierViewModel.FF_Move_CH12(FFCH.Ch2, ch12List);
                ret = FourierViewModel.GetFFReviewImgForTrigger(1, Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Level, SxPos, 100);
            }

            {
                using var bitmap = BytesToBitmapImage(ret);

                if (SelectedTabIndex == 0)
                {
                    var croppedImage = GetPictureRegion(bitmap, (int)PupilCameraAlignmentValue.RectCh1Position.X, (int)PupilCameraAlignmentValue.RectCh1Position.Y, PupilCameraAlignmentValue.Ch1ImageWidth, PupilCameraAlignmentValue.Ch1ImageHeight);
                    Cache.BitmapImageDrawableCh10.BitmapImage = croppedImage;
                    Cache.Item.OriginImageFilePathOld1 = Path.Combine(ImageFileDirectory, "CH1", "Initial" + "__" + $"{Guid.NewGuid():N}.jpg");
                    Cache.BitmapImageDrawableCh10.BitmapImage.Save(Cache.Item.OriginImageFilePathOld1);
                    //calibrationAlgorithmService.GetPictureGray(BitmapImageDrawable.BitmapImage.ToHImage(), 255, out var hv_Histo);
                    //ImageGrayOldCh1 = hv_Histo.TupleSum();    
                    using var hImage = Cache.BitmapImageDrawableCh10.BitmapImage.ToHImage();
                    HOperatorSet.Intensity(hImage, hImage, out var meanGrayOld1, out _);
                    MeanGrayOld1 = meanGrayOld1;
                }
                else if (SelectedTabIndex == 1)
                {
                    var croppedImage = GetPictureRegion(bitmap, (int)PupilCameraAlignmentValue.RectCh2Position.X, (int)PupilCameraAlignmentValue.RectCh2Position.Y, PupilCameraAlignmentValue.Ch2ImageWidth, PupilCameraAlignmentValue.Ch2ImageHeight);
                    Cache.BitmapImageDrawableCh20.BitmapImage = croppedImage;
                    Cache.Item.OriginImageFilePathOld2 = Path.Combine(ImageFileDirectory, "CH2", "Initial" + "__" + $"{Guid.NewGuid():N}.jpg");
                    Cache.BitmapImageDrawableCh20.BitmapImage.Save(Cache.Item.OriginImageFilePathOld2);
                    //calibrationAlgorithmService.GetPictureGray(BitmapImageDrawable.BitmapImage.ToHImage(), 255, out var hv_Histo);
                    //ImageGrayOldCh2 = hv_Histo.TupleSum();
                    using var hImage = Cache.BitmapImageDrawableCh20.BitmapImage.ToHImage();
                    HOperatorSet.Intensity(hImage, hImage, out var meanGrayOld2, out _);
                    MeanGrayOld2 = meanGrayOld2;
                }
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task SaveImageFileCh12Async()
    {
        await Task.Run(() =>
        {
            int rodNum;
            var ch12List = new List<(int rodnumber, double rodpos)>();
            var result = FourierViewModel.GetFourierConfig();
            {
                rodNum = result.RodNum;
            }
            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, 0));
                //ch12List.Add((j, SetAllRods[j - 1].Percent));
            }

            byte[] ret;
            {
                if (SelectedTabIndex == 0)
                {
                    FourierViewModel.FF_Move_CH12(FFCH.Ch1, ch12List);
                    //await Task.Delay(2000).ConfigureAwait(false);
                    ret = FourierViewModel.GetFFReviewImgForTrigger(0, Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Level, SxPos, 100);

                    using var bitmap = BytesToBitmapImage(ret);

                    var croppedImage = GetPictureRegion(bitmap, (int)PupilCameraAlignmentValue.RectCh1Position.X, (int)PupilCameraAlignmentValue.RectCh1Position.Y, PupilCameraAlignmentValue.Ch1ImageWidth, PupilCameraAlignmentValue.Ch1ImageHeight);
                    Cache.BitmapImageDrawableCh11.BitmapImage = croppedImage;
                    Cache.Item.OriginImageFilePathNew1 = Path.Combine(ImageFileDirectory, "CH1", Ch12Percentage + "__" + $"{Guid.NewGuid():N}.jpg");
                    Cache.BitmapImageDrawableCh11.BitmapImage.Save(Cache.Item.OriginImageFilePathNew1);
                    Cache.Ch1Image = Cache.BitmapImageDrawableCh11.BitmapImage;
                    //calibrationAlgorithmService.GetPictureGray(BitmapImageDrawable.BitmapImage.ToHImage(), 255, out var hv_Histo);
                    //ImageGrayNewCh1 = hv_Histo.TupleSum();
                    using var hImage = Cache.BitmapImageDrawableCh11.BitmapImage.ToHImage();
                    HOperatorSet.Intensity(hImage, hImage, out var meanGrayNew1, out _);
                    MeanGrayNew1 = meanGrayNew1;
                }
                else if (SelectedTabIndex == 1)
                {
                    FourierViewModel.FF_Move_CH12(FFCH.Ch2, ch12List);
                    //await Task.Delay(2000).ConfigureAwait(false);
                    ret = FourierViewModel.GetFFReviewImgForTrigger(1, Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Level, SxPos, 100);

                    using var bitmap = BytesToBitmapImage(ret);

                    var croppedImage = GetPictureRegion(bitmap, (int)PupilCameraAlignmentValue.RectCh2Position.X, (int)PupilCameraAlignmentValue.RectCh2Position.Y, PupilCameraAlignmentValue.Ch2ImageWidth, PupilCameraAlignmentValue.Ch2ImageHeight);
                    Cache.BitmapImageDrawableCh21.BitmapImage = croppedImage;
                    Cache.Item.OriginImageFilePathNew2 = Path.Combine(ImageFileDirectory, "CH2", Ch12Percentage + "__" + $"{Guid.NewGuid():N}.jpg");
                    Cache.BitmapImageDrawableCh21.BitmapImage.Save(Cache.Item.OriginImageFilePathNew2);
                    Cache.Ch2Image = Cache.BitmapImageDrawableCh21.BitmapImage;
                    //calibrationAlgorithmService.GetPictureGray(BitmapImageDrawable.BitmapImage.ToHImage(), 255, out var hv_Histo);
                    //ImageGrayNewCh2 = hv_Histo.TupleSum();
                    using var hImage = Cache.BitmapImageDrawableCh21.BitmapImage.ToHImage();
                    HOperatorSet.Intensity(hImage, hImage, out var meanGrayNew2, out _);
                    MeanGrayNew2 = meanGrayNew2;
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                //ImageFilePath = filePath;
                //BitmapImageDrawable.BitmapImage = bitmap;
                RebuildRectRoiDrawableList(); // ✅ 统一入口
            });
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task GetImageFileResultCh12Async()
    {
        await Task.Run(() =>
        {
            if (SelectedTabIndex == 0)
            {
                CurrentImageAxis = [];
                CurrentImageRectListCh1 = [];
                for (int j = 0; j < Cache.RectROIDrawableListCh11.Count; j++)
                {
                    var rectRoi = Cache.RectROIDrawableListCh11[j];
                    var rectOne = Cache.BitmapImageDrawableCh11.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                    CurrentImageAxis = [.. CurrentImageAxis, rectOne];
                }

                for (int j = 0; j < CurrentImageAxis.Count; j++)
                {
                    var rect = CurrentImageAxis[j];
                    CurrentImageRectListCh1 = [.. CurrentImageRectListCh1, rect];
                }

                Cache.Item.CgFFBoxMoveDownPercentListCh1.Clear();
                for (int j = 0; j < CurrentImageRectListCh1.Count; j++)
                {
                    double relatioin;
                    if ((j + 1) >= PupilSideChannelFlexibleApertureValue.CgFFBoxBeginNumber2Ch1 && (j + 1) <= PupilSideChannelFlexibleApertureValue.CgFFBoxEndNumber2Ch1)
                        relatioin = PupilSideChannelFlexibleApertureValue.CgFFBoxHeightRelationPercentListCh1[j + 1 - PupilSideChannelFlexibleApertureValue.CgFFBoxBeginNumber2Ch1];
                    else
                        relatioin = PupilSideChannelFlexibleApertureValue.CgFFBoxHeightRelationPercentListCh1.Average();

                    Cache.Item.CgFFBoxMoveDownPercentListCh1.Add(PupilSideChannelFlexibleApertureValue.CgFFBoxAllRodsBeginPercentCh1 + Math.Round((CurrentImageRectListCh1[j].Height - PupilSideChannelFlexibleApertureValue.CurrentImageRectListFirstCh1[j].Height) / relatioin, 2));
                }

                for (int j = 0; j < Cache.Item.CgFFBoxMoveDownPercentListCh1.Count; j++)
                {
                    if (Cache.Item.CgFFBoxMoveDownPercentListCh1[j] <= 0)
                        Cache.Item.CgFFBoxMoveDownPercentListCh1[j] = 0;
                }
            }

            if (SelectedTabIndex == 1)
            {
                CurrentImageAxis = [];
                CurrentImageRectListCh2 = [];
                for (int j = 0; j < Cache.RectROIDrawableListCh11.Count; j++)
                {
                    var rectRoi = Cache.RectROIDrawableListCh11[j];
                    var rectOne = Cache.BitmapImageDrawableCh21.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                    CurrentImageAxis = [.. CurrentImageAxis, rectOne];
                }

                for (int j = 0; j < CurrentImageAxis.Count; j++)
                {
                    var rect = CurrentImageAxis[j];
                    CurrentImageRectListCh2 = [.. CurrentImageRectListCh2, rect];
                }

                Cache.Item.CgFFBoxMoveDownPercentListCh2.Clear();
                for (int j = 0; j < CurrentImageRectListCh2.Count; j++)
                {
                    double relatioin;
                    if ((j + 1) >= PupilSideChannelFlexibleApertureValue.CgFFBoxBeginNumber2Ch2 && (j + 1) <= PupilSideChannelFlexibleApertureValue.CgFFBoxEndNumber2Ch2)
                        relatioin = PupilSideChannelFlexibleApertureValue.CgFFBoxHeightRelationPercentListCh2[j + 1 - PupilSideChannelFlexibleApertureValue.CgFFBoxBeginNumber2Ch2];
                    else
                        relatioin = PupilSideChannelFlexibleApertureValue.CgFFBoxHeightRelationPercentListCh2.Average();

                    Cache.Item.CgFFBoxMoveDownPercentListCh2.Add(PupilSideChannelFlexibleApertureValue.CgFFBoxAllRodsBeginPercentCh2 + Math.Round((CurrentImageRectListCh2[j].Height - PupilSideChannelFlexibleApertureValue.CurrentImageRectListFirstCh2[j].Height) / relatioin, 2));
                }

                for (int j = 0; j < Cache.Item.CgFFBoxMoveDownPercentListCh2.Count; j++)
                {
                    if (Cache.Item.CgFFBoxMoveDownPercentListCh2[j] <= 0)
                        Cache.Item.CgFFBoxMoveDownPercentListCh2[j] = 0;
                }
            }

            int rodNum;
            var ch12List = new List<(int rodnumber, double rodpos)>();
            var result = FourierViewModel.GetFourierConfig();
            {
                rodNum = result.RodNum;
            }

            {
                if (SelectedTabIndex == 0)
                {
                    for (int j = 1; j <= rodNum; j++)
                    {
                        ch12List.Add((j, Cache.Item.CgFFBoxMoveDownPercentListCh1[j - 1] / 100.0));
                    }

                    FourierViewModel.FF_Move_CH12(FFCH.Ch1, ch12List);
                    //await Task.Delay(2000).ConfigureAwait(false);
                    var ret = FourierViewModel.GetFFReviewImgForTrigger(0, Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Level, SxPos, 100);
                    using var bitmap = BytesToBitmapImage(ret);

                    var croppedImage = GetPictureRegion(bitmap, (int)PupilCameraAlignmentValue.RectCh1Position.X, (int)PupilCameraAlignmentValue.RectCh1Position.Y, PupilCameraAlignmentValue.Ch1ImageWidth, PupilCameraAlignmentValue.Ch1ImageHeight);
                    Cache.BitmapImageDrawableCh11.BitmapImage = croppedImage;
                    Cache.Item.OriginImageFilePathNew1 = Path.Combine(ImageFileDirectory, "CH1", Ch12Percentage + "__" + $"{Guid.NewGuid():N}.jpg");
                    Cache.BitmapImageDrawableCh11.BitmapImage.Save(Cache.Item.OriginImageFilePathNew1);
                    Cache.Ch1Image = Cache.BitmapImageDrawableCh11.BitmapImage;
                    using var hImage = Cache.Ch1Image.ToHImage();
                    HOperatorSet.Intensity(hImage, hImage, out var meanGrayNew1, out _);
                    MeanGrayNew1 = meanGrayNew1;
                }
                else if (SelectedTabIndex == 1)
                {
                    for (int j = 1; j <= rodNum; j++)
                    {
                        ch12List.Add((j, Cache.Item.CgFFBoxMoveDownPercentListCh2[j - 1] / 100.0));
                    }

                    FourierViewModel.FF_Move_CH12(FFCH.Ch2, ch12List);
                    //await Task.Delay(2000).ConfigureAwait(false);
                    var ret = FourierViewModel.GetFFReviewImgForTrigger(1, Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Level, SxPos, 100);
                    using var bitmap = BytesToBitmapImage(ret);

                    var croppedImage = GetPictureRegion(bitmap, (int)PupilCameraAlignmentValue.RectCh2Position.X, (int)PupilCameraAlignmentValue.RectCh2Position.Y, PupilCameraAlignmentValue.Ch2ImageWidth, PupilCameraAlignmentValue.Ch2ImageHeight);
                    Cache.BitmapImageDrawableCh21.BitmapImage = croppedImage;
                    Cache.Item.OriginImageFilePathNew2 = Path.Combine(ImageFileDirectory, "CH2", Ch12Percentage + "__" + $"{Guid.NewGuid():N}.jpg");
                    Cache.BitmapImageDrawableCh21.BitmapImage.Save(Cache.Item.OriginImageFilePathNew2);
                    Cache.Ch2Image = Cache.BitmapImageDrawableCh21.BitmapImage;
                    using var hImage = Cache.Ch2Image.ToHImage();
                    HOperatorSet.Intensity(hImage, hImage, out var meanGrayNew2, out _);
                    MeanGrayNew2 = meanGrayNew2;
                }
            }
            DialogWindowProvider.ShowDialog("Set Data Success!");
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GetLightShowAsync(CancellationToken cancellationToken)
    {
        int rodNum;
        var ch12List = new List<(int rodnumber, double rodpos)>();
        var result = FourierViewModel.GetFourierConfig();
        {
            rodNum = result.RodNum;
        }

        for (int j = 1; j <= rodNum; j++)
        {
            ch12List.Add((j, 0));
        }

        FourierViewModel.FF_Move_CH12(FFCH.Ch1, ch12List);
        FourierViewModel.FF_Move_CH12(FFCH.Ch2, ch12List);

        StageViewModel.SetAbsoluteStageTheta(0d);
        StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(Cache.Item.ShinyWaferPosition);
        AfViewModel.ToggleDarkFieldEnable(true);

        Point darkFieldPosition = StageViewModel.GetDarkFieldStagePosition();
        string ch1Path;
        string ch2Path;
        {
            using var darkFieldImage1 = await CIBViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Dark,
                darkFieldPosition,
                ImageWidthPixel,
                Cache.Item.CIBInformation1,
                (false, CalChipSiteModelEnum.ShinyWaferModel),
                (false, Cache.Item.OpticsConfiguration),
                (false, Cache.Item.CIBConfiguration),
                (false, Cache.Item.LaserLightInformation),
                false,
                cancellationToken);
            var path1 = Path.Combine(ImageFileDirectory, "CH1", "__" + "LightShow" + "__" + $"{Guid.NewGuid():N}.jpg");

            using var hImage = darkFieldImage1.Image.ToHImage();
            ReviewImageGrayCh1[0] = hImage.GetIntensity().Average;
            darkFieldImage1.Image.Save(path1);
            ch1Path = path1;
        }
        {
            using var darkFieldImage1 = await CIBViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Dark,
                darkFieldPosition,
                ImageWidthPixel,
                Cache.Item.CIBInformation2,
                (false, CalChipSiteModelEnum.ShinyWaferModel),
                (false, Cache.Item.OpticsConfiguration),
                (false, Cache.Item.CIBConfiguration),
                (false, Cache.Item.LaserLightInformation),
                false,
                cancellationToken);
            var path2 = Path.Combine(ImageFileDirectory, "CH2", "__" + "LightShow" + "__" + $"{Guid.NewGuid():N}.jpg");

            using var hImage = darkFieldImage1.Image.ToHImage();
            ReviewImageGrayCh2[0] = hImage.GetIntensity().Average;
            darkFieldImage1.Image.Save(path2);
            ch2Path = path2;
        }
        ReviewImageShowPath = new[]
        {
            ch1Path, // CH1
            ch2Path // CH2
        };
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GetLightHideAsync(CancellationToken cancellationToken)
    {
        int rodNum;
        var ch12List = new List<(int rodnumber, double rodpos)>();
        var result = FourierViewModel.GetFourierConfig();
        {
            rodNum = result.RodNum;
        }

        {
            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, Cache.Item.CgFFBoxMoveDownPercentListCh1[j - 1] / 100.0));
            }

            FourierViewModel.FF_Move_CH12(FFCH.Ch1, ch12List);

            ch12List.Clear();
            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, Cache.Item.CgFFBoxMoveDownPercentListCh2[j - 1] / 100.0));
            }

            FourierViewModel.FF_Move_CH12(FFCH.Ch2, ch12List);
        }

        StageViewModel.SetAbsoluteStageTheta(0d);
        StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(Cache.Item.ShinyWaferPosition);
        AfViewModel.ToggleDarkFieldEnable(true);

        Point darkFieldPosition = StageViewModel.GetDarkFieldStagePosition();
        string ch1Path;
        string ch2Path;
        {
            using var darkFieldImage1 = await CIBViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Dark,
                darkFieldPosition,
                ImageWidthPixel,
                Cache.Item.CIBInformation1,
                (false, CalChipSiteModelEnum.ShinyWaferModel),
                (false, Cache.Item.OpticsConfiguration),
                (false, Cache.Item.CIBConfiguration),
                (false, Cache.Item.LaserLightInformation),
                false,
                cancellationToken);
            var path1 = Path.Combine(ImageFileDirectory, "CH1", "__" + "LightHide" + "__" + $"{Guid.NewGuid():N}.jpg");

            using var hImage = darkFieldImage1.Image.ToHImage();
            ReviewImageGrayCh1[1] = hImage.GetIntensity().Average;
            darkFieldImage1.Image.Save(path1);
            ch1Path = path1;
        }

        {
            using var darkFieldImage1 = await CIBViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Dark,
                darkFieldPosition,
                ImageWidthPixel,
                Cache.Item.CIBInformation2,
                (false, CalChipSiteModelEnum.ShinyWaferModel),
                (false, Cache.Item.OpticsConfiguration),
                (false, Cache.Item.CIBConfiguration),
                (false, Cache.Item.LaserLightInformation),
                false,
                cancellationToken);
            var path2 = Path.Combine(ImageFileDirectory, "CH2", "__" + "LightHide" + "__" + $"{Guid.NewGuid():N}.jpg");

            using var hImage = darkFieldImage1.Image.ToHImage();
            ReviewImageGrayCh2[1] = hImage.GetIntensity().Average;
            darkFieldImage1.Image.Save(path2);
            ch2Path = path2;
        }
        ReviewImageHidePath = new[]
        {
            ch1Path, // CH1
            ch2Path // CH2
        };
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GetImageCompareAsync(CancellationToken cancellationToken)
    {
        ReviewImageCompareCh1 = ReviewImageGrayCh1[0] / ReviewImageGrayCh1[1];
        ReviewImageCompareCh2 = ReviewImageGrayCh2[0] / ReviewImageGrayCh2[1];

        await InvokeVerifyAsync(() =>
        {
            Logger.LogHtmlInformation("ReviewImageCompare:Ch1", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                HtmlTab = new HtmlTab(new
                {
                    LightShowImageCh1 = new HtmlImage(ReviewImageShowPath[0]),
                    LightHideImageCh1 = new HtmlImage(ReviewImageHidePath[0])
                })
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlInformation("ReviewImageCompare:Ch2", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                HtmlTab = new HtmlTab(new
                {
                    LightShowImageCh2 = new HtmlImage(ReviewImageShowPath[1]),
                    LightHideImageCh2 = new HtmlImage(ReviewImageHidePath[1])
                })
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlInformation("VerifyResult", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                ReviewImageGrayCh1Before = ReviewImageGrayCh1[0],
                ReviewImageGrayCh1After = ReviewImageGrayCh1[1],
                ReviewImageGrayCh2Before = ReviewImageGrayCh2[0],
                ReviewImageGrayCh2After = ReviewImageGrayCh2[1],
                ReviewImageCompareCh1,
                ReviewImageCompareCh2
            }), HtmlLogUniqueId.LoggingHtml());

            if (ResultPupilSideChannelSpecularBlockerDTOList.Count <= 0)
            {
                DialogWindowProvider.TryShowDialog("Please find Offset!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
            }
            else
            {
                foreach (var (index, pupilSideChannelSpecularBlockerDTO) in ResultPupilSideChannelSpecularBlockerDTOList.Select((dto, i) => (i, dto)))
                {
                    if (pupilSideChannelSpecularBlockerDTO.ProductivityInformation == Cache.ProductivityInformation)
                    {
                        pupilSideChannelSpecularBlockerDTO.IsVerified = true;

                        if (Save(pupilSideChannelSpecularBlockerDTO, cancellationToken, index == ResultPupilSideChannelSpecularBlockerDTOList.Count - 1)) break;

                        pupilSideChannelSpecularBlockerDTO.IsVerified = false;
                        Logger.LogError("{@Name} Error: Save Failed!", Name);
                        return false;
                    }
                }
            }

            DialogWindowProvider.ShowDialog("Verify Success!");
            return true;
        }).ConfigureAwait(false);
    }

    private BitmapImage GetPictureRegion(BitmapImage originImage, int startX, int startY, int width, int height)
    {
        // 3. 【关键】使用 CropPart 进行真实裁剪 // 参数: 原图, 起始列(Column), 起始行(Row), 宽度, 高度     
        using var hImage = originImage.ToHImage();
        using var cropHImage = hImage.CropPart(startY, startX, width, height);
        return cropHImage.ToBitmapImage();
    }

    private bool Save(PupilSideChannelSpecularBlockerDTO itemDto, CancellationToken cancellationToken, bool isSave = true) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        Calibrations =
        [
            .. Calibrations
                .Where(t => !(t.ProductivityInformation == itemDto.ProductivityInformation)),
            itemDto.Clone()
        ];
        if (!isSave) return;

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(ResultPupilSideChannelSpecularBlockerDTOList.Clear);
    }

    public static BitmapImage BytesToBitmapImage(byte[] bytes)
    {
        if (bytes.Length == 0)
            return null!;
        // Net.Utilities.Graphics.Primitives.Medias.Imaging.BitmapImage
        // 使用接受字节数组的构造函数（反编译源码显示有此构造函数）
        return new BitmapImage(bytes, isCopy: true);
    }
}