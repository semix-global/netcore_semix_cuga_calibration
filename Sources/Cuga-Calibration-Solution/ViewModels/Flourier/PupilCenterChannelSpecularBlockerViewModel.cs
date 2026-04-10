using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Models;
using Core.Models.Models.Common.Fourier;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Fourier;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using Core.Utilities;
using HalconDotNet;
using Local.SQL.Cache.Providers.Extensions;
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
using System.IO;
using System.Windows;
using Point = Net.Utilities.Models.Geometries.Point;
using Rect = Net.Utilities.Models.Geometries.Rect;
using Size = Net.Utilities.Models.Geometries.Size;

namespace CugaCalibration.ViewModels.Flourier;

[IOCAppService(ServiceType = typeof(PupilCenterChannelSpecularBlockerViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class PupilCenterChannelSpecularBlockerViewModel(ICalibrationAlgorithmService calibrationAlgorithmService,
    CalibrationSetting calibrationSetting, ICalibrationFourierService calibrationFlourierService,
    ICalibrationLaserService calibrationLaserService) : CalibrationViewModelBase
{
    #region 界面相关

    public enum ChartShowType
    {
        ID_0_Style,
        ID_1_Style,
        ID_2_Style,
        ID_3_Style
    };

    [ObservableProperty]
    private int _selectedTabIndex = 0;

    [ObservableProperty]
    private int _rodWidth = 250;

    [ObservableProperty]
    private int _rodHight = 50;

    [ObservableProperty]
    private int _xStartPixel = 100;

    [ObservableProperty]
    private int _selectRodWidth = 0;

    [ObservableProperty]
    private int _selectRodHeight = 0;

    [ObservableProperty]
    private float _ch3TurnY = 3.0f;

    [ObservableProperty]
    private double _ch3TurnYMotorRelation = 13.0f;

    [ObservableProperty]
    private double _ch3PushXMotorRelation = 22.0f;

    [ObservableProperty]
    private float _ch3Push = 42.0f;

    private HTuple histoOld = new(), histoNew = new();

    public ChartShowType[] Ch3ShowTypeValues => Enum.GetValues(typeof(ChartShowType)).Cast<ChartShowType>().ToArray();

    [ObservableProperty]
    private ChartShowType _selectedCh3ShowType = (ChartShowType)(-1);

    [ObservableProperty]
    private Point _sxPos = new();

    [ObservableProperty]
    private int _imageWidthPixel = 1000;

    [ObservableProperty]
    private string _reviewImageShowPath = string.Empty;

    [ObservableProperty]
    private string _reviewImageHidePath = string.Empty;

    [ObservableProperty]
    private double[] _reviewImageGrayCh3 = new double[2];

    [ObservableProperty]
    private double _reviewImageCompareCh3 = 0;

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Optics Illumination Mode" },
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "Select Shiny Wafer Position" },
        new() { StepName = "Pupil Center Channel Specular Blocker Calibration" }
    ];

    #region Calibrate  

    [ObservableProperty]
    private ObservableCollection<PupilCenterChannelSpecularBlockerDTO> _resultPupilCenterChannelSpecularBlockerDTOList = [];

    [ObservableProperty]
    private IReadOnlyList<OpticsIlluminationModeAndProductivityInformationStatus> _calibrationStatuses = [];

    #endregion Calibrate

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private PupilCenterChannelSpecularBlockerDTO _resultDto = new();

    [ObservableProperty]
    private MicroscopeCalChipDTO _microscopeCalChip = new();

    [ObservableProperty]
    private PupilCameraAlignmentDTO _pupilCameraAlignmentValue = new();

    [ObservableProperty]
    private PupilCenterChannelFlexibleApertureDTO _pupilCenterChannelFlexibleApertureValue = new();

    [ObservableProperty]
    private PupilCenterChannelSpecularBlockerCache _cache = new();

    [ObservableProperty]
    private PupilCenterChannelSpecularBlockerDTO[] _calibrations = [];

    #endregion 缓存

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false)
            return false;

        MicroscopeCalChip = CalibrationStatusService.GetCalibration<MicroscopeCalChipDTO>();
        PupilCameraAlignmentValue = CalibrationStatusService.GetCalibration<PupilCameraAlignmentDTO>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<PupilCenterChannelSpecularBlockerCache>();

        Calibrations = CacheProvider.GetOrDefaultArray<PupilCenterChannelSpecularBlockerDTO>();

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

        PupilCenterChannelFlexibleApertureValue = CalibrationStatusService.GetCalibration<PupilCenterChannelFlexibleApertureDTO>();

        Ch3TurnYMotorRelation = PupilCenterChannelFlexibleApertureValue.CgFFBoxTurnYMotorRelationCH3.Count > 0 ? PupilCenterChannelFlexibleApertureValue.CgFFBoxTurnYMotorRelationCH3.Average() : 0;
        Ch3PushXMotorRelation = PupilCenterChannelFlexibleApertureValue.CgFFBoxPushXMotorRelationCH3;

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
            case 3:

                ResultDto.Ch3TurnY = Ch3TurnY;
                ResultDto.Ch3Push = Ch3Push;

                SynchronizationContextProvider.Send(() => ResultPupilCenterChannelSpecularBlockerDTOList.Add(ResultDto));

                if (ResultPupilCenterChannelSpecularBlockerDTOList.Count <= 0)
                {
                    DialogWindowProvider.TryShowDialog("Please find Offset!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    Calibrations = [.. Calibrations.ToList().Where(t => (t.ProductivityInformation == Cache.ProductivityInformation) == false)];
                    foreach (var (index, pupilCenterChannelSpecularBlockerDTO) in ResultPupilCenterChannelSpecularBlockerDTOList.Select((dto, i) => (i, dto)))
                    {
                        pupilCenterChannelSpecularBlockerDTO.IsCalibrated = true;
                        if (Save(pupilCenterChannelSpecularBlockerDTO, cancellationToken, index == ResultPupilCenterChannelSpecularBlockerDTOList.Count - 1)) continue;

                        pupilCenterChannelSpecularBlockerDTO.IsCalibrated = false;
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
        return Task.FromResult(true);
    }

    [RelayCommand]
    private Task Step0Async()
    {
        calibrationFlourierService.SetFFHome(FFCH.Ch1);
        calibrationFlourierService.SetFFHome(FFCH.Ch2);
        calibrationFlourierService.SetFFHome(FFCH.Ch3_X);
        calibrationFlourierService.SetFFHome(FFCH.Ch3_Y);

        calibrationFlourierService.SetFFRPOS_CH3(FFCH.Ch3_X, 0);
        calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_X, 0);
        calibrationFlourierService.SetFFPPOS_CH3(FFCH.Ch3_X, 0);
        calibrationFlourierService.SetFFRPOS_CH3(FFCH.Ch3_Y, 0);
        calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_Y, 0);

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
        if (Cache.Ch3Image != null)
        {
            return InvokeCalibrateAsync(() =>
            {
                var chi_square = Cache.Item.ImageGrayOldCh3 / Cache.Item.ImageGrayNewCh3;
                Cache.Item.ImageGrayCompareCh3 = $"Ch3:Image Gray Compare(old/new) value is: {chi_square:F4}";
                Logger.LogHtmlInformation("ResultImage", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    ImageGrayCompareCh3 = $"Ch3:Image Gray Compare(old/new) value is: {chi_square:F4}",
                    InitialImageFilePath3 = Cache.Item.OriginImageFilePathOld,
                    ProcessImageFilePath3 = Cache.Item.OriginImageFilePathNew,
                    ImageGrayOldCh3 = Cache.Item.ImageGrayOldCh3,
                    ImageGrayNewCh3 = Cache.Item.ImageGrayNewCh3,
                    HtmlTab = new HtmlTab(new
                    {
                        InitialImageCh3 = new HtmlImage(Cache.Item.OriginImageFilePathOld),
                        ProcessImageCh3 = new HtmlImage(Cache.Item.OriginImageFilePathNew)
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("CalibrationResult", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Ch3TurnY = Ch3TurnY,
                    Ch3Push = Ch3Push
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

    [RelayCommand]
    private async Task OpenImageFileCH3Async()
    {
        var ret = calibrationFlourierService.GetFFReviewImgForTrigger(2, Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Level, SxPos, 100);
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

            Cache.Item.OriginImageFilePathOld = Path.Combine(ImageFileDirectory, "CH3", "Initial" + "__" + $"{Guid.NewGuid():N}.jpg");
            var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh3Position.X, (int)PupilCameraAlignmentValue.RectCh3Position.Y, PupilCameraAlignmentValue.Ch3ImageWidth, PupilCameraAlignmentValue.Ch3ImageHeight);
            Cache.BitmapImageDrawableCh30.BitmapImage = croppedImage.ToBitmapImage();
            Cache.BitmapImageDrawableCh30.BitmapImage.Save(Cache.Item.OriginImageFilePathOld);
            //calibrationAlgorithmService.GetPictureGray(BitmapImageDrawable.BitmapImage.ToHImage(), 255, out var hv_Histo);
            HOperatorSet.Intensity(Cache.BitmapImageDrawableCh30.BitmapImage.ToHImage(), Cache.BitmapImageDrawableCh30.BitmapImage.ToHImage(), out var meanGrayOld3, out var deviation);
            Cache.Item.ImageGrayOldCh3 = (float)meanGrayOld3.D;
            //histoOld=hv_Histo;
        }
    }

    [RelayCommand]
    private async Task SaveImageFileCH3Async()
    {
        for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
        {
            var rectRoi = Cache.RectROIDrawableList[j];
            var PushRectVertical = Cache.BitmapImageDrawableCh31.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
            SelectRodWidth = (int)PushRectVertical.Width;
            SelectRodHeight = (int)PushRectVertical.Height;
            break;
        }

        if (Cache.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.OI)
        {
            calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_Y, 0);
            calibrationFlourierService.SetFFPPOS_CH3(FFCH.Ch3_X, Ch3Push);
        }
        if (Cache.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.NI)
        {
            calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_Y, Ch3TurnY);
            calibrationFlourierService.SetFFPPOS_CH3(FFCH.Ch3_X, Ch3Push);
        }

        var ret = calibrationFlourierService.GetFFReviewImgForTrigger(2, Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Level, SxPos, 100);
        if (ret.IsSuccess == false)
        {
            throw new CugaException(ret.ErrorMsg);
        }
        else
        {
            var originPicture0 = ret.Anything;
            if (originPicture0 == null)
                return;

            using var bitmap = BytesToBitmapImage(originPicture0);
            if (bitmap == null)
                return;

            Cache.Item.OriginImageFilePathNew = Path.Combine(ImageFileDirectory, "CH3", "_" + Ch3TurnY + "_" + Ch3Push + "_" + $"{Guid.NewGuid():N}.jpg");
            var croppedImage = GetPictureRegion(bitmap.ToHImage(), (int)PupilCameraAlignmentValue.RectCh3Position.X, (int)PupilCameraAlignmentValue.RectCh3Position.Y, PupilCameraAlignmentValue.Ch3ImageWidth, PupilCameraAlignmentValue.Ch3ImageHeight);
            Cache.BitmapImageDrawableCh31.BitmapImage = croppedImage.ToBitmapImage();
            Cache.Ch3Image = Cache.BitmapImageDrawableCh31.BitmapImage;
            //var templateFilePath = $"{originImageFilePath}_Template";
            Cache.BitmapImageDrawableCh31.BitmapImage.Save(Cache.Item.OriginImageFilePathNew);
            //calibrationAlgorithmService.GetPictureGray(BitmapImageDrawable.BitmapImage.ToHImage(), 255, out var hv_Histo);
            HOperatorSet.Intensity(Cache.BitmapImageDrawableCh31.BitmapImage.ToHImage(), Cache.BitmapImageDrawableCh31.BitmapImage.ToHImage(), out var meanGrayNew3, out var deviation);
            Cache.Item.ImageGrayNewCh3 = (float)meanGrayNew3.D;
            //histoNew = hv_Histo;
        }
    }

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
            case ChartShowType.ID_2_Style:
                // 执行 pmt_id 相关的业务逻辑
                HandleId2();
                break;
            case ChartShowType.ID_3_Style:
                // 执行 swathid_pmtid 相关的业务逻辑
                HandleId3();
                break;
            default:
                // 处理未定义的枚举值
                MessageBox.Show($"未处理的 ChartShowType: {chartShowType}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
        }
    }

    private void HandleId0()
    {
        if (Cache.BitmapImageDrawableCh31?.BitmapImage != null)
        {
            // 示例逻辑：创建特定的矩形配置
            var newList = new ObservableCollection<RectROIDrawable>();
            // 根据 swath_all_id 业务逻辑创建矩形
            var imageRect = new Rect(new Point(XStartPixel + 0 * RodWidth, 20), new Size(1900, RodHight + 150));
            var cartesianRect = Cache.BitmapImageDrawableCh31.ImageCoordinateToCartesianCoordinate(imageRect);
            newList.Add(new RectROIDrawable { Rect = cartesianRect });

            // 更新UI线程
            Application.Current.Dispatcher.Invoke(() =>
            {
                Cache.RectROIDrawableList = newList;
            });
        }
    }

    private void HandleId1()
    {
        if (Cache.BitmapImageDrawableCh31?.BitmapImage != null)
        {
            // 示例逻辑：创建特定的矩形配置
            var newList = new ObservableCollection<RectROIDrawable>();
            var imageRect = new Rect(new Point(XStartPixel + 0 * RodWidth, 20), new Size(1900, RodHight + 300));
            var cartesianRect = Cache.BitmapImageDrawableCh31.ImageCoordinateToCartesianCoordinate(imageRect);
            newList.Add(new RectROIDrawable { Rect = cartesianRect });
            // 更新UI线程
            Application.Current.Dispatcher.Invoke(() =>
            {
                Cache.RectROIDrawableList = newList;
            });
        }
    }

    private void HandleId2()
    {
        if (Cache.BitmapImageDrawableCh31?.BitmapImage != null)
        {
            // 示例逻辑：创建特定的矩形配置
            var newList = new ObservableCollection<RectROIDrawable>();
            // 根据 swath_all_id 业务逻辑创建矩形
            var imageRect = new Rect(new Point(XStartPixel + 850, 0), new Size(RodWidth + 150, 990));
            var cartesianRect = Cache.BitmapImageDrawableCh31.ImageCoordinateToCartesianCoordinate(imageRect);
            newList.Add(new RectROIDrawable { Rect = cartesianRect });

            Application.Current.Dispatcher.Invoke(() =>
            {
                Cache.RectROIDrawableList = newList;
            });
        }
    }

    private void HandleId3()
    {
        if (Cache.BitmapImageDrawableCh31?.BitmapImage != null)
        {
            // 示例逻辑：创建特定的矩形配置
            var newList = new ObservableCollection<RectROIDrawable>();
            // 根据 swath_all_id 业务逻辑创建矩形
            var imageRect = new Rect(new Point(XStartPixel + 850, 0), new Size(RodWidth + 250, 990));
            var cartesianRect = Cache.BitmapImageDrawableCh31.ImageCoordinateToCartesianCoordinate(imageRect);
            newList.Add(new RectROIDrawable { Rect = cartesianRect });
            // 更新UI线程
            Application.Current.Dispatcher.Invoke(() =>
            {
                Cache.RectROIDrawableList = newList;
            });
        }
    }


    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GetLightShowAsync(CancellationToken cancellationToken)
    {
        calibrationFlourierService.SetFFHome(FFCH.Ch1);
        calibrationFlourierService.SetFFHome(FFCH.Ch2);
        calibrationFlourierService.SetFFHome(FFCH.Ch3_X);
        calibrationFlourierService.SetFFHome(FFCH.Ch3_Y);

        calibrationFlourierService.SetFFRPOS_CH3(FFCH.Ch3_X, 0);
        calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_X, 0);
        calibrationFlourierService.SetFFPPOS_CH3(FFCH.Ch3_X, 0);
        calibrationFlourierService.SetFFRPOS_CH3(FFCH.Ch3_Y, 0);
        calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_Y, 0);

        StageViewModel.SetAbsoluteStageTheta(0);
        StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(Cache.Item.ShinyWaferPosition);
        AfViewModel.ToggleDarkFieldEnable(true);

        Point DarkFieldPosition = StageViewModel.GetDarkFieldStagePosition();
        {
            {
                using var darkFieldImage1 = await CIBViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Dark,
                DarkFieldPosition,
                ImageWidthPixel,
                Cache.Item.CIBInformation3,
                (false, CalChipSiteModelEnum.ShinyWaferModel),
                (false, Cache.Item.OpticsConfiguration),
                (false, Cache.Item.CIBConfiguration),
                (false, Cache.Item.LaserLightInformation),
                false,
                cancellationToken);
                var path1 = Path.Combine(ImageFileDirectory, "CH3", "__" + "LightShow" + "__" + $"{Guid.NewGuid():N}.jpg");

                ReviewImageGrayCh3[0] = darkFieldImage1.Image.GetIntensity().Average;
                darkFieldImage1.Image.Save(path1);
                ReviewImageShowPath = path1;
            }
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GetLightHideAsync(CancellationToken cancellationToken)
    {
        if (Cache.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.OI)
        {
            calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_Y, 0);
            calibrationFlourierService.SetFFPPOS_CH3(FFCH.Ch3_X, Ch3Push);
        }
        if (Cache.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.NI)
        {
            calibrationFlourierService.SetFFLPOS_CH3(FFCH.Ch3_Y, Ch3TurnY);
            calibrationFlourierService.SetFFPPOS_CH3(FFCH.Ch3_X, Ch3Push);
        }

        StageViewModel.SetAbsoluteStageTheta(0);
        StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(Cache.Item.ShinyWaferPosition);
        AfViewModel.ToggleDarkFieldEnable(true);

        Point DarkFieldPosition = StageViewModel.GetDarkFieldStagePosition();
        {
            {
                using var darkFieldImage1 = await CIBViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Dark,
                DarkFieldPosition,
                ImageWidthPixel,
                Cache.Item.CIBInformation3,
                (false, CalChipSiteModelEnum.ShinyWaferModel),
                (false, Cache.Item.OpticsConfiguration),
                (false, Cache.Item.CIBConfiguration),
                (false, Cache.Item.LaserLightInformation),
                false,
                cancellationToken);
                var path1 = Path.Combine(ImageFileDirectory, "CH3", "__" + "LightHide" + "__" + $"{Guid.NewGuid():N}.jpg");

                ReviewImageGrayCh3[1] = darkFieldImage1.Image.GetIntensity().Average;
                darkFieldImage1.Image.Save(path1);
                ReviewImageHidePath = path1;
            }
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GetImageCompareAsync(CancellationToken cancellationToken)
    {
        ReviewImageCompareCh3 = ReviewImageGrayCh3[0] / ReviewImageGrayCh3[1];

        Logger.LogHtmlInformation("Verify", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            ReviewImageGrayCh3Before = ReviewImageGrayCh3[0],
            ReviewImageGrayCh3After = ReviewImageGrayCh3[1],
            ReviewImageCompareCh3 = ReviewImageCompareCh3
        }), HtmlLogUniqueId.LoggingHtml());
        return;
    }

    private HImage GetPictureRegion(HImage originImage, int startX, int startY, int width, int height)
    {
        // 3. 【关键】使用 CropPart 进行真实裁剪 // 参数: 原图, 起始列(Column), 起始行(Row), 宽度, 高度     
        return originImage.CropPart(startY, startX, width, height);
    }

    private bool Save(PupilCenterChannelSpecularBlockerDTO itemDto, CancellationToken cancellationToken, bool isSave = true) => InvokeSave(update =>
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
        SynchronizationContextProvider.Send(ResultPupilCenterChannelSpecularBlockerDTOList.Clear);
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


