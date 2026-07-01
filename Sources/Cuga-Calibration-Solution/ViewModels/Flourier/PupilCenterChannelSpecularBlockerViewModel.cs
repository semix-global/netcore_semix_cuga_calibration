using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Fourier;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Fourier.CameraAlignment;
using Core.Models.Models.Fourier.CenterChannelFlexibleAperture;
using Core.Models.Models.Fourier.CenterChannelSpecularBlocker;
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
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using Point = Net.Utilities.Models.Geometries.Point;
using Rect = Net.Utilities.Models.Geometries.Rect;
using Size = Net.Utilities.Models.Geometries.Size;

namespace CugaCalibration.ViewModels.Flourier;

[IOCAppService(ServiceType = typeof(PupilCenterChannelSpecularBlockerViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class PupilCenterChannelSpecularBlockerViewModel : CalibrationViewModelBase<PupilCenterChannelSpecularBlockerCache>
{
    #region 界面相关

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public enum PositionShowType
    {
        Position1Angle120,
        Position2Angle180,
        Position3Angle240,
        Position4Angle300
    }

    [ObservableProperty]
    public partial int SelectedTabIndex { get; set; }

    [ObservableProperty]
    public partial int RodWidth { get; set; } = 250;

    [ObservableProperty]
    public partial int RodHight { get; set; } = 50;

    [ObservableProperty]
    public partial int XStartPixel { get; set; } = 100;

    [ObservableProperty]
    public partial int SelectRodWidth { get; set; }

    [ObservableProperty]
    public partial int SelectRodHeight { get; set; }

    [ObservableProperty]
    public partial float Ch3TurnY { get; set; } = 3.0f;

    [ObservableProperty]
    public partial double Ch3TurnYMotorRelation { get; set; } = 13.0f;

    [ObservableProperty]
    public partial double Ch3PushXMotorRelation { get; set; } = 22.0f;

    [ObservableProperty]
    public partial float Ch3Push { get; set; } = 42.0f;

    public PositionShowType[] Ch3PositionTypeValues => Enum.GetValues(typeof(PositionShowType)).Cast<PositionShowType>().ToArray();

    [ObservableProperty]
    public partial PositionShowType SelectedCh3PositionType { get; set; } = (PositionShowType)(-1);

    [ObservableProperty]
    public partial Point SxPos { get; set; }

    [ObservableProperty]
    public partial int ImageWidthPixel { get; set; } = 1000;

    [ObservableProperty]
    public partial ObservableCollection<Rect> CurrentImageRectListCh3 { get; set; } = [];

    [ObservableProperty]
    public partial string ReviewImageShowPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ReviewImageHidePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double[] ReviewImageGrayCh3 { get; set; } = new double[2];

    [ObservableProperty]
    public partial double ReviewImageCompareCh3 { get; set; }

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select Shiny Wafer Position And Productivity Information" },
        new() { StepName = "Pupil Center Channel Specular Blocker Calibration" }
    ];

    #region Calibrate

    [ObservableProperty]
    public partial ObservableCollection<PupilCenterChannelSpecularBlockerDTO> ResultPupilCenterChannelSpecularBlockerDTOList { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<OpticsIlluminationModeAndProductivityInformationStatus> CalibratingStatuses { get; set; } = [];

    #endregion Calibrate

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    public partial PupilCenterChannelSpecularBlockerDTO ResultDto { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    [ObservableProperty]
    public partial PupilCameraAlignmentDTO PupilCameraAlignmentValue { get; set; } = new();

    [ObservableProperty]
    public partial PupilCenterChannelFlexibleApertureDTO PupilCenterChannelFlexibleApertureValue { get; set; } = new();

    [RecipeCache]
    [ObservableProperty]
    public override partial PupilCenterChannelSpecularBlockerCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial PupilCenterChannelSpecularBlockerDTO[] Calibrations { get; set; } = [];

    #endregion 缓存

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false)
            return false;

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);
        PupilCameraAlignmentValue = ApplicationCookieService.GetCalibration<PupilCameraAlignmentDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<PupilCenterChannelSpecularBlockerCache>(cancellationToken);

        Calibrations = ApplicationCookieService.GetCalibrations<PupilCenterChannelSpecularBlockerDTO>(cancellationToken);

        UpdateEntryStatus(Unsafe.As<CalibrationDTOBase[]>(Calibrations), cancellationToken);

        PupilCenterChannelFlexibleApertureValue = ApplicationCookieService.GetCalibration<PupilCenterChannelFlexibleApertureDTO>(cancellationToken);

        Ch3TurnYMotorRelation = PupilCenterChannelFlexibleApertureValue.CgFFBoxTurnYMotorRelationCh3.Count > 0 ? PupilCenterChannelFlexibleApertureValue.CgFFBoxTurnYMotorRelationCh3.Average() : 0;
        Ch3PushXMotorRelation = PupilCenterChannelFlexibleApertureValue.CgFFBoxPushXMotorRelationCh3;
        Ch3Push = (float)PupilCenterChannelFlexibleApertureValue.CgFFBoxPushXMotorPositionCh3;

        ClearCalibrationTemp();

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
            case 1:
                if (Cache.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.OI)
                {
                    ResultDto.ProductivityInformation = Cache.ProductivityInformation;
                    ResultDto.OpticsIlluminationMode = Cache.OpticsIlluminationModeEnum;
                    ResultDto.Ch3Angle = 0;
                    ResultDto.Ch3TurnY = 0;
                    ResultDto.Ch3Push = Cache.Item.Ch3Push;
                }

                if (Cache.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.NI)
                {
                    ResultDto.ProductivityInformation = Cache.ProductivityInformation;
                    ResultDto.OpticsIlluminationMode = Cache.OpticsIlluminationModeEnum;
                    ResultDto.Ch3Angle = Cache.Item.Ch3Angle;
                    ResultDto.Ch3TurnY = Cache.Item.Ch3TurnY;
                    ResultDto.Ch3Push = Cache.Item.Ch3Push;
                }

                SynchronizationContextProvider.Send(() => ResultPupilCenterChannelSpecularBlockerDTOList.Add(ResultDto.Clone()));

                if (ResultPupilCenterChannelSpecularBlockerDTOList.Count <= 0)
                {
                    DialogWindowProvider.TryShowDialog("Please find Offset!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    Calibrations = [.. Calibrations.ToList().Where(t => !(t.ProductivityInformation == Cache.ProductivityInformation))];
                    foreach (var (index, pupilCenterChannelSpecularBlockerDTO) in ResultPupilCenterChannelSpecularBlockerDTOList.Select((dto, i) => (i, dto)))
                    {
                        pupilCenterChannelSpecularBlockerDTO.IsCalibrated = true;
                        if (Save(pupilCenterChannelSpecularBlockerDTO, cancellationToken, index == ResultPupilCenterChannelSpecularBlockerDTOList.Count - 1)) continue;

                        pupilCenterChannelSpecularBlockerDTO.IsCalibrated = false;
                        Logger.LogError("{@Name} Error: Save Failed!", Name);
                        return false;
                    }
                }

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
        FourierViewModel.SetFFHome(FFCH.Ch1);
        FourierViewModel.SetFFHome(FFCH.Ch2);
        FourierViewModel.SetFFHome(FFCH.Ch3_X);
        FourierViewModel.SetFFHome(FFCH.Ch3_Y);

        FourierViewModel.SetFFRPOS_CH3(FFCH.Ch3_X, 0);
        FourierViewModel.SetFFLPOS_CH3(FFCH.Ch3_X, 0);
        FourierViewModel.SetFFPPOS_CH3(FFCH.Ch3_X, 0);
        FourierViewModel.SetFFRPOS_CH3(FFCH.Ch3_Y, 0);
        FourierViewModel.SetFFLPOS_CH3(FFCH.Ch3_Y, 0);
        //Cache.Item.ShinyWaferPosition = StageViewModel.GetBrightFieldStagePosition();
        Cache.Item.ShinyWaferPosition = Guard.IsNotNullAndReturn(MicroscopeCalChip.ShinyWaferItem).BrightFieldMachinePosition;
        Cache.Item.ShinyWaferPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.ShinyWaferPosition);

        Cache.OpticsIlluminationModeEnum = Cache.ProductivityInformation.OpticsIlluminationModeEnum;

        if (Cache.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.OI)
            Ch3TurnY = 0;

        AfViewModel.ToggleDarkFieldEnable(true);
        SxPos = new Point(Cache.Item.ShinyWaferPosition.X, Cache.Item.ShinyWaferPosition.Y);

        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Item.ShinyWaferPosition,
                Cache.Item.LaserLightInformation,
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        if (Cache.Ch3Image != null)
        {
            return InvokeCalibrateAsync(() =>
            {
                if (Cache.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.OI)
                {
                    Cache.Item.Ch3Angle = 0;
                    Cache.Item.Ch3TurnY = 0;
                }

                if (Cache.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.NI)
                {
                    if (SelectedCh3PositionType == PositionShowType.Position1Angle120)
                    {
                        Cache.Item.Ch3Angle = (float)PupilCenterChannelFlexibleApertureValue.CgFFBoxTurnYAngleCh3[0];
                    }

                    if (SelectedCh3PositionType == PositionShowType.Position2Angle180)
                    {
                        Cache.Item.Ch3Angle = (float)PupilCenterChannelFlexibleApertureValue.CgFFBoxTurnYAngleCh3[1];
                    }

                    if (SelectedCh3PositionType == PositionShowType.Position3Angle240)
                    {
                        Cache.Item.Ch3Angle = (float)PupilCenterChannelFlexibleApertureValue.CgFFBoxTurnYAngleCh3[2];
                    }

                    if (SelectedCh3PositionType == PositionShowType.Position4Angle300)
                    {
                        Cache.Item.Ch3Angle = (float)PupilCenterChannelFlexibleApertureValue.CgFFBoxTurnYAngleCh3[3];
                    }

                    Cache.Item.Ch3TurnY = Ch3TurnY;
                }

                Cache.Item.Ch3Push = Ch3Push;

                var chiSquare = Cache.Item.ImageGrayOldCh3 / Cache.Item.ImageGrayNewCh3;
                Cache.Item.ImageGrayCompareCh3 = $"Ch3:Image Gray Compare(old/new) value is: {chiSquare:F4}";
                Logger.LogHtmlInformation("ResultImage", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    ImageGrayCompareCh3 = $"Ch3:Image Gray Compare(old/new) value is: {chiSquare:F4}",
                    InitialImageFilePath3 = Cache.Item.OriginImageFilePathOld,
                    ProcessImageFilePath3 = Cache.Item.OriginImageFilePathNew,
                    Cache.Item.ImageGrayOldCh3,
                    Cache.Item.ImageGrayNewCh3,
                    HtmlTab = new HtmlTab(new
                    {
                        InitialImageCh3 = new HtmlImage(Cache.Item.OriginImageFilePathOld, htmlImageOverlays: CurrentImageRectListCh3.Select(rect => new HtmlImageRectangleOverlay(rect)).ToList()),
                        ProcessImageCh3 = new HtmlImage(Cache.Item.OriginImageFilePathNew, htmlImageOverlays: CurrentImageRectListCh3.Select(rect => new HtmlImageRectangleOverlay(rect)).ToList())
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("CalibrationResult", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Cache.Item.Ch3Angle,
                    Cache.Item.Ch3TurnY,
                    Cache.Item.Ch3Push
                }), HtmlLogUniqueId.LoggingHtml());
                return true;
            });
        }

        DialogWindowProvider.ShowDialog("Make sure you have open image of CH1、CH2、CH3 and save all channel images first，then you can goto next step!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        return Task.FromResult(false);
    }

    [RelayCommand]
    private async Task OpenImageFileCh3Async()
    {
        await Task.Run(() =>
        {
            var ret = FourierViewModel.GetFFReviewImgForTrigger(2, Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Level, SxPos, 100);
            {
                using var bitmap = BytesToBitmapImage(ret);

                Cache.Item.OriginImageFilePathOld = Path.Combine(ImageFileDirectory, "CH3", "Initial" + "__" + $"{Guid.NewGuid():N}.jpg");
                var croppedImage = GetPictureRegion(bitmap, (int)PupilCameraAlignmentValue.RectCh3Position.X, (int)PupilCameraAlignmentValue.RectCh3Position.Y, PupilCameraAlignmentValue.Ch3ImageWidth, PupilCameraAlignmentValue.Ch3ImageHeight);
                Cache.BitmapImageDrawableCh30.BitmapImage = croppedImage;
                Cache.BitmapImageDrawableCh30.BitmapImage.Save(Cache.Item.OriginImageFilePathOld);
                //calibrationAlgorithmService.GetPictureGray(BitmapImageDrawable.BitmapImage.ToHImage(), 255, out var hv_Histo);
                using var hImage = Cache.BitmapImageDrawableCh30.BitmapImage.ToHImage();
                HOperatorSet.Intensity(hImage, hImage, out var meanGrayOld3, out _);
                Cache.Item.ImageGrayOldCh3 = (float)meanGrayOld3.D;
                //histoOld=hv_Histo;
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task SaveImageFileCh3Async()
    {
        await Task.Run(() =>
        {
            for (int j = 0; j < Cache.RectROIDrawableList.Count;)
            {
                var rectRoi = Cache.RectROIDrawableList[j];
                var pushRectVertical = Cache.BitmapImageDrawableCh31.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                SelectRodWidth = (int)pushRectVertical.Width;
                SelectRodHeight = (int)pushRectVertical.Height;
                break;
            }

            if (Cache.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.OI)
            {
                FourierViewModel.SetFFLPOS_CH3(FFCH.Ch3_Y, 0);
                FourierViewModel.SetFFPPOS_CH3(FFCH.Ch3_X, Ch3Push);
            }

            if (Cache.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.NI)
            {
                FourierViewModel.SetFFLPOS_CH3(FFCH.Ch3_Y, Ch3TurnY);
                FourierViewModel.SetFFPPOS_CH3(FFCH.Ch3_X, Ch3Push);
            }

            var ret = FourierViewModel.GetFFReviewImgForTrigger(2, Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Level, SxPos, 100);
            {
                using var bitmap = BytesToBitmapImage(ret);

                Cache.Item.OriginImageFilePathNew = Path.Combine(ImageFileDirectory, "CH3", "_" + Ch3TurnY + "_" + Ch3Push + "_" + $"{Guid.NewGuid():N}.jpg");
                var croppedImage = GetPictureRegion(bitmap, (int)PupilCameraAlignmentValue.RectCh3Position.X, (int)PupilCameraAlignmentValue.RectCh3Position.Y, PupilCameraAlignmentValue.Ch3ImageWidth, PupilCameraAlignmentValue.Ch3ImageHeight);
                Cache.BitmapImageDrawableCh31.BitmapImage = croppedImage;
                Cache.Ch3Image = Cache.BitmapImageDrawableCh31.BitmapImage;
                //var templateFilePath = $"{originImageFilePath}_Template";
                Cache.BitmapImageDrawableCh31.BitmapImage.Save(Cache.Item.OriginImageFilePathNew);
                //calibrationAlgorithmService.GetPictureGray(BitmapImageDrawable.BitmapImage.ToHImage(), 255, out var hv_Histo);
                using var hImage = Cache.BitmapImageDrawableCh31.BitmapImage.ToHImage();
                HOperatorSet.Intensity(hImage, hImage, out var meanGrayNew3, out _);
                Cache.Item.ImageGrayNewCh3 = (float)meanGrayNew3.D;
                //histoNew = hv_Histo;
            }

            {
                CurrentImageRectListCh3 = [];
                for (int j = 0; j < Cache.RectROIDrawableList.Count; j++)
                {
                    var rectRoi = Cache.RectROIDrawableList[j];
                    var rectOne = Cache.BitmapImageDrawableCh31.CartesianCoordinateToImageCoordinate(rectRoi.Rect);
                    CurrentImageRectListCh3 = [.. CurrentImageRectListCh3, rectOne];
                }
            }
        }).ConfigureAwait(false);
    }

    partial void OnSelectedCh3PositionTypeChanged(PositionShowType value)
    {
        // 执行与所选类型相关的业务逻辑
        ExecuteCh3PositionTypeLogic(value);
    }

    // 添加私有方法来处理不同的业务逻辑
    private void ExecuteCh3PositionTypeLogic(PositionShowType positionShowType)
    {
        switch (positionShowType)
        {
            case PositionShowType.Position1Angle120:
                // 执行 swath_all_id 相关的业务逻辑
                HandleId120();
                break;
            case PositionShowType.Position2Angle180:
                // 执行 swath_odd_even 相关的业务逻辑
                HandleId180();
                break;
            case PositionShowType.Position3Angle240:
                // 执行 pmt_id 相关的业务逻辑
                HandleId240();
                break;
            case PositionShowType.Position4Angle300:
                // 执行 swathid_pmtid 相关的业务逻辑
                HandleId300();
                break;
            default:
                // 处理未定义的枚举值
                MessageBox.Show($"未处理的 PositionShowType: {positionShowType}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
        }
    }

    private void HandleId120()
    {
        if (Cache.BitmapImageDrawableCh31.BitmapImage != null)
        {
            var newList = new ObservableCollection<RectROIDrawable>();
            Cache.RectROIDrawableList = newList;

            var currentImageRectListFirstCh3 = PupilCenterChannelFlexibleApertureValue.CgFFBoxPushXRectPositionCh3;
            var imageRect = new Rect(new Point(currentImageRectListFirstCh3.X, currentImageRectListFirstCh3.Y), new Size(currentImageRectListFirstCh3.Width, currentImageRectListFirstCh3.Height));
            var cartesianRect = Cache.BitmapImageDrawableCh31.ImageCoordinateToCartesianCoordinate(imageRect);
            newList.Add(new RectROIDrawable { Rect = cartesianRect, Label = "Push" });

            currentImageRectListFirstCh3 = PupilCenterChannelFlexibleApertureValue.CgFFBoxTurnYRectPositionCh3[0];
            imageRect = new Rect(new Point(currentImageRectListFirstCh3.X, currentImageRectListFirstCh3.Y), new Size(currentImageRectListFirstCh3.Width, currentImageRectListFirstCh3.Height));
            cartesianRect = Cache.BitmapImageDrawableCh31.ImageCoordinateToCartesianCoordinate(imageRect);
            newList.Add(new RectROIDrawable { Rect = cartesianRect, Label = ((int)PupilCenterChannelFlexibleApertureValue.CgFFBoxTurnYAngleCh3[0]).ToString() });

            // 更新UI线程
            Application.Current.Dispatcher.Invoke(() => { Cache.RectROIDrawableList = newList; });
        }
    }

    private void HandleId180()
    {
        if (Cache.BitmapImageDrawableCh31.BitmapImage != null)
        {
            var newList = new ObservableCollection<RectROIDrawable>();
            Cache.RectROIDrawableList = newList;

            var currentImageRectListFirstCh3 = PupilCenterChannelFlexibleApertureValue.CgFFBoxPushXRectPositionCh3;
            var imageRect = new Rect(new Point(currentImageRectListFirstCh3.X, currentImageRectListFirstCh3.Y), new Size(currentImageRectListFirstCh3.Width, currentImageRectListFirstCh3.Height));
            var cartesianRect = Cache.BitmapImageDrawableCh31.ImageCoordinateToCartesianCoordinate(imageRect);
            newList.Add(new RectROIDrawable { Rect = cartesianRect, Label = "Push" });

            currentImageRectListFirstCh3 = PupilCenterChannelFlexibleApertureValue.CgFFBoxTurnYRectPositionCh3[1];
            imageRect = new Rect(new Point(currentImageRectListFirstCh3.X, currentImageRectListFirstCh3.Y), new Size(currentImageRectListFirstCh3.Width, currentImageRectListFirstCh3.Height));
            cartesianRect = Cache.BitmapImageDrawableCh31.ImageCoordinateToCartesianCoordinate(imageRect);
            newList.Add(new RectROIDrawable { Rect = cartesianRect, Label = ((int)PupilCenterChannelFlexibleApertureValue.CgFFBoxTurnYAngleCh3[1]).ToString() });

            // 更新UI线程
            Application.Current.Dispatcher.Invoke(() => { Cache.RectROIDrawableList = newList; });
        }
    }

    private void HandleId240()
    {
        if (Cache.BitmapImageDrawableCh31.BitmapImage != null)
        {
            var newList = new ObservableCollection<RectROIDrawable>();
            Cache.RectROIDrawableList = newList;

            var currentImageRectListFirstCh3 = PupilCenterChannelFlexibleApertureValue.CgFFBoxPushXRectPositionCh3;
            var imageRect = new Rect(new Point(currentImageRectListFirstCh3.X, currentImageRectListFirstCh3.Y), new Size(currentImageRectListFirstCh3.Width, currentImageRectListFirstCh3.Height));
            var cartesianRect = Cache.BitmapImageDrawableCh31.ImageCoordinateToCartesianCoordinate(imageRect);
            newList.Add(new RectROIDrawable { Rect = cartesianRect, Label = "Push" });

            currentImageRectListFirstCh3 = PupilCenterChannelFlexibleApertureValue.CgFFBoxTurnYRectPositionCh3[2];
            imageRect = new Rect(new Point(currentImageRectListFirstCh3.X, currentImageRectListFirstCh3.Y), new Size(currentImageRectListFirstCh3.Width, currentImageRectListFirstCh3.Height));
            cartesianRect = Cache.BitmapImageDrawableCh31.ImageCoordinateToCartesianCoordinate(imageRect);
            newList.Add(new RectROIDrawable { Rect = cartesianRect, Label = ((int)PupilCenterChannelFlexibleApertureValue.CgFFBoxTurnYAngleCh3[2]).ToString() });

            // 更新UI线程
            Application.Current.Dispatcher.Invoke(() => { Cache.RectROIDrawableList = newList; });
        }
    }

    private void HandleId300()
    {
        if (Cache.BitmapImageDrawableCh31.BitmapImage != null)
        {
            var newList = new ObservableCollection<RectROIDrawable>();
            Cache.RectROIDrawableList = newList;

            var currentImageRectListFirstCh3 = PupilCenterChannelFlexibleApertureValue.CgFFBoxPushXRectPositionCh3;
            var imageRect = new Rect(new Point(currentImageRectListFirstCh3.X, currentImageRectListFirstCh3.Y), new Size(currentImageRectListFirstCh3.Width, currentImageRectListFirstCh3.Height));
            var cartesianRect = Cache.BitmapImageDrawableCh31.ImageCoordinateToCartesianCoordinate(imageRect);
            newList.Add(new RectROIDrawable { Rect = cartesianRect, Label = "Push" });

            currentImageRectListFirstCh3 = PupilCenterChannelFlexibleApertureValue.CgFFBoxTurnYRectPositionCh3[3];
            imageRect = new Rect(new Point(currentImageRectListFirstCh3.X, currentImageRectListFirstCh3.Y), new Size(currentImageRectListFirstCh3.Width, currentImageRectListFirstCh3.Height));
            cartesianRect = Cache.BitmapImageDrawableCh31.ImageCoordinateToCartesianCoordinate(imageRect);
            newList.Add(new RectROIDrawable { Rect = cartesianRect, Label = ((int)PupilCenterChannelFlexibleApertureValue.CgFFBoxTurnYAngleCh3[3]).ToString() });

            // 更新UI线程
            Application.Current.Dispatcher.Invoke(() => { Cache.RectROIDrawableList = newList; });
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GetLightShowAsync(CancellationToken cancellationToken)
    {
        FourierViewModel.SetFFHome(FFCH.Ch1);
        FourierViewModel.SetFFHome(FFCH.Ch2);
        FourierViewModel.SetFFHome(FFCH.Ch3_X);
        FourierViewModel.SetFFHome(FFCH.Ch3_Y);

        FourierViewModel.SetFFRPOS_CH3(FFCH.Ch3_X, 0);
        FourierViewModel.SetFFLPOS_CH3(FFCH.Ch3_X, 0);
        FourierViewModel.SetFFPPOS_CH3(FFCH.Ch3_X, 0);
        FourierViewModel.SetFFRPOS_CH3(FFCH.Ch3_Y, 0);
        FourierViewModel.SetFFLPOS_CH3(FFCH.Ch3_Y, 0);
        StageViewModel.SetAbsoluteStageTheta(0);
        StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(Cache.Item.ShinyWaferPosition);
        AfViewModel.ToggleDarkFieldEnable(true);

        Point darkFieldPosition = StageViewModel.GetDarkFieldStagePosition();
        {
            using var darkFieldImage1 = await CIBViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Dark,
                darkFieldPosition,
                ImageWidthPixel,
                Cache.Item.CIBInformation3,
                (false, CalChipSiteModelEnum.ShinyWaferModel),
                (false, Cache.Item.OpticsConfiguration),
                (false, Cache.Item.CIBConfiguration),
                (false, Cache.Item.LaserLightInformation),
                false,
                cancellationToken);
            ReviewImageShowPath = Path.Combine(ImageFileDirectory, "CH3", "__" + "LightShow" + "__" + $"{Guid.NewGuid():N}.jpg");

            using var hImage = darkFieldImage1.Image.ToHImage();
            ReviewImageGrayCh3[0] = hImage.GetIntensity().Average;
            darkFieldImage1.Image.Save(ReviewImageShowPath);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GetLightHideAsync(CancellationToken cancellationToken)
    {
        if (Cache.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.OI)
        {
            FourierViewModel.SetFFLPOS_CH3(FFCH.Ch3_Y, 0);
            FourierViewModel.SetFFPPOS_CH3(FFCH.Ch3_X, Ch3Push);
        }

        if (Cache.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.NI)
        {
            FourierViewModel.SetFFLPOS_CH3(FFCH.Ch3_Y, Ch3TurnY);
            FourierViewModel.SetFFPPOS_CH3(FFCH.Ch3_X, Ch3Push);
        }

        StageViewModel.SetAbsoluteStageTheta(0d);
        StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(Cache.Item.ShinyWaferPosition);
        AfViewModel.ToggleDarkFieldEnable(true);

        Point darkFieldPosition = StageViewModel.GetDarkFieldStagePosition();
        {
            using var darkFieldImage1 = await CIBViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Dark,
                darkFieldPosition,
                ImageWidthPixel,
                Cache.Item.CIBInformation3,
                (false, CalChipSiteModelEnum.ShinyWaferModel),
                (false, Cache.Item.OpticsConfiguration),
                (false, Cache.Item.CIBConfiguration),
                (false, Cache.Item.LaserLightInformation),
                false,
                cancellationToken);
            ReviewImageHidePath = Path.Combine(ImageFileDirectory, "CH3", "__" + "LightHide" + "__" + $"{Guid.NewGuid():N}.jpg");

            using var hImage = darkFieldImage1.Image.ToHImage();
            ReviewImageGrayCh3[1] = hImage.GetIntensity().Average;
            darkFieldImage1.Image.Save(ReviewImageHidePath);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GetImageCompareAsync(CancellationToken cancellationToken)
    {
        ReviewImageCompareCh3 = ReviewImageGrayCh3[0] / ReviewImageGrayCh3[1];

        await InvokeVerifyAsync(() =>
        {
            Logger.LogHtmlInformation("ReviewImage:CoverBefor", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                LightShowImage = new HtmlImage(ReviewImageShowPath)
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlInformation("ReviewImage:CoverAfter", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                LightHideImage = new HtmlImage(ReviewImageHidePath)
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlInformation("VerifyResult", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                ReviewImageGrayCh3Before = ReviewImageGrayCh3[0],
                ReviewImageGrayCh3After = ReviewImageGrayCh3[1],
                ReviewImageCompareCh3
            }), HtmlLogUniqueId.LoggingHtml());

            if (ResultPupilCenterChannelSpecularBlockerDTOList.Count <= 0)
            {
                DialogWindowProvider.TryShowDialog("Please find Offset!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
            }
            else
            {
                foreach (var (index, pupilCenterChannelSpecularBlockerDTO) in ResultPupilCenterChannelSpecularBlockerDTOList.Select((dto, i) => (i, dto)))
                {
                    if (pupilCenterChannelSpecularBlockerDTO.ProductivityInformation == Cache.ProductivityInformation)
                    {
                        pupilCenterChannelSpecularBlockerDTO.IsVerified = true;
                        if (Save(pupilCenterChannelSpecularBlockerDTO, cancellationToken, index == ResultPupilCenterChannelSpecularBlockerDTOList.Count - 1)) break;

                        pupilCenterChannelSpecularBlockerDTO.IsVerified = false;
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

    private bool Save(PupilCenterChannelSpecularBlockerDTO itemDto, CancellationToken cancellationToken, bool isSave = true) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        Calibrations =
        [
            itemDto,
            .. Calibrations
                .Where(t => (t.OpticsIlluminationMode == itemDto.OpticsIlluminationMode && t.ProductivityInformation == itemDto.ProductivityInformation) == false)
        ];
        if (!isSave) return;

        ApplicationCookieService.SetCalibrations(Calibrations, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var temps = Guard.IsAssignableToTypeAndReturn<PupilCenterChannelSpecularBlockerDTO[]>(calibrations);
        var status = Entry.Status;

        CalibratingStatuses =
        [
            .. ApplicationCookie.OpticsIlluminationModeEnums.Select(t => new OpticsIlluminationModeAndProductivityInformationStatus
            {
                SelectedItem = t,
                ProductivityInformationStatusList = [.. ApplicationCookie.GetProductivityInformations(t).Select(tt => new ProductivityInformationStatus { SelectedItem = tt, IsCalibrated = false })]
            })
        ];

        Calibrations =
        [
            .. temps
                .Where(t => ApplicationCookie.OpticsIlluminationModeEnums.Contains(t.OpticsIlluminationMode)
                            && ApplicationCookie.ProductivityInformations.Contains(t.ProductivityInformation))
                .DistinctBy(t => (t.OpticsIlluminationMode, t.ProductivityInformation))
                .Select(t =>
                {
                    CalibratingStatuses
                        .Single(tt => tt.SelectedItem == t.OpticsIlluminationMode)
                        .ProductivityInformationStatusList
                        .Single(tt => tt.SelectedItem == t.ProductivityInformation)
                        .IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        status.TotalCalibrationCount = ApplicationCookie.OpticsIlluminationModeEnums.Count * ApplicationCookie.ProductivityInformations.Count;
        status.CalibratedCount = Calibrations.Count(t => t.IsCalibrated);
        status.VerifiedCount = Calibrations.Count(t => t.IsVerified);
        status.Details =
        [
            .. ApplicationCookie.OpticsIlluminationModeEnums.SelectMany(opticsIlluminationModeEnum =>
                ApplicationCookie.ProductivityInformations.Select(productivityInformation =>
                {
                    var item = Calibrations.SingleOrDefault(tt => tt.OpticsIlluminationMode == opticsIlluminationModeEnum
                                                                  && tt.ProductivityInformation == productivityInformation);

                    return new CalibrationViewModelStatus.Detail(
                        $"{opticsIlluminationModeEnum}/ {productivityInformation}",
                        item?.IsCalibrated,
                        item?.IsVerified);
                }))
        ];
    }

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(ResultPupilCenterChannelSpecularBlockerDTOList.Clear);
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