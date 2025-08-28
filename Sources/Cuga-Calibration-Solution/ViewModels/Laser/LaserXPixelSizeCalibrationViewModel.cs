using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Ads.PressureGains;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.XPixelSize;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.View;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;
using System.IO;
using Point = Net.Utilities.Models.Geometries.Point;
using RawImageHelper = Core.Utilities.RawImageHelper;
using Size = Net.Utilities.Models.Geometries.Size;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserXPixelSizeCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserXPixelSizeCalibrationViewModel(
    CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel,
    EnableOpticsMagWindowViewModel enableOpticsMagWindowViewModel,
    EnableStageSpeedWindowViewModel enableStageSpeedWindowViewModel) : CalibrationViewModelBase
{
    #region 属性

    private List<(OpticsMagTypeEnum mag, bool isEnbale)> _enableOpticsMagList = [];

    private List<(StageSpeedEnum stageSpeed, bool isEnbale)> _enableStageSpeedList = [];

    private List<(OpticsMagTypeEnum mag, StageSpeedEnum stageSpeed)> _opticsMagStageSpeedList = [];

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Config" },
        new() { StepName = "Select a Mag" },
        new() { StepName = "Select a Speed" },
        new() { StepName = "Select a location" },
        new() { StepName = "X Pixel Size Calibration" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ObservableCollection<OpticsMagTypeEnumAndStageSpeedEnumCalibrationStatus> _calibrationStatusList =
    [
        ..EnumHelper.Enums<OpticsMagTypeEnum>().Select(t => new OpticsMagTypeEnumAndStageSpeedEnumCalibrationStatus
        {
            OpticsMagTypeEnum = t,
            StageSpeedEnumCalibrationStatusList = [..EnumHelper.Enums<StageSpeedEnum>().Select(tt => new StageSpeedEnumCalibrationStatus { StageSpeedEnum = tt, IsCalibrated = false })]
        })
    ];

    [ObservableProperty]
    private ObservableCollection<StageSpeedEnumCalibrationStatus> _calibrationStatusListItem =
    [
        .. EnumHelper.Enums<StageSpeedEnum>().Select(t => new StageSpeedEnumCalibrationStatus { StageSpeedEnum = t, IsCalibrated = false })
    ];

    [ObservableProperty]
    private LaserXPixelSizeItemDto _laserXPixelSizeItem = new();

    [ObservableProperty]
    private ObservableCollection<LaserXPixelSizeItemDto> _resultLaserXPixelSizeItemList = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ObservableCollection<LaserXPixelSizeItemDto> _reviewList = [];

    [ObservableProperty]
    private ObservableCollection<LaserXPixelSizeItemDto> _selectReviewList = [];

    [ObservableProperty]
    private ObservableCollection<DarkFieldXPixelSizeICropImage> _darkFieldCropImageList = [];

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private LaserXPixelSizeCache _cache = new();

    [ObservableProperty]
    private LaserXPixelSizeItemDto[] _calibrations = [];

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<AdsPressureGainsDto>(out _, out var errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<LaserXPixelSizeCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<LaserXPixelSizeItemDto>();

        foreach (var calibrationStatus in Calibrations)
        {
            CalibrationStatusList
                .Single(t => t.OpticsMagTypeEnum == calibrationStatus.OpticsMagTypeEnum)
                .StageSpeedEnumCalibrationStatusList.Single(t => t.StageSpeedEnum == calibrationStatus.XStageSpeedEnum)
                .IsCalibrated = calibrationStatus.IsCalibrated;
        }

        if (Cache.MicroscopeLensInformation.LensCode == -1) Cache.MicroscopeLensInformation = ApplicationCookie.MicroscopeLensInformationList[0];

        return isHasCache || RecipeCacheProvider.Set(Cache, cancellationToken);
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        ReviewList =
        [
            .. Calibrations
                .Select(t => t.Clone())
                .OrderBy(t => t.OpticsMagTypeEnum)
                .ThenBy(t => t.XStageSpeedEnum)
                .ThenBy(t => t.PmtId)
        ];

        if (ReviewList.All(t => t.IsCalibrated == false))
            return false;
        StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindTemplatePosition);

        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 1:

                return true;

            case 2:
                if (IsRecipeCalibrate)
                {
                    if (await AutomationRecipeInformationAsync() == false) return false;
                }

                return true;

            case 3:

                return true;

            case 4:
                if (ResultLaserXPixelSizeItemList.Count <= 0)
                {
                    DialogWindowProvider.TryShowDialog("Please X Pixel Size Calibration!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    LaserXPixelSizeItem.IsCalibrated = true;
                    if (Save(LaserXPixelSizeItem, cancellationToken) == false)
                    {
                        LaserXPixelSizeItem.IsCalibrated = false;
                        Logger.LogError("{@Name} Error: Save Failed!", Name);
                        return false;
                    }
                }

                CalibrationStatusList.Single(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum)
                    .StageSpeedEnumCalibrationStatusList.Single(t => t.StageSpeedEnum == Cache.XStageSpeedEnum)
                    .IsCalibrated = true;

                IsCalibrated = CalibrationStatusList.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                ClearCalibrationTemp();
                return true;

            default:
                return true;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand]
    private async Task GetPointAsync(string name)
    {
        try
        {
            await Task.Run(() =>
            {
                Cache.FindTemplatePosition = StageViewModel.GetBrightFieldStagePosition();
                Cache.FindStartPosition = new Point(Cache.FindTemplatePosition.X - Cache.DieWidthUm * Cache.ColumnNumber, Cache.FindTemplatePosition.Y);
                Cache.FindEndPosition = new Point(Cache.FindTemplatePosition.X + Cache.DieWidthUm * Cache.ColumnNumber, Cache.FindTemplatePosition.Y);
                Cache.SplitImageCount = Cache.ColumnNumber * 2;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get Point Failed", Name);
        }
    }

    [RelayCommand]
    private async Task GotoPointAsync(string name)
    {
        try
        {
            switch (name)
            {
                case "FindStartPosition":
                    await Task.Run(() => StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindStartPosition)).ConfigureAwait(false);
                    break;

                case "FindEndPosition":
                    await Task.Run(() => StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindEndPosition)).ConfigureAwait(false);
                    break;

                case "FindTemplatePosition":
                    await Task.Run(() => StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindTemplatePosition)).ConfigureAwait(false);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
        }
    }

    [RelayCommand]
    private void ReviewImage(string filePath)
    {
        DialogWindowProvider.ShowImage(filePath);
    }

    [RelayCommand]
    private void Review(LaserXPixelSizeItemDto laserXPixelSizeItemDto)
    {
        var filePathList = new List<(string path, string title)>();
        foreach (var itemImage in laserXPixelSizeItemDto.DarkFieldCropImageList)
        {
            var imageFilePath = (itemImage.FilePath, "X:" + itemImage.Position.X + ", " +
                                                     "Y:" + itemImage.Position.Y);
            filePathList.Add(imageFilePath);
        }

        DialogWindowProvider.ShowImage(filePathList);
    }

    [RelayCommand]
    private async Task MagnificationSelectedAsync(object obj)
    {
        try
        {
            if (obj is not MicroscopeLensInformation)
            {
                Logger.LogError("{@Name}: Select magnification illegal!", Name);
                return;
            }

            await Task.Run(() => MicroscopeViewModel.SwitchMicroscopeLensInformation(ApplicationCookie.MicroscopeLensInformationList.Single(t => t == (MicroscopeLensInformation)obj))
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
        }
    }

    [RelayCommand]
    private Task ConfigStepActionAsync()
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                IsAutoGain = Cache.CIBConfiguration.IsAutoGainControl,
                DcGainVoltage = Cache.CIBConfiguration.Gain,
                IsL0k = Cache.CIBConfiguration.IsL0K,
                CIBProfileTypeEnum = Cache.CIBConfiguration.CIBProfileMode
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                OpticsMagType = Cache.OpticsMagTypeEnum
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                StageSpeed = Cache.XStageSpeedEnum
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            if (await GetTemplateImagePathAsync() == false) return false;
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            if (await Step3ActionAsync(cancellationToken) == false) return false;
            return true;
        });
    }

    private async Task<bool> Step3ActionAsync(CancellationToken cancellationToken)
    {
        var (isSuccess, matchPoint) = await GetXPixelSizeAsync(LaserXPixelSizeItem, Guid.NewGuid(), cancellationToken, false);
        if (isSuccess == false) return false;
        var calPixelDifferences = matchPoint
            .Zip(matchPoint.Skip(1), (prev, curr) => curr.X - prev.X)
            .ToList();
        var umPerPixel = (Cache.DieWidthUm / Vector<double>.Build.DenseOfEnumerable(calPixelDifferences)).Average();
        var error = calPixelDifferences.Max() - calPixelDifferences.Min();
        var resultStatus = Math.Abs(error) < Cache.Threshold;
        LaserXPixelSizeItem.XPixelSize = umPerPixel;
        LaserXPixelSizeItem.DarkFieldCropImageList = [.. DarkFieldCropImageList];
        LaserXPixelSizeItem.IsCalibrated = resultStatus;

        SynchronizationContextProvider.Send(() => ResultLaserXPixelSizeItemList.Add(LaserXPixelSizeItem));
        //DialogWindowProvider.ShowDialog($"Calibrate {(resultStatus ? "OK" : "Failed")},Error: ({error:f2}) Threshold: ({Cache.Threshold})", DialogButtonsEnum.OK,
        //resultStatus ? DialogIconEnum.Information : DialogIconEnum.Warning);
        Logger.LogHtmlInformation("End Split", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            Cache.Threshold,
            Error = $"{error:f2}",
            IdealUmPerPixel = $"{Cache.IdealUmPerPixel:f20}",
            XPixelSize = $"{umPerPixel:f20}",
            Um = new HtmlPlot2DLinesChart([(nameof(calPixelDifferences), calPixelDifferences.ToPoints())], nameof(calPixelDifferences))
        }), HtmlLogUniqueId.LoggingHtml());
        return true;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(async () =>
        {
            if (SelectReviewList.Count == 0)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            foreach (var selectReviewItemDto in SelectReviewList)
            {
                if (await VerifyAsync(selectReviewItemDto, cancellationToken) == false) return false;
                return true;
            }

            return true;
        }).ConfigureAwait(false);
    }

    private async Task<bool> VerifyAsync(LaserXPixelSizeItemDto laserXPixelSizeItemDto, CancellationToken cancellationToken)
    {
        Cache.IdealUmPerPixel = laserXPixelSizeItemDto.XPixelSize;
        Cache.OpticsMagTypeEnum = laserXPixelSizeItemDto.OpticsMagTypeEnum;
        Cache.XStageSpeedEnum = laserXPixelSizeItemDto.XStageSpeedEnum;
        Cache.FindStartPosition = laserXPixelSizeItemDto.FindStartPosition;
        Cache.FindEndPosition = laserXPixelSizeItemDto.FindEndPosition;
        Cache.TemplateFilePath = laserXPixelSizeItemDto.FilePath;
        Cache.TemplateImageFilePath = laserXPixelSizeItemDto.FileTemplatePath;
        var (isSuccess, matchPoint) = await GetXPixelSizeAsync(laserXPixelSizeItemDto, Guid.NewGuid(), cancellationToken, true);
        if (isSuccess == false) return false;

        var differences = matchPoint
            .Zip(matchPoint.Skip(1), (prev, curr) => curr.X - prev.X)
            .ToList();
        var realUmPerPixel = (Cache.DieWidthUm / Vector<double>.Build.DenseOfEnumerable(differences)).Average();

        var error = differences.Max() - differences.Min();
        var resultStatus = Math.Abs(error) < Cache.Threshold;

        Logger.LogHtmlInformation(resultStatus ? "OK" : "Failed", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            NewXPixelSize = realUmPerPixel,
            OldXPixelSize = laserXPixelSizeItemDto.XPixelSize,
            Cache.Threshold,
            Error = error,
            Point = new HtmlPlot2DLinesChart([(nameof(differences), differences.ToPoints())], nameof(differences))
        }), HtmlLogUniqueId.LoggingHtml());

        laserXPixelSizeItemDto.IsVerified = resultStatus;

        if (Save(laserXPixelSizeItemDto, cancellationToken) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
            laserXPixelSizeItemDto.IsVerified = false;
            return false;
        }

        if (!IsAutoCalibrate || !resultStatus)
        {
            DialogWindowProvider.ShowDialog($"Verify {(resultStatus ? "OK" : "Failed")}, New XPixelSize: ({realUmPerPixel}) Old XPixelSize: ({laserXPixelSizeItemDto.XPixelSize})", DialogButtonsEnum.OK,
                resultStatus ? DialogIconEnum.Information : DialogIconEnum.Warning);
        }

        return true;
    }

    private Task<bool> GetTemplateImagePathAsync()
    {
        return Task.Run(() =>
        {
            var darkFieldImageDto = LaserViewModel.GetDarkFieldLineScanImage(
                CalChipSiteModelEnum.ChuckModel,
                Cache.FindTemplatePosition,
                (false, CalibrationSetting.SettingCommonParam.MainLaserLightInformation),
                false,
                Cache.CIBConfiguration,
                Cache.SplitWidthPixel,
                Cache.OpticsMagTypeEnum,
                Cache.XStageSpeedEnum,
                stageCoordinateSystemEnum: StageCoordinateSystemEnum.Bright);
            var detectImageDirectory = ImageFileDirectory;
            using var _ = darkFieldImageDto;

            Cache.TemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.MicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            if (Cache.AlgorithmTemplateTypeEnum == AlgorithmTemplateTypeEnum.Projection)
            {
                if (ReviewViewModel.TryGenerateProjectionTemplate(darkFieldImageDto.Image, Cache.TemplateFilePath) == false)
                {
                    DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }
            }
            else
            {
                var filePath = $"{detectImageDirectory}\\Guid({HtmlLogUniqueId}_{Guid.NewGuid()}).jpg";
                HalconHelper.Save(darkFieldImageDto.Image, filePath);
                createDarkImageTemplateWindowViewModel.ImageFilePath = filePath;
                createDarkImageTemplateWindowViewModel.TemplateFilePath = Cache.TemplateFilePath;

                var showDialog = WindowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel);

                if (showDialog == false)
                {
                    DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                Cache.FindStartPosition = new Point(Cache.FindTemplatePosition.X - Cache.DieWidthUm * Cache.ColumnNumber, Cache.FindTemplatePosition.Y);
                Cache.FindEndPosition = new Point(Cache.FindTemplatePosition.X + Cache.DieWidthUm * Cache.ColumnNumber, Cache.FindTemplatePosition.Y);
                Cache.SplitImageCount = Cache.ColumnNumber * 2;
            }

            Cache.TemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.TemplateFilePath);
            Cache.TemplateFilePath = $"{Cache.TemplateFilePath}.{Cache.AlgorithmTemplateTypeEnum.ToString().ToLower()}";
            Cache.SetMagSpeedTemplateFilePath(Cache.OpticsMagTypeEnum, Cache.XStageSpeedEnum, Cache.TemplateFilePath);
            Logger.LogHtmlInformation("TemplateImage", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.AlgorithmTemplateTypeEnum,
                FindPosition = Cache.FindTemplatePosition,
                Cache.FindStartPosition,
                Cache.FindEndPosition,
                templateFilePath = Cache.TemplateFilePath,
                templateImageFilePath = Cache.TemplateImageFilePath,
                HtmlTab = new HtmlTab(new
                {
                    TemplateImage = new HtmlImage(Cache.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    private Task<(bool isSuccess, List<Point> matchPoint)> GetXPixelSizeAsync(LaserXPixelSizeItemDto laserXPixelSizeItem, Guid guid, CancellationToken cancellationToken, bool isReview)
    {
        var points = new List<Point>();
        return Task.Run(() =>
        {
            try
            {
                if (isReview == false) Cache.GetMagSpeedIdeaXPixelSize(Cache.OpticsMagTypeEnum, Cache.XStageSpeedEnum);
                Cache.GetMagSpeedTemplateFilePath(Cache.OpticsMagTypeEnum, Cache.XStageSpeedEnum);
                ClearCalibrationTemp();
                laserXPixelSizeItem.OpticsMagTypeEnum = Cache.OpticsMagTypeEnum;
                laserXPixelSizeItem.XStageSpeedEnum = Cache.XStageSpeedEnum;
                laserXPixelSizeItem.PmtId = 8;
                laserXPixelSizeItem.FindPosition = Cache.FindTemplatePosition;
                laserXPixelSizeItem.FindStartPosition = Cache.FindStartPosition;
                laserXPixelSizeItem.FindEndPosition = Cache.FindEndPosition;
                laserXPixelSizeItem.FilePath = Cache.TemplateFilePath;
                laserXPixelSizeItem.FileTemplatePath = Cache.TemplateImageFilePath;
                using var templateId = HalconHelper.ReadNccTemplate(Cache.TemplateFilePath);

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    opticsMagType = Cache.OpticsMagTypeEnum,
                    stageSpeed = Cache.XStageSpeedEnum,
                    findStartPosition = Cache.FindStartPosition,
                    findEndPosition = Cache.FindEndPosition,
                    idealUmPerPixel = Cache.IdealUmPerPixel,
                    dieWidthUm = Cache.DieWidthUm,
                    splitWidthPixel = Cache.SplitWidthPixel,
                    splitImageCount = Cache.SplitImageCount,
                    templateFilePath = Cache.TemplateFilePath,
                    templateImageFilePath = Cache.TemplateImageFilePath
                }), HtmlLogUniqueId.LoggingHtml());

                StageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(laserXPixelSizeItem.FindStartPosition, CalChipSiteModelEnum.ChuckModel);
                var startMachinePosition = StageViewModel.GetMachineStagePosition();
                StageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(laserXPixelSizeItem.FindEndPosition, CalChipSiteModelEnum.ChuckModel);
                var endMachinePosition = StageViewModel.GetMachineStagePosition();

                var extendWidth = Cache.SplitWidthPixel * Cache.IdealUmPerPixel / 2.0;

                var (xDirection, _) = StageViewModel.GetMachineDirection();
                // 计算采图的起点终点机械坐标
                startMachinePosition -= new Vector(xDirection * extendWidth, 0);
                endMachinePosition += new Vector(xDirection * 3 * extendWidth, 0);
                //采集长图
                var resultImage = LaserViewModel.GetDarkFieldLineScanImageList(
                    startMachinePosition,
                    endMachinePosition,
                    Cache.OpticsMagTypeEnum,
                    Cache.XStageSpeedEnum,
                    8,
                    StageCoordinateSystemEnum.Machine,
                    Cache.CIBConfiguration,
                    (false, 0.5),
                    false
                );

                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindTemplatePosition);
                var originFilePath = resultImage[2].Url;
                laserXPixelSizeItem.OriginalFilePath = originFilePath;

                var isSplitImage = SplitLongImage(originFilePath, Cache.IdealUmPerPixel, guid, out var matchPoint);
                if (isReview)
                {
                    return (isSplitImage, matchPoint);
                }

                if (isSplitImage == false)
                {
                    while (!isSplitImage)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (matchPoint.Count < 2)
                        {
                            DialogWindowProvider.ShowDialog("Split Image Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            isSplitImage = false;
                            return (false, []);
                        }
                        else
                        {
                            if (matchPoint.Count < Cache.SplitImageCount)
                            {
                                var calPixelDifferences = matchPoint
                                    .Zip(matchPoint.Skip(1), (prev, curr) => curr.X - prev.X)
                                    .ToList();
                                var umPerPixel = (Cache.DieWidthUm / Vector<double>.Build.DenseOfEnumerable(calPixelDifferences)).Average();
                                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                                {
                                    XPixelSize = umPerPixel,
                                }), HtmlLogUniqueId.LoggingHtml());
                                isSplitImage = SplitLongImage(originFilePath, umPerPixel, Guid.NewGuid(), out matchPoint);
                                if (isSplitImage) points = matchPoint;
                            }
                            else
                            {
                                isSplitImage = true;
                                points = matchPoint;
                            }
                        }
                    }
                }
                else
                {
                    points = matchPoint;
                }

                return (true, points);
            }
            catch (Exception ex)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment(ex.Message), HtmlLogUniqueId.LoggingHtml());
                return (false, points);
            }
        });
    }

    private bool SplitLongImage(string uri, double xPixelSize, Guid guid, out List<Point> matchPoint)
    {
        SynchronizationContextProvider.Send(() => DarkFieldCropImageList.Clear());

        using var fileSteam = File.OpenRead(uri);
        using var binaryReader = new BinaryReader(fileSteam);

        var (_, templateImageSize) = ImageHelper.GetImageInfo(Cache.TemplateImageFilePath);
        using var templateId = HalconHelper.ReadNccTemplate(Cache.TemplateFilePath);
        var detectImageDirectory = $"{ImageFileDirectory}\\{Cache.OpticsMagTypeEnum}\\PmtId(8)_Guid({guid}).jpg";
        matchPoint = new List<Point>();

        var (calUmPerPixelBodyBytesSize, calUmPerPixelBodyBytesStartIndex, calUmPerPixelBodyBytesLength) = RawImageHelper.GetSize(binaryReader);
        var (_, calUmPerPixelHeightPixel) = calUmPerPixelBodyBytesSize.DeconstructToInt32();

        var calUmPerPixelDieWidthPixel = Cache.DieWidthUm / xPixelSize;
        var calUmPerPixelSplitImageWidthPixel = Convert.ToInt32(calUmPerPixelDieWidthPixel) / 10;
        var calUmPerPixelHeightPixelByteLength = calUmPerPixelHeightPixel * 2;
        var calUmPerPixelSplitImageAllPixelByteLength = calUmPerPixelSplitImageWidthPixel * calUmPerPixelHeightPixelByteLength;

        // ReadOnlySpan<byte> calUmPerPixelSpan = ((byte[])[1,2,3]).AsSpan().Slice(calUmPerPixelBodyBytesStartIndex, calUmPerPixelBodyBytesLength);
        var calUmPerPixelPointerList = Enumerable
            .Range(0, Cache.SplitImageCount)
            .Select(t => 0 + t * calUmPerPixelDieWidthPixel * calUmPerPixelHeightPixelByteLength)
            .Select(Convert.ToInt64)
            .ToList(); // 分割指针集合
        foreach (var (index, pointer) in calUmPerPixelPointerList.Select((t, i) => (Index: i, Pointer: t)))
        {
            var pointerTemp = pointer - pointer % calUmPerPixelHeightPixelByteLength; // dieWidthPixel不是整数倍, 需要对齐
            if (index > 0)
            {
                pointerTemp -= calUmPerPixelSplitImageAllPixelByteLength / 2;
                pointerTemp -= pointerTemp % calUmPerPixelHeightPixelByteLength; // dieWidthPixel不是整数倍, 需要对齐
            }

            byte[] array;
            if (pointerTemp + calUmPerPixelSplitImageAllPixelByteLength > calUmPerPixelBodyBytesLength)
            {
                if (index != calUmPerPixelPointerList.Count - 1) ThrowHelper.ThrowArgumentException("Data length is not a multiple of width.");

                var offset = calUmPerPixelSplitImageWidthPixel - (calUmPerPixelBodyBytesLength - pointerTemp) / calUmPerPixelHeightPixelByteLength; // 算出右边差多少像素

                pointerTemp += offset * calUmPerPixelHeightPixelByteLength; // 那么左边也去掉这么多像素
                pointerTemp -= pointerTemp % calUmPerPixelHeightPixelByteLength; // dieWidthPixel不是整数倍, 需要对齐

                fileSteam.Seek(calUmPerPixelBodyBytesStartIndex + pointerTemp, SeekOrigin.Begin);
                array = binaryReader.ReadBytes();
            }
            else
            {
                fileSteam.Seek(calUmPerPixelBodyBytesStartIndex + pointerTemp, SeekOrigin.Begin);
                array = binaryReader.ReadBytes(calUmPerPixelSplitImageAllPixelByteLength);
            }

            var currentWidthPixel = array.Length / calUmPerPixelHeightPixelByteLength;
            var calUmPerPixelSplitImageRawBytes = CalibrationAlgorithmService.ToRawBytes(array, new Size(currentWidthPixel, calUmPerPixelHeightPixel));
            var (image, _, _) = CalibrationAlgorithmService.ToHorizontalFlipImageInfo(calUmPerPixelSplitImageRawBytes);
            var calUmPerPixelImageLeftPixel = pointerTemp / calUmPerPixelHeightPixelByteLength;
            using var _ = image;
            var originImageFilePath = Path.Combine(ImageFileDirectory, Path.GetFileNameWithoutExtension(detectImageDirectory), $"calUmPerPixelImage_{index + 1}.jpg");
            HalconHelper.Save(image, originImageFilePath);
            //HalconHelper.TryNccTemplateMathToOffset(image, templateId, out var result, out var score, out var _);
            var isSuccess = CalibrationAlgorithmService.TryTemplateMatchToOffset(Cache.AlgorithmTemplateTypeEnum, image, templateId, out var result, out var _, out var resultScore, out var _);
            if (!isSuccess || resultScore < Cache.NccScoreThreshold)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    Score = resultScore,
                    RawImageFile = new HtmlDownload(calUmPerPixelSplitImageRawBytes, $"calUmPerPixelImage_{index + 1}.raw"),
                    HtmlTab = new HtmlTab(new
                    {
                        OriginImage = new HtmlImage(originImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(result, templateImageSize), new HtmlImageRectangleOverlay(result, templateImageSize)]),
                        TemplateImage = new HtmlImage(Cache.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                }), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            var point = new Point(currentWidthPixel - result.X + calUmPerPixelImageLeftPixel, result.Y); // 水平翻转后的坐标
            var darkFieldCropImage = new DarkFieldXPixelSizeICropImage()
            {
                Position = point,
                Width = currentWidthPixel,
                Height = calUmPerPixelHeightPixel,
                FilePath = originImageFilePath
            };
            SynchronizationContextProvider.Send(() => DarkFieldCropImageList.Add(darkFieldCropImage));
            Logger.LogHtmlInformation($"{index + 1}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                Score = resultScore,
                LeftPixel = calUmPerPixelImageLeftPixel,
                Point = point,
                RawImageFile = new HtmlDownload(calUmPerPixelSplitImageRawBytes, $"calUmPerPixelImage_{index + 1}.raw"),
                HtmlTab = new HtmlTab(new
                {
                    OriginImage = new HtmlImage(originImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(result, templateImageSize), new HtmlImageRectangleOverlay(result, templateImageSize)]),
                    TemplateImage = new HtmlImage(Cache.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            matchPoint.Add(point);
        }

        return true;
    }

    private bool Save(LaserXPixelSizeItemDto itemDto, CancellationToken cancellationToken, bool isSave = true) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        itemDto.MicroscopeLensInformation = Cache.MicroscopeLensInformation;

        Calibrations =
        [
            .. Calibrations
                .Where(t => (t.OpticsMagTypeEnum == itemDto.OpticsMagTypeEnum && t.XStageSpeedEnum == itemDto.XStageSpeedEnum) == false),
            itemDto.Clone(),
        ];

        if (isSave == false) return true;

        return CacheProvider.SetArray(Calibrations, cancellationToken)
               && RecipeCacheProvider.Set(Cache, cancellationToken)
               && EnableDependedCalibrationItems(cancellationToken);
    });

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(() => DarkFieldCropImageList.Clear());
        SynchronizationContextProvider.Send(() => ResultLaserXPixelSizeItemList.Clear());
    }

    #endregion 校准

    #region 自动化校准

    public override void GetAutoCalibrationStep()
    {
        _opticsMagStageSpeedList.Clear();
        var autoCalibrationStepList = new ObservableCollection<CalibrationItemStep>();
        WindowManagerService.ShowDialog(enableOpticsMagWindowViewModel);
        WindowManagerService.ShowDialog(enableStageSpeedWindowViewModel);
        _enableOpticsMagList = [.. enableOpticsMagWindowViewModel.OpticsMagEnableList.Where(t => t.IsEnable).Select(t => (t.OpticsMagTypeEnum, t.IsEnable))];
        _enableStageSpeedList = [.. enableStageSpeedWindowViewModel.StageSpeedEnableList.Where(t => t.IsEnable).Select(t => (t.StageSpeedEnum, t.IsEnable))];
        if (_enableOpticsMagList.Count == 0 || _enableStageSpeedList.Count == 0) return;
        autoCalibrationStepList.Add(new() { StepName = "Loading" });
        foreach (var opticsMag in _enableOpticsMagList)
        {
            foreach (var stageSpeed in _enableStageSpeedList)
            {
                var calibrationItemStep = new CalibrationItemStep()
                {
                    StepName = $"{opticsMag.mag} Mag-{stageSpeed.stageSpeed} Speed",
                };
                autoCalibrationStepList.Add(calibrationItemStep);
                _opticsMagStageSpeedList.Add((opticsMag.mag, stageSpeed.stageSpeed));
            }
        }

        autoCalibrationStepList.Add(new() { StepName = "Review" });
        AutoCalibrationStepList = autoCalibrationStepList;
    }

    public override async Task<bool> AutomationActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            GetAutoCalibrationStep();
            await base.AutomationActionAsync(cancellationToken);
            if (await LoadedingAsync(cancellationToken) == false) return false;
            CalibrationStepIndex = 1;
            if (await NextingAsync(cancellationToken) == false) return false;
            await InvokeCalibrateAsync(() =>
            {
                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.MicroscopeLensInformation.LensName
                }), HtmlLogUniqueId.LoggingHtml());
                return true;
            });
            if (await AutoNextingAsync(cancellationToken) == false) return false;

            foreach (var opticsMagStageSpeed in _opticsMagStageSpeedList)
            {
                CalibrationStepIndex = 4;
                Cache.OpticsMagTypeEnum = opticsMagStageSpeed.Item1;
                Cache.XStageSpeedEnum = opticsMagStageSpeed.Item2;
                if (await AutomationRecipeInformationAsync() == false) return false;
                if (await InvokeCalibrateAsync(async () =>
                    {
                        if (await GetTemplateImagePathAsync() == false) return false;
                        if (await Step3ActionAsync(cancellationToken) == false) return false;
                        return true;
                    }) == false) return false;
                if (await NextingAsync(cancellationToken) == false) return false;
                if (await AutoNextingAsync(cancellationToken) == false) return false;
            }

            AutoReviewCalibrationStepIndex = 10;
            if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false) return false;
            if (await InvokeCalibrateAsync(async () =>
                {
                    foreach (var itemReview in ReviewList)
                    {
                        if (await VerifyAsync(itemReview, cancellationToken) == false) return false;
                    }

                    return true;
                }) == false) return false;
            if (await AutoNextingAsync(cancellationToken) == false) return false;

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Calibration Failed! Error massage:{ex.Message}"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }
    }

    public override async Task<bool> AutomationRecipeInformationAsync(string stepName = "")
    {
        await Task.CompletedTask.ConfigureAwait(false);
        if (IsRecipeCalibrate == false)
            return true;

        if (CalibrationRecipeService.GetCorrectWaferMapByOffset(!IsAutoCalibrate) == false)
            return false;

        if (CalibrationRecipeDto is null)
        {
            DialogWindowProvider.ShowDialog("Revise wafer map is empty!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var originReticle = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument.ReticleModel.Single(t => t.Index is { X: 0, Y: 0 });
        // Bright Field
        if (CalibrationRecipeService.GetLaserReticleMaskMachineInfo(Cache.WaferMaskTypeEnum, Cache.MicroscopeLensInformation, null, null, out var brightFieldMaskInfo) == false)
            return false;
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(originReticle, brightFieldMaskInfo, out var brightFieldMaskPosition);
        Cache.FindTemplatePosition = brightFieldMaskPosition;
        Cache.FindStartPosition = new Point(Cache.FindTemplatePosition.X - Cache.DieWidthUm * Cache.ColumnNumber, Cache.FindTemplatePosition.Y);
        Cache.FindEndPosition = new Point(Cache.FindTemplatePosition.X + Cache.DieWidthUm * Cache.ColumnNumber, Cache.FindTemplatePosition.Y);
        Cache.SplitImageCount = Cache.ColumnNumber * 2;

        // Dark Field
        if (CalibrationRecipeService.GetLaserReticleMaskMachineInfo(Cache.WaferMaskTypeEnum, null, Cache.OpticsMagTypeEnum, Cache.XStageSpeedEnum, out var darkFieldMaskInfo) == false)
            return false;
        Cache.TemplateFilePath = darkFieldMaskInfo.RecipeDarkFieldTemplateDto.TemplateFilePath;
        Cache.TemplateImageFilePath = darkFieldMaskInfo.RecipeDarkFieldTemplateDto.TemplateImageFilePath;
        Cache.SetMagSpeedTemplateFilePath(Cache.OpticsMagTypeEnum, Cache.XStageSpeedEnum, Cache.TemplateFilePath);
        return true;
    }

    private async Task<bool> AutoNextingAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            AutoCalibrationStepIndex++;
            AutoCalibrationProgress = AutoCalibrationStepIndex / (double)AutoCalibrationStepList.Count * 100;
        }, cancellationToken);
        return true;
    }

    public override async Task<bool> AutomationReviewActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            GetAutoCalibrationStep();
            if (_opticsMagStageSpeedList.Count == 0) return false;
            await base.AutomationReviewActionAsync(cancellationToken);
            if (await LoadedingAsync(cancellationToken) == false) return false;
            if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false)
            {
                DialogWindowProvider.ShowDialog($"Please Calibration!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var result = false;
            if (await InvokeVerifyAsync(async () =>
                {
                    foreach (var opticsMagStage in _opticsMagStageSpeedList)
                    {
                        var itemReview = ReviewList.FirstOrDefault(t => t.OpticsMagTypeEnum == opticsMagStage.Item1 && t.XStageSpeedEnum == opticsMagStage.Item2);
                        if (itemReview is not null)
                        {
                            Cache.OpticsMagTypeEnum = opticsMagStage.Item1;
                            Cache.XStageSpeedEnum = opticsMagStage.Item2;
                            if (await AutomationRecipeInformationAsync() == false) return false;
                            if (await GetTemplateImagePathAsync() == false) return false;
                            itemReview.FindStartPosition = Cache.FindStartPosition;
                            itemReview.FindEndPosition = Cache.FindEndPosition;
                            itemReview.FilePath = Cache.TemplateFilePath;
                            itemReview.FileTemplatePath = Cache.TemplateImageFilePath;
                            if (await VerifyAsync(itemReview, cancellationToken) == false) return false;
                        }
                    }

                    result = true;
                    return result;
                }) == false) return false;
            AutoCalibrationProgress = (AutoCalibrationStepIndex + 1) / (double)AutoCalibrationStepList.Count * 100;
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Review Failed! Error massage:{ex.Message}"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }
    }

    #endregion 自动化校准
}