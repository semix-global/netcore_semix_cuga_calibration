using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Models;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Fourier;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using Core.Utilities;
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
public sealed partial class PupilSideChannelSpecularBlockerViewModel(
    ICalibrationAlgorithmService calibrationAlgorithmService,
    CalibrationSetting calibrationSetting,
    ICalibrationFourierService calibrationFlourierService,
    ICalibrationLaserService calibrationLaserService) : CalibrationViewModelBase
{
    #region 界面相关

    [ObservableProperty]
    private int _selectedTabIndex = 0;

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

    private double meanGrayOld1, meanGrayOld2, meanGrayNew1, meanGrayNew2;

    // 当 RodNum/ RodWidth/ XStartPixel 改变时自动重建矩形集合
    partial void OnRodNumChanged(int value) => RebuildRectROIDrawableList();
    partial void OnRodWidthChanged(int value) => RebuildRectROIDrawableList();
    partial void OnRodHightChanged(int value) => RebuildRectROIDrawableList();

    [ObservableProperty]
    private int _imageWidth = 2048;

    [ObservableProperty]
    private int _imageHeight = 2048;

    [ObservableProperty]
    private int _imageX = 0;

    [ObservableProperty]
    private int _imageY = 0;

    [ObservableProperty]
    private Point _sxPos = new();

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
    private double _reviewImageCompareCh1 = 0;

    [ObservableProperty]
    private double _reviewImageCompareCh2 = 0;

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Optics Illumination Mode" },
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "Select Shiny Wafer Position" },
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

    [ObservableProperty]
    private PupilSideChannelSpecularBlockerCache _cache = new();

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

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

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
            case 3:
                SelectedTabIndex = 1;
                return true;

            case 4:
                SelectedTabIndex = 0;

                ResultDto.CgFFBoxBeginPositionCh1 = Cache.Item.CgFFBoxBeginPositionCh1;
                ResultDto.CgFFBoxBeginNumberCh1 = Cache.Item.CgFFBoxBeginNumberCh1;
                ResultDto.CgFFBoxEndNumberCh1 = Cache.Item.CgFFBoxEndNumberCh1;
                ResultDto.CgFFBoxMoveDownPercentListCh1 = Cache.Item.CgFFBoxMoveDownPercentListCh1;

                ResultDto.CgFFBoxBeginPositionCh2 = Cache.Item.CgFFBoxBeginPositionCh2;
                ResultDto.CgFFBoxBeginNumberCh2 = Cache.Item.CgFFBoxBeginNumberCh2;
                ResultDto.CgFFBoxEndNumberCh2 = Cache.Item.CgFFBoxEndNumberCh2;
                ResultDto.CgFFBoxMoveDownPercentListCh2 = Cache.Item.CgFFBoxMoveDownPercentListCh2;

                SynchronizationContextProvider.Send(() => ResultPupilSideChannelSpecularBlockerDTOList.Add(ResultDto));

                if (ResultPupilSideChannelSpecularBlockerDTOList.Count <= 0)
                {
                    DialogWindowProvider.TryShowDialog("Please find Offset!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    Calibrations = [.. Calibrations.ToList().Where(t => (t.ProductivityInformation == Cache.ProductivityInformation) == false)];
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
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                ClearCalibrationTemp();

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

    [RelayCommand]
    private Task Step0Async()
    {
        calibrationFlourierService.SetFFHome(FFCH.Ch1);
        calibrationFlourierService.SetFFHome(FFCH.Ch2);
        calibrationFlourierService.SetFFHome(FFCH.Ch3_X);
        calibrationFlourierService.SetFFHome(FFCH.Ch3_Y);

        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                OpticsIlluminationMode = Cache.OpticsIlluminationModeEnum
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                ProductivityInformation = Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
    {
        Cache.Item.ShinyWaferPosition = StageViewModel.GetBrightFieldStagePosition();
        AfViewModel.ToggleDarkFieldEnable(true);
        SxPos = new Point(Cache.Item.ShinyWaferPosition.X, Cache.Item.ShinyWaferPosition.Y);

        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                ShinyWaferPosition = Cache.Item.ShinyWaferPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3Async(CancellationToken cancellationToken)
    {
        if (Cache.Ch1Image != null)
        {
            Cache.Item.ImageGrayCompareCh1 = $"Ch1:Image Gray Compare(old/new) value is: {meanGrayOld1 / meanGrayNew1:F4}";
            return InvokeCalibrateAsync(() =>
            {
                Logger.LogHtmlInformation("ResultImage", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    ImageGrayCompareCh1 = $"Ch1:Image Gray Compare(old/new) value is: {meanGrayOld1 / meanGrayNew1:F4}",
                    InitialImageFilePath1 = Cache.Item.OriginImageFilePathOld1,
                    ProcessImageFilePath1 = Cache.Item.OriginImageFilePathNew1,
                    ImageGrayOldCh1 = meanGrayOld1,
                    ImageGrayNewCh1 = meanGrayNew1,
                    HtmlTabCh1 = new HtmlTab(new
                    {
                        InitialImageCh1 = new HtmlImage(Cache.Item.OriginImageFilePathOld1),
                        ProcessImageCh1 = new HtmlImage(Cache.Item.OriginImageFilePathNew1, htmlImageOverlays: CurrentImageRectListCh1.Select(rect => new HtmlImageRectangleOverlay(rect)).ToList())
                    })
                }), HtmlLogUniqueId.LoggingHtml());
                return true;
            });
        }
        else
        {
            DialogWindowProvider.ShowDialog("Make sure you have open image of CH1、CH2、CH3 and save all channel images first，then you can goto next step!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return Task.FromResult(false);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step4Async(CancellationToken cancellationToken)
    {
        if (Cache.Ch2Image != null)
        {
            Cache.Item.ImageGrayCompareCh2 = $"Ch2:Image Gray Compare(old/new) value is: {meanGrayOld2 / meanGrayNew2:F4}";
            return InvokeCalibrateAsync(() =>
            {
                Logger.LogHtmlInformation("ResultImage", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    ImageGrayCompareCh2 = $"Ch2:Image Gray Compare(old/new) value is: {meanGrayOld2 / meanGrayNew2:F4}",
                    InitialImageFilePath2 = Cache.Item.OriginImageFilePathOld2,
                    ProcessImageFilePath2 = Cache.Item.OriginImageFilePathNew2,
                    ImageGrayOldCh2 = meanGrayOld2,
                    ImageGrayNewCh2 = meanGrayNew2,
                    HtmlTabCh2 = new HtmlTab(new
                    {
                        InitialImageCh2 = new HtmlImage(Cache.Item.OriginImageFilePathOld2),
                        ProcessImageCh2 = new HtmlImage(Cache.Item.OriginImageFilePathNew2, htmlImageOverlays: CurrentImageRectListCh2.Select(rect => new HtmlImageRectangleOverlay(rect)).ToList())
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("CalibrationResult", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    CgFFBoxBeginPositionCh1 = Cache.Item.CgFFBoxBeginPositionCh1,
                    CgFFBoxBeginNumberCh1 = Cache.Item.CgFFBoxBeginNumberCh1,
                    CgFFBoxEndNumberCh1 = Cache.Item.CgFFBoxEndNumberCh1,
                    CgFFBoxMoveDownPercentListCh1 = Cache.Item.CgFFBoxMoveDownPercentListCh1,

                    CgFFBoxBeginPositionCh2 = Cache.Item.CgFFBoxBeginPositionCh2,
                    CgFFBoxBeginNumberCh2 = Cache.Item.CgFFBoxBeginNumberCh2,
                    CgFFBoxEndNumberCh2 = Cache.Item.CgFFBoxEndNumberCh2,
                    CgFFBoxMoveDownPercentListCh2 = Cache.Item.CgFFBoxMoveDownPercentListCh2
                }), HtmlLogUniqueId.LoggingHtml());
                return true;
            });
        }
        else
        {
            DialogWindowProvider.ShowDialog("Make sure you have open image of CH1、CH2、CH3 and save all channel images first，then you can goto next step!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return Task.FromResult(false);
        }
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
        if (!IsSyncingFromDrag)
        {
            RebuildRectROIDrawableList();
        }
    }

    // 在 RebuildRectROIDrawableList() 创建新列表后，订阅第一个矩形
    private void RebuildRectROIDrawableList()
    {
        {
            if (Cache.BitmapImageDrawableCh11?.BitmapImage is null)
            {
                // 取消旧订阅（可选）
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
                    var imageRect = new Rect(new Point(currentImageRectListFirstCh1[i].X, currentImageRectListFirstCh1[i].Y), new Size(currentImageRectListFirstCh1[i].Width, currentImageRectListFirstCh1[i].Height));
                    var cartesianRect = Cache.BitmapImageDrawableCh11.ImageCoordinateToCartesianCoordinate(imageRect);
                    newList.Add(new RectROIDrawable { Rect = cartesianRect, Label = (i + 1).ToString() });
                }

                /*var newList = new ObservableCollection<RectROIDrawable>();
                for (int i = 0; i < RodNum; i++)
                {
                    var imageRect = new Rect(new Point(XStartPixel + i * RodWidth, 0), new Size(RodWidth, RodHight));
                    var cartesianRect = Cache.BitmapImageDrawableCh11.ImageCoordinateToCartesianCoordinate(imageRect);
                    newList.Add(new RectROIDrawable { Rect = cartesianRect });
                }*/

                // 取消旧订阅
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
                    var imageRect = new Rect(new Point(currentImageRectListFirstCh2[i].X, currentImageRectListFirstCh2[i].Y), new Size(currentImageRectListFirstCh2[i].Width, currentImageRectListFirstCh2[i].Height));
                    var cartesianRect = Cache.BitmapImageDrawableCh21.ImageCoordinateToCartesianCoordinate(imageRect);
                    newList.Add(new RectROIDrawable { Rect = cartesianRect, Label = (i + 1).ToString() });
                }

                /*var newList = new ObservableCollection<RectROIDrawable>();
                for (int i = 0; i < RodNum; i++)
                {
                    var imageRect = new Rect(new Point(XStartPixel + i * RodWidth, 0), new Size(RodWidth, RodHight));
                    var cartesianRect = Cache.BitmapImageDrawableCh11.ImageCoordinateToCartesianCoordinate(imageRect);
                    newList.Add(new RectROIDrawable { Rect = cartesianRect });
                }*/

                // 取消旧订阅
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
    private async Task OpenImageFileCH12Async()
    {
        int rodNum = 0;
        var ch12List = new List<(int rodnumber, double rodpos)>();
        var config = calibrationFlourierService.GetFourierConfig();
        if (config.IsSuccess == false)
        {
            throw new CugaException(config.ErrorMsg);
        }
        else
        {
            var result = config.Anything;
            rodNum = result.RodNum;
        }

        for (int j = 1; j <= rodNum; j++)
        {
            ch12List.Add((j, 0));
        }

        var ret = calibrationFlourierService.GetFFReviewImgForTrigger(0, Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Level, SxPos, 100);
        if (SelectedTabIndex == 0)
        {
            calibrationFlourierService.FF_Move_CH12(FFCH.Ch1, ch12List);
            ret = calibrationFlourierService.GetFFReviewImgForTrigger(0, Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Level, SxPos, 100);
        }
        else if (SelectedTabIndex == 1)
        {
            calibrationFlourierService.FF_Move_CH12(FFCH.Ch2, ch12List);
            ret = calibrationFlourierService.GetFFReviewImgForTrigger(1, Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Level, SxPos, 100);
        }

        if (ret.IsSuccess == false)
        {
            throw new CugaException(ret.ErrorMsg);
        }
        else
        {
            var originPicture0 = ret.Anything;
            if (originPicture0 == null)
            {
                // 处理错误或返回
                return;
            }

            using var bitmap = BytesToBitmapImage(originPicture0);
            if (bitmap == null)
                return;

            if (SelectedTabIndex == 0)
            {
                var croppedImage = GetPictureRegion(bitmap, (int)PupilCameraAlignmentValue.RectCh1Position.X, (int)PupilCameraAlignmentValue.RectCh1Position.Y, PupilCameraAlignmentValue.Ch1ImageWidth, PupilCameraAlignmentValue.Ch1ImageHeight);
                Cache.BitmapImageDrawableCh10.BitmapImage = croppedImage;
                Cache.Item.OriginImageFilePathOld1 = Path.Combine(ImageFileDirectory, "CH1", "Initial" + "__" + $"{Guid.NewGuid():N}.jpg");
                Cache.BitmapImageDrawableCh10.BitmapImage.SaveImage(Cache.Item.OriginImageFilePathOld1);
                //calibrationAlgorithmService.GetPictureGray(BitmapImageDrawable.BitmapImage.ToHImage(), 255, out var hv_Histo);
                //ImageGrayOldCh1 = hv_Histo.TupleSum();    

                meanGrayOld1 = calibrationAlgorithmService.GetImageMeanGray(Cache.BitmapImageDrawableCh10.BitmapImage,
                    new Rect(Point.Origin, Cache.BitmapImageDrawableCh10.BitmapImage.GetSize()));
            }
            else if (SelectedTabIndex == 1)
            {
                var croppedImage = GetPictureRegion(bitmap, (int)PupilCameraAlignmentValue.RectCh2Position.X, (int)PupilCameraAlignmentValue.RectCh2Position.Y, PupilCameraAlignmentValue.Ch2ImageWidth, PupilCameraAlignmentValue.Ch2ImageHeight);
                Cache.BitmapImageDrawableCh20.BitmapImage = croppedImage;
                Cache.Item.OriginImageFilePathOld2 = Path.Combine(ImageFileDirectory, "CH2", "Initial" + "__" + $"{Guid.NewGuid():N}.jpg");
                Cache.BitmapImageDrawableCh20.BitmapImage.SaveImage(Cache.Item.OriginImageFilePathOld2);
                //calibrationAlgorithmService.GetPictureGray(BitmapImageDrawable.BitmapImage.ToHImage(), 255, out var hv_Histo);
                //ImageGrayOldCh2 = hv_Histo.TupleSum();
                meanGrayOld2 = calibrationAlgorithmService.GetImageMeanGray(Cache.BitmapImageDrawableCh20.BitmapImage,
                    new Rect(Point.Origin, Cache.BitmapImageDrawableCh20.BitmapImage.GetSize()));
            }
        }
    }

    [RelayCommand]
    private async Task SaveImageFileCH12Async()
    {
        int rodNum = 0;
        var ch12List = new List<(int rodnumber, double rodpos)>();
        var config = calibrationFlourierService.GetFourierConfig();
        if (config.IsSuccess == false)
        {
            throw new CugaException(config.ErrorMsg);
        }
        else
        {
            var result = config.Anything;
            rodNum = result.RodNum;
        }

        for (int j = 1; j <= rodNum; j++)
        {
            ch12List.Add((j, 0));
            //ch12List.Add((j, SetAllRods[j - 1].Percent));
        }

        var ret = calibrationFlourierService.GetFFReviewImgForTrigger(0, Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Level, SxPos, 100);
        if (ret.IsSuccess == false)
        {
            throw new CugaException(ret.ErrorMsg);
        }
        else
        {
            if (SelectedTabIndex == 0)
            {
                calibrationFlourierService.FF_Move_CH12(FFCH.Ch1, ch12List);
                //await Task.Delay(2000).ConfigureAwait(false);
                ret = calibrationFlourierService.GetFFReviewImgForTrigger(0, Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Level, SxPos, 100);
                var originPicture0 = ret.Anything;
                if (originPicture0 == null)
                {
                    // 处理错误或返回
                    return;
                }

                using var bitmap = BytesToBitmapImage(originPicture0);
                if (bitmap == null)
                    return;

                var croppedImage = GetPictureRegion(bitmap, (int)PupilCameraAlignmentValue.RectCh1Position.X, (int)PupilCameraAlignmentValue.RectCh1Position.Y, PupilCameraAlignmentValue.Ch1ImageWidth, PupilCameraAlignmentValue.Ch1ImageHeight);
                Cache.BitmapImageDrawableCh11.BitmapImage = croppedImage;
                Cache.Item.OriginImageFilePathNew1 = Path.Combine(ImageFileDirectory, "CH1", Ch12Percentage + "__" + Cache.Item.CgFFBoxBeginNumberCh1 + "__" + Cache.Item.CgFFBoxEndNumberCh1 + "__" + $"{Guid.NewGuid():N}.jpg");
                Cache.BitmapImageDrawableCh11.BitmapImage.SaveImage(Cache.Item.OriginImageFilePathNew1);
                Cache.Ch1Image = Cache.BitmapImageDrawableCh11.BitmapImage;
                //calibrationAlgorithmService.GetPictureGray(BitmapImageDrawable.BitmapImage.ToHImage(), 255, out var hv_Histo);
                //ImageGrayNewCh1 = hv_Histo.TupleSum();
                meanGrayNew1 = calibrationAlgorithmService.GetImageMeanGray(Cache.BitmapImageDrawableCh11.BitmapImage,
                    new Rect(Point.Origin, Cache.BitmapImageDrawableCh11.BitmapImage.GetSize()));
            }
            else if (SelectedTabIndex == 1)
            {
                calibrationFlourierService.FF_Move_CH12(FFCH.Ch2, ch12List);
                //await Task.Delay(2000).ConfigureAwait(false);
                ret = calibrationFlourierService.GetFFReviewImgForTrigger(1, Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Level, SxPos, 100);
                var originPicture0 = ret.Anything;
                if (originPicture0 == null)
                {
                    // 处理错误或返回
                    return;
                }

                using var bitmap = BytesToBitmapImage(originPicture0);
                if (bitmap == null)
                    return;

                var croppedImage = GetPictureRegion(bitmap, (int)PupilCameraAlignmentValue.RectCh2Position.X, (int)PupilCameraAlignmentValue.RectCh2Position.Y, PupilCameraAlignmentValue.Ch2ImageWidth, PupilCameraAlignmentValue.Ch2ImageHeight);
                Cache.BitmapImageDrawableCh21.BitmapImage = croppedImage;
                Cache.Item.OriginImageFilePathNew2 = Path.Combine(ImageFileDirectory, "CH2", Ch12Percentage + "__" + Cache.Item.CgFFBoxBeginNumberCh2 + "__" + Cache.Item.CgFFBoxEndNumberCh2 + "__" + $"{Guid.NewGuid():N}.jpg");
                Cache.BitmapImageDrawableCh21.BitmapImage.SaveImage(Cache.Item.OriginImageFilePathNew2);
                Cache.Ch2Image = Cache.BitmapImageDrawableCh21.BitmapImage;
                //calibrationAlgorithmService.GetPictureGray(BitmapImageDrawable.BitmapImage.ToHImage(), 255, out var hv_Histo);
                //ImageGrayNewCh2 = hv_Histo.TupleSum();
                meanGrayNew2 = calibrationAlgorithmService.GetImageMeanGray(Cache.BitmapImageDrawableCh21.BitmapImage,
                    new Rect(Point.Origin, Cache.BitmapImageDrawableCh21.BitmapImage.GetSize()));
            }
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            //ImageFilePath = filePath;
            //BitmapImageDrawable.BitmapImage = bitmap;
            RebuildRectROIDrawableList(); // ✅ 统一入口
        });
    }

    [RelayCommand]
    private async Task GetImageFileResultCH12Async()
    {
        if (SelectedTabIndex == 0)
        {
            CurrentImageAxis.Clear();
            CurrentImageRectListCh1.Clear();
            for (int j = 0; j < Cache.RectROIDrawableListCh11.Count; j++)
            {
                var rectRoi = Cache.RectROIDrawableListCh11[j];
                var rectOne = Cache.BitmapImageDrawableCh11.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                CurrentImageAxis.Add(rectOne);
            }

            for (int j = 0; j < CurrentImageAxis.Count; j++)
            {
                var rect = CurrentImageAxis[j];
                CurrentImageRectListCh1.Add(rect);
            }

            Cache.Item.CgFFBoxMoveDownPercentListCh1.Clear();
            for (int j = 0; j < CurrentImageRectListCh1.Count; j++)
            {
                Cache.Item.CgFFBoxMoveDownPercentListCh1.Add(PupilSideChannelFlexibleApertureValue.CgFFBoxAllRodsBeginPercentCh1 + Math.Round((CurrentImageRectListCh1[j].Height - PupilSideChannelFlexibleApertureValue.CurrentImageRectListFirstCh1[j].Height) / PupilSideChannelFlexibleApertureValue.CgFFBoxHeightRelationPercentListCh1.Average(), 2));
            }
        }

        if (SelectedTabIndex == 1)
        {
            CurrentImageAxis.Clear();
            CurrentImageRectListCh2.Clear();
            for (int j = 0; j < Cache.RectROIDrawableListCh11.Count; j++)
            {
                var rectRoi = Cache.RectROIDrawableListCh11[j];
                var rectOne = Cache.BitmapImageDrawableCh21.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                CurrentImageAxis.Add(rectOne);
            }

            for (int j = 0; j < CurrentImageAxis.Count; j++)
            {
                var rect = CurrentImageAxis[j];
                CurrentImageRectListCh2.Add(rect);
            }

            Cache.Item.CgFFBoxMoveDownPercentListCh2.Clear();
            for (int j = 0; j < CurrentImageRectListCh2.Count; j++)
            {
                Cache.Item.CgFFBoxMoveDownPercentListCh2.Add(PupilSideChannelFlexibleApertureValue.CgFFBoxAllRodsBeginPercentCh2 + Math.Round((CurrentImageRectListCh2[j].Height - PupilSideChannelFlexibleApertureValue.CurrentImageRectListFirstCh2[j].Height) / PupilSideChannelFlexibleApertureValue.CgFFBoxHeightRelationPercentListCh2.Average(), 2));
            }
        }

        int rodNum = 0;
        var ch12List = new List<(int rodnumber, double rodpos)>();
        var config = calibrationFlourierService.GetFourierConfig();
        if (config.IsSuccess == true)
        {
            var result = config.Anything;
            rodNum = result.RodNum;
        }
        else
            throw new CugaException(config.ErrorMsg);

        {
            if (SelectedTabIndex == 0)
            {
                for (int j = 1; j <= rodNum; j++)
                {
                    ch12List.Add((j, Cache.Item.CgFFBoxMoveDownPercentListCh1[j - 1]));
                }

                calibrationFlourierService.FF_Move_CH12(FFCH.Ch1, ch12List);
                //await Task.Delay(2000).ConfigureAwait(false);
                var ret = calibrationFlourierService.GetFFReviewImgForTrigger(0, Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Level, SxPos, 100);
                var originPicture0 = ret.Anything;
                if (originPicture0 == null)
                    return;
                using var bitmap = BytesToBitmapImage(originPicture0);
                if (bitmap == null)
                    return;

                var croppedImage = GetPictureRegion(bitmap, (int)PupilCameraAlignmentValue.RectCh1Position.X, (int)PupilCameraAlignmentValue.RectCh1Position.Y, PupilCameraAlignmentValue.Ch1ImageWidth, PupilCameraAlignmentValue.Ch1ImageHeight);
                Cache.BitmapImageDrawableCh11.BitmapImage = croppedImage;
                Cache.Item.OriginImageFilePathNew1 = Path.Combine(ImageFileDirectory, "CH1", Ch12Percentage + "__" + Cache.Item.CgFFBoxBeginNumberCh1 + "__" + Cache.Item.CgFFBoxEndNumberCh1 + "__" + $"{Guid.NewGuid():N}.jpg");
                Cache.BitmapImageDrawableCh11.BitmapImage.SaveImage(Cache.Item.OriginImageFilePathNew1);
                Cache.Ch1Image = Cache.BitmapImageDrawableCh11.BitmapImage;
                meanGrayNew1 = calibrationAlgorithmService.GetImageMeanGray(Cache.BitmapImageDrawableCh11.BitmapImage,
                    new Rect(Point.Origin, Cache.BitmapImageDrawableCh11.BitmapImage.GetSize()));
            }
            else if (SelectedTabIndex == 1)
            {
                for (int j = 1; j <= rodNum; j++)
                {
                    ch12List.Add((j, Cache.Item.CgFFBoxMoveDownPercentListCh2[j - 1]));
                }

                calibrationFlourierService.FF_Move_CH12(FFCH.Ch2, ch12List);
                //await Task.Delay(2000).ConfigureAwait(false);
                var ret = calibrationFlourierService.GetFFReviewImgForTrigger(1, Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Level, SxPos, 100);
                var originPicture0 = ret.Anything;
                if (originPicture0 == null)
                    return;
                using var bitmap = BytesToBitmapImage(originPicture0);
                if (bitmap == null)
                    return;

                var croppedImage = GetPictureRegion(bitmap, (int)PupilCameraAlignmentValue.RectCh2Position.X, (int)PupilCameraAlignmentValue.RectCh2Position.Y, PupilCameraAlignmentValue.Ch2ImageWidth, PupilCameraAlignmentValue.Ch2ImageHeight);
                Cache.BitmapImageDrawableCh21.BitmapImage = croppedImage;
                Cache.Item.OriginImageFilePathNew2 = Path.Combine(ImageFileDirectory, "CH2", Ch12Percentage + "__" + Cache.Item.CgFFBoxBeginNumberCh2 + "__" + Cache.Item.CgFFBoxEndNumberCh2 + "__" + $"{Guid.NewGuid():N}.jpg");
                Cache.BitmapImageDrawableCh21.BitmapImage.SaveImage(Cache.Item.OriginImageFilePathNew2);
                Cache.Ch2Image = Cache.BitmapImageDrawableCh21.BitmapImage;
                meanGrayNew2 = calibrationAlgorithmService.GetImageMeanGray(Cache.BitmapImageDrawableCh21.BitmapImage,
                    new Rect(Point.Origin, Cache.BitmapImageDrawableCh21.BitmapImage.GetSize()));
            }
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GetLightShowAsync(CancellationToken cancellationToken)
    {
        int rodNum = 0;
        var ch12List = new List<(int rodnumber, double rodpos)>();
        var config = calibrationFlourierService.GetFourierConfig();
        if (config.IsSuccess == false)
            throw new CugaException(config.ErrorMsg);
        else
        {
            var result = config.Anything;
            rodNum = result.RodNum;
        }

        for (int j = 1; j <= rodNum; j++)
        {
            ch12List.Add((j, 0));
        }

        calibrationFlourierService.FF_Move_CH12(FFCH.Ch1, ch12List);
        calibrationFlourierService.FF_Move_CH12(FFCH.Ch2, ch12List);

        StageViewModel.SetAbsoluteStageTheta(0d);
        StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(Cache.Item.ShinyWaferPosition);
        AfViewModel.ToggleDarkFieldEnable(true);

        Point DarkFieldPosition = StageViewModel.GetDarkFieldStagePosition();
        var Ch1Path = string.Empty;
        var Ch2Path = string.Empty;
        {
            using var darkFieldImage1 = await CIBViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Dark,
                DarkFieldPosition,
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
            darkFieldImage1.Image.SaveImage(path1);
            Ch1Path = path1;
        }
        {
            using var darkFieldImage1 = await CIBViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Dark,
                DarkFieldPosition,
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
            darkFieldImage1.Image.SaveImage(path2);
            Ch2Path = path2;
        }
        ReviewImageShowPath = new[]
        {
            Ch1Path, // CH1
            Ch2Path // CH2
        };
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GetLightHideAsync(CancellationToken cancellationToken)
    {
        int rodNum = 0;
        var ch12List = new List<(int rodnumber, double rodpos)>();
        var config = calibrationFlourierService.GetFourierConfig();
        if (config.IsSuccess == false)
            throw new CugaException(config.ErrorMsg);
        else
        {
            var result = config.Anything;
            rodNum = result.RodNum;
        }

        {
            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, Cache.Item.CgFFBoxMoveDownPercentListCh1[j - 1]));
            }

            calibrationFlourierService.FF_Move_CH12(FFCH.Ch1, ch12List);

            ch12List.Clear();
            for (int j = 1; j <= rodNum; j++)
            {
                ch12List.Add((j, Cache.Item.CgFFBoxMoveDownPercentListCh2[j - 1]));
            }

            calibrationFlourierService.FF_Move_CH12(FFCH.Ch2, ch12List);
        }

        StageViewModel.SetAbsoluteStageTheta(0d);
        StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(Cache.Item.ShinyWaferPosition);
        AfViewModel.ToggleDarkFieldEnable(true);

        Point DarkFieldPosition = StageViewModel.GetDarkFieldStagePosition();
        var Ch1Path = string.Empty;
        var Ch2Path = string.Empty;
        {
            using var darkFieldImage1 = await CIBViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Dark,
                DarkFieldPosition,
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
            darkFieldImage1.Image.SaveImage(path1);
            Ch1Path = path1;
        }

        {
            using var darkFieldImage1 = await CIBViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Dark,
                DarkFieldPosition,
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
            darkFieldImage1.Image.SaveImage(path2);
            Ch2Path = path2;
        }
        ReviewImageHidePath = new[]
        {
            Ch1Path, // CH1
            Ch2Path // CH2
        };
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GetImageCompareAsync(CancellationToken cancellationToken)
    {
        ReviewImageCompareCh1 = ReviewImageGrayCh1[0] / ReviewImageGrayCh1[1];
        ReviewImageCompareCh2 = ReviewImageGrayCh2[0] / ReviewImageGrayCh2[1];

        Logger.LogHtmlInformation("Verify", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            ReviewImageGrayCh1Before = ReviewImageGrayCh1[0],
            ReviewImageGrayCh1After = ReviewImageGrayCh1[1],
            ReviewImageGrayCh2Before = ReviewImageGrayCh2[0],
            ReviewImageGrayCh2After = ReviewImageGrayCh2[1],
            ReviewImageCompareCh1 = ReviewImageCompareCh1,
            ReviewImageCompareCh2 = ReviewImageCompareCh2
        }), HtmlLogUniqueId.LoggingHtml());
        return;
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
                .Where(t => (t.ProductivityInformation == itemDto.ProductivityInformation
                             && t.OpticsIlluminationMode == itemDto.OpticsIlluminationMode) == false),
            itemDto.Clone()
        ];
        if (isSave == false) return;

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(ResultPupilSideChannelSpecularBlockerDTOList.Clear);
    }

    public static BitmapImage BytesToBitmapImage(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return null;
        // Net.Utilities.Graphics.Primitives.Medias.Imaging.BitmapImage
        // 使用接受字节数组的构造函数（反编译源码显示有此构造函数）
        return new BitmapImage(bytes, isCopy: true);
    }
}