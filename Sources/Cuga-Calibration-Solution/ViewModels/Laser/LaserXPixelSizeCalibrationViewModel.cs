using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Ads.PressureGains;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.XPixelSize;
using Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.View;
using HalconDotNet;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Channels;
using Point = Net.Utilities.Models.Geometries.Point;
using Size = Net.Utilities.Models.Geometries.Size;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserXPixelSizeCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserXPixelSizeCalibrationViewModel(
    CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel,
    EnableProductiveInformationWindowViewModel enableProductiveInformationWindowViewModel,
    ICalibrationAlgorithmService calibrationAlgorithmService) : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();
    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    private List<(ProductivityInformation productiveInformation, bool isEnbale)> _enableProductiveInformationList = [];

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Config" },
        new() { StepName = "Select a Mag" },
        new() { StepName = "Select a Speed" },
        new() { StepName = "Select a location" },
        new() { StepName = "X Pixel Size Calibration" }
    ];

    [ObservableProperty]
    private int _splitWidthPixel = 1000;

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ObservableCollection<ProductivityInformationCalibrationStatus> _calibrationStatusList = [];

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

        if (CalibrationStatusList.Count == 0)
            CalibrationStatusList =
            [
                .. ApplicationCookie.OpticsMagTypeProductivityInformations.Select(t => new ProductivityInformationCalibrationStatus { ProductivityInformation = t, IsCalibrated = false })
            ];
        Calibrations =
        [
            ..Calibrations.Where(t => ApplicationCookie.ProductivityInformations.Contains(t.ProductivityInformation))
                .Select(t =>
                {
                    t.IsCalibrated = CalibrationStatusList.Single(tt => tt.ProductivityInformation == t.ProductivityInformation).IsCalibrated;
                    return t;
                })
        ];

        if (Cache.MicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.MicroscopeLensInformation = CalibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        ReviewList =
        [
            .. Calibrations
                .Select(t => t.Clone())
                .OrderBy(t => t.ProductivityInformation)
                .ThenBy(t => t.PmtId)
        ];

        if (ReviewList.All(t => t.IsCalibrated == false))
            return false;
        StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.Item.FindTemplatePosition);

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

                    if (EnableDependedCalibrationItems(cancellationToken) == false)
                    {
                        Logger.LogError("{@Name} Error: Enable Depended Calibration Items Failed!", Name);
                        return false;
                    }
                }

                CalibrationStatusList.Single(t => t.ProductivityInformation == Cache.ProductivityInformation)
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
                Cache.Item.FindTemplatePosition = StageViewModel.GetBrightFieldStagePosition();
                Cache.FindStartPosition = new Point(Cache.Item.FindTemplatePosition.X - Cache.DieWidthUm * Cache.ColumnNumber, Cache.Item.FindTemplatePosition.Y);
                Cache.FindEndPosition = new Point(Cache.Item.FindTemplatePosition.X + Cache.DieWidthUm * Cache.ColumnNumber, Cache.Item.FindTemplatePosition.Y);
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
                    await Task.Run(() => StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.Item.FindTemplatePosition)).ConfigureAwait(false);
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
                Cache.ProductivityInformation
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
                StageSpeed = Cache.ProductivityInformation.StageSpeedType
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
        Logger.LogHtmlInformation("End Split", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            Cache.Threshold,
            Error = $"{error:f2}",
            //IdealUmPerPixel = $"{Cache.IdealUmPerPixel:f20}",
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
        Cache.ProductivityInformation = laserXPixelSizeItemDto.ProductivityInformation;
        Cache.FindStartPosition = laserXPixelSizeItemDto.FindStartPosition;
        Cache.FindEndPosition = laserXPixelSizeItemDto.FindEndPosition;
        Cache.Item.TemplateFilePath = laserXPixelSizeItemDto.FilePath;
        Cache.Item.TemplateImageFilePath = laserXPixelSizeItemDto.FileTemplatePath;
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
                Cache.Item.FindTemplatePosition,
                (false, CalibrationSetting.SettingCommonParam.MainLaserLightInformation),
                false,
                Cache.CIBConfiguration,
                Cache.ProductivityInformation,
                Cache.SplitWidthPixel,
                stageCoordinateSystemEnum: StageCoordinateSystemEnum.Bright);
            var detectImageDirectory = ImageFileDirectory;
            using var _ = darkFieldImageDto;

            Cache.Item.TemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.MicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            if (Cache.AlgorithmTemplateTypeEnum == AlgorithmTemplateTypeEnum.Projection)
            {
                if (ReviewViewModel.TryGenerateProjectionTemplate(darkFieldImageDto.Image, Cache.Item.TemplateFilePath) == false)
                {
                    DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }
            }
            else
            {
                var filePath = $"{detectImageDirectory}\\Guid({HtmlLogUniqueId}_{Guid.NewGuid()}).jpg";
                darkFieldImageDto.Image.Save(filePath);
                createDarkImageTemplateWindowViewModel.ImageFilePath = filePath;
                createDarkImageTemplateWindowViewModel.TemplateFilePath = Cache.Item.TemplateFilePath;

                var showDialog = WindowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel);

                if (showDialog == false)
                {
                    DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                Cache.FindStartPosition = new Point(Cache.Item.FindTemplatePosition.X - Cache.DieWidthUm * Cache.ColumnNumber, Cache.Item.FindTemplatePosition.Y);
                Cache.FindEndPosition = new Point(Cache.Item.FindTemplatePosition.X + Cache.DieWidthUm * Cache.ColumnNumber, Cache.Item.FindTemplatePosition.Y);
                Cache.SplitImageCount = Cache.ColumnNumber * 2;
            }

            Cache.Item.TemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.Item.TemplateFilePath);
            Cache.Item.TemplateFilePath = $"{Cache.Item.TemplateFilePath}.{Cache.AlgorithmTemplateTypeEnum.ToString().ToLower()}";
            Logger.LogHtmlInformation("TemplateImage", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.AlgorithmTemplateTypeEnum,
                FindPosition = Cache.Item.FindTemplatePosition,
                Cache.FindStartPosition,
                Cache.FindEndPosition,
                templateFilePath = Cache.Item.TemplateFilePath,
                templateImageFilePath = Cache.Item.TemplateImageFilePath,
                HtmlTab = new HtmlTab(new
                {
                    TemplateImage = new HtmlImage(Cache.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    private Task<(bool isSuccess, List<Point> matchPoint)> GetXPixelSizeAsync(LaserXPixelSizeItemDto laserXPixelSizeItem, Guid guid, CancellationToken cancellationToken, bool isReview)
    {
        var points = new List<Point>();
        return Task.Run(async () =>
        {
            try
            {
                ClearCalibrationTemp();
                laserXPixelSizeItem.ProductivityInformation = Cache.ProductivityInformation;
                laserXPixelSizeItem.PmtId = 8;
                laserXPixelSizeItem.FindPosition = Cache.Item.FindTemplatePosition;
                laserXPixelSizeItem.FindStartPosition = Cache.FindStartPosition;
                laserXPixelSizeItem.FindEndPosition = Cache.FindEndPosition;
                laserXPixelSizeItem.FilePath = Cache.Item.TemplateFilePath;
                laserXPixelSizeItem.FileTemplatePath = Cache.Item.TemplateImageFilePath;
                using var templateId = Cache.Item.TemplateFilePath.ReadNccTemplate();

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    opticsMagType = Cache.ProductivityInformation.OpticsMagType,
                    stageSpeed = Cache.ProductivityInformation.StageSpeedType,
                    findStartPosition = Cache.FindStartPosition,
                    findEndPosition = Cache.FindEndPosition,
                    dieWidthUm = Cache.DieWidthUm,
                    splitWidthPixel = Cache.SplitWidthPixel,
                    splitImageCount = Cache.SplitImageCount,
                    templateFilePath = Cache.Item.TemplateFilePath,
                    templateImageFilePath = Cache.Item.TemplateImageFilePath
                }), HtmlLogUniqueId.LoggingHtml());

                StageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(laserXPixelSizeItem.FindStartPosition, CalChipSiteModelEnum.ChuckModel);
                var startMachinePosition = StageViewModel.GetMachineStagePosition();
                StageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(new Point(Cache.ChuckRadius - 10, laserXPixelSizeItem.FindStartPosition.Y), CalChipSiteModelEnum.ChuckModel);
                var endMachinePosition = StageViewModel.GetMachineStagePosition();

                var extendWidth = Cache.SplitWidthPixel * 0.333 / 2.0;

                var (xDirection, _) = StageViewModel.GetMachineDirection();
                startMachinePosition -= new Vector(xDirection * extendWidth, 0);

                //采集长图
                var resultImage = LaserViewModel.GetDarkFieldLineScanImageList(
                    startMachinePosition,
                    endMachinePosition,
                    Cache.ProductivityInformation,
                    8,
                    StageCoordinateSystemEnum.Machine,
                    Cache.CIBConfiguration,
                    (false, CalibrationSetting.SettingCommonParam.MainLaserLightInformation),
                    false
                );

                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.Item.FindTemplatePosition);
                var originFilePath = resultImage[2].Url;
                laserXPixelSizeItem.OriginalFilePath = originFilePath;

                var (isSplitImage, matchPoint) = await SplitLongImageAsync(originFilePath, guid).ConfigureAwait(false);
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
                                    XPixelSize = umPerPixel
                                }), HtmlLogUniqueId.LoggingHtml());
                                (isSplitImage, matchPoint) = await SplitLongImageAsync(originFilePath, Guid.NewGuid()).ConfigureAwait(false);
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

    private async Task<(bool isSuccess, List<Point> matchPoints)> SplitLongImageAsync(string uri, Guid guid)
    {
        SynchronizationContextProvider.Send(() => DarkFieldCropImageList.Clear());
        var matchPoint = new List<Point>(); // 必须立即初始化
        // 使用 Task.Run 并等待结果,//var task = Task.Run(() =>
        {
            using var fileSteam = File.OpenRead(uri);
            using var binaryReader = new BinaryReader(fileSteam, Encoding.UTF8, true);
            var detectImageDirectory = $"{ImageFileDirectory}\\{Cache.ProductivityInformation.OpticsMagType}\\PmtId(8)_Guid({guid}).jpg";
            using var templateId = Cache.Item.TemplateFilePath.ReadNccTemplate();
            var (_, templateImageSize) = ImageHelper.GetImageInfo(Cache.Item.TemplateImageFilePath);

            try
            {
                #region Get Um Per Pixel

                Logger.LogHtmlInformation("1. Get Um Per Pixel", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                Logger.LogHtmlInformation("1.1 Param", HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
                {
                    detectImageDirectory,
                    Cache.DieWidthUm,
                    Cache.SlideWindowValue,
                    Cache.SlideStepValue,
                    uri,
                    TemplateImage = new HtmlImage(Cache.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                }), HtmlLogUniqueId.LoggingHtml());


                var (calUmPerPixelBodyBytesSize, calUmPerPixelBodyBytesStartIndex, calUmPerPixelBodyBytesLength) = RawImageFactory.GetSize(binaryReader);
                var (_, calUmPerPixelHeightPixel) = (SizeI)calUmPerPixelBodyBytesSize;

                var calUmPerPixelSplitImageWidthPixel = Cache.SlideWindowValue;
                var calUmPerPixelHeightPixelByteLength = calUmPerPixelHeightPixel * 2;
                var calUmPerPixelSplitImageAllPixelByteLength = calUmPerPixelSplitImageWidthPixel * calUmPerPixelHeightPixelByteLength;

                // 预计算所有需要处理的块信息
                var blockInfos = new List<(int k, long pointerTemp, int byteLength, int currentWidthPixel, long calUmPerPixelImageLeftPixel)>();

                for (int k = 0; ; k++)
                {
                    long pointerTemp;
                    if (k == 0)
                        pointerTemp = 0;
                    else
                        pointerTemp = k * (Cache.SlideWindowValue * calUmPerPixelHeightPixelByteLength - (Cache.SlideWindowValue - Cache.SlideStepValue) * calUmPerPixelHeightPixelByteLength);

                    int byteLength;
                    bool isLastBlock = false;

                    if (pointerTemp + calUmPerPixelSplitImageAllPixelByteLength > calUmPerPixelBodyBytesLength)
                    {
                        var offset = Cache.SlideWindowValue - (calUmPerPixelBodyBytesLength - pointerTemp) / calUmPerPixelHeightPixelByteLength;
                        pointerTemp -= offset * calUmPerPixelHeightPixelByteLength;
                        pointerTemp -= pointerTemp % calUmPerPixelHeightPixelByteLength;
                        isLastBlock = true;
                        byteLength = calUmPerPixelSplitImageAllPixelByteLength;
                    }
                    else
                    {
                        byteLength = calUmPerPixelSplitImageAllPixelByteLength;
                    }

                    var currentWidthPixel = byteLength / calUmPerPixelHeightPixelByteLength;
                    var calUmPerPixelImageLeftPixel = pointerTemp / calUmPerPixelHeightPixelByteLength;

                    blockInfos.Add((k, pointerTemp, byteLength, currentWidthPixel, calUmPerPixelImageLeftPixel));

                    if (isLastBlock) break;
                }

                // 定义处理结果的数据结构
                var results = new ConcurrentBag<(int k, Point matchPoint, double score, long pointerTemp, int currentWidthPixel, long calUmPerPixelImageLeftPixel)>();
                var scoreList = new ConcurrentBag<Point>();
                var scoreCalibrateList = new ConcurrentBag<Point>();

                // 使用Channel实现生产者和消费者模式，带取消功能
                await ProcessWithChannelAndCancellationAsync(
                    blockInfos,
                    fileSteam,
                    binaryReader,
                    templateId,
                    calUmPerPixelHeightPixel,
                    calUmPerPixelBodyBytesStartIndex,
                    results,
                    scoreList,
                    scoreCalibrateList).ConfigureAwait(false);
                // 按原始顺序排序结果
                var orderedResults = results.OrderBy(r => r.k).ToList();
                var calUmPerPixelMatchPoint = orderedResults.Select(r => r.matchPoint).ToList();
                var orderedScores = scoreList.OrderBy(s => s.X).ToList();
                var orderedCalibrateScores = scoreCalibrateList.OrderBy(s => s.X).ToList();

                var calUmPerPixelMatchPointListValid = new List<Point>();
                var scoreListValid = new List<Point>();
                for (int j = 0; j < scoreList.Count; j++)
                {
                    if (orderedScores[j].Y > Cache.NccScoreThreshold)
                    {
                        //scoreListValid.Add(orderedScores[j]);
                        scoreListValid.Add(orderedCalibrateScores[j]);
                        calUmPerPixelMatchPointListValid.Add(calUmPerPixelMatchPoint[j]);
                    }
                }

                scoreListValid = scoreListValid
                    .Where(point => point.Y > Cache.NccScoreThreshold)
                    .ToList();

                List<Point> differencesMatchPointList = calUmPerPixelMatchPointListValid
                    .Zip(calUmPerPixelMatchPointListValid.Skip(1), (prev, curr) => new Point(curr.X - prev.X, curr.Y - prev.Y))
                    .ToList();
                List<double> differencesMatchPointXList = differencesMatchPointList.Select(p => p.X).Where(x => x > 10).ToList();
                var averageXList = differencesMatchPointXList.Average();
                List<double> differencesMatchPointXListAverage = differencesMatchPointXList.Where(x => (x >= averageXList) && (x < averageXList + 1000)).ToList();
                List<double> averagePointXList = Enumerable.Repeat(averageXList, differencesMatchPointXList.Count).ToList();

                Logger.LogHtmlInformation("All Match Point Initial Scores", HtmlHeaderLevelEnum.Header6, new HtmlQuote(new
                {
                    TraceBufferList = new HtmlPlot2DLinesChart([("orderedScoresInitialList", orderedCalibrateScores.ToArray())], string.Empty)
                }), HtmlLogUniqueId.LoggingHtml());
                Logger.LogHtmlInformation("All Match Point Process Scores", HtmlHeaderLevelEnum.Header6, new HtmlQuote(new
                {
                    TraceBufferList = new HtmlPlot2DLinesChart([("orderedScoresProcessList", scoreListValid.ToArray())], string.Empty)
                }), HtmlLogUniqueId.LoggingHtml());
                Logger.LogHtmlInformation("Origin Match Point Analysis Point", HtmlHeaderLevelEnum.Header6, new HtmlQuote(new
                {
                    TraceBufferList = new HtmlPlot2DLinesChart([("MatchPointList", calUmPerPixelMatchPointListValid.ToArray())], string.Empty)
                }), HtmlLogUniqueId.LoggingHtml());
                Logger.LogHtmlInformation("Origin Match Point Analysis X", HtmlHeaderLevelEnum.Header6, new HtmlQuote(new
                {
                    TraceBufferList = new HtmlPlot2DLinesChart([("MatchPointList X", calUmPerPixelMatchPointListValid.Select(p => p.X).ToPoints())], string.Empty)
                }), HtmlLogUniqueId.LoggingHtml());

                var realUmPerPixel = Cache.DieWidthUm / Vector<double>.Build.DenseOfEnumerable(differencesMatchPointXListAverage).Average();
                var (calUmPerPixelWidthPixel, _) = (SizeI)calUmPerPixelBodyBytesSize;
                Cache.SplitImageCount = (int)(calUmPerPixelWidthPixel * 1.0 / (Cache.DieWidthUm / realUmPerPixel));

                Logger.LogHtmlInformation("1.2. End Split", HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
                {
                    RealUmPerPixel = $"{realUmPerPixel:f20}",
                    //Um = new HtmlPlot2DLinesChart([(nameof(differencesMatchPointList), differencesMatchPointList.ToArray())], nameof(differencesMatchPointList))
                    UmX = new HtmlPlot2DLinesChart([("differencesMatchPointListX", differencesMatchPointList.Select(p => p.X).ToPoints())], "differencesMatchPointListX"),
                    UmXProcess = new HtmlPlot2DLinesChart([("differencesMatchPointListX_Process", differencesMatchPointXList.ToPoints()), ("differencesMatchPointListAverage", averagePointXList.ToPoints())], "differencesMatchPointListX_Process")
                }), HtmlLogUniqueId.LoggingHtml());

                #endregion Get Um Per Pixel

                Logger.LogHtmlInformation("2. Split Image", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                Logger.LogHtmlInformation("2.1 Param", HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
                {
                    detectImageDirectory,
                    Cache.DieWidthUm,
                    RealUmPerPixel = $"{realUmPerPixel:f20}",
                    uri,
                    SplitWidthPixel,
                    SplitCount = Cache.SplitImageCount,
                    TemplateImage = new HtmlImage(Cache.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                }), HtmlLogUniqueId.LoggingHtml());

                var (bodyBytesSize, bodyBytesStartIndex, bodyBytesLength) = RawImageFactory.GetSize(binaryReader);
                var (_, heightPixel) = (SizeI)bodyBytesSize;

                var dieWidthPixel = Cache.DieWidthUm / realUmPerPixel;
                var heightPixelByteLength = heightPixel * 2;
                var splitImageAllPixelByteLength = SplitWidthPixel * heightPixelByteLength;

                var matchOffsetPoint = new List<Point>();
                var pointerList = Enumerable
                    .Range(0, Cache.SplitImageCount + 1)
                    .Select((count, index) => index == 0 ? 0 : count * dieWidthPixel * heightPixelByteLength)
                    .Select(Convert.ToInt64)
                    .ToList();
                foreach (var (index, pointer) in pointerList.Select((t, i) => (Index: i, Pointer: t)))
                {
                    var pointerTemp = pointer - pointer % heightPixelByteLength; // dieWidthPixel不是整数倍, 需要对齐
                    byte[] array;
                    if (pointerTemp + splitImageAllPixelByteLength > bodyBytesLength)
                    {
                        if (index != pointerList.Count - 1) ThrowHelper.ThrowArgumentException("Data length is not a multiple of width.");

                        var offset = SplitWidthPixel - (bodyBytesLength - pointerTemp) / heightPixelByteLength;

                        pointerTemp += offset * heightPixelByteLength;
                        pointerTemp -= pointerTemp % heightPixelByteLength; // dieWidthPixel不是整数倍, 需要对齐

                        fileSteam.Seek(bodyBytesStartIndex + pointerTemp, SeekOrigin.Begin);
                        array = binaryReader.ReadBytes(splitImageAllPixelByteLength);
                    }
                    else
                    {
                        fileSteam.Seek(bodyBytesStartIndex + pointerTemp, SeekOrigin.Begin);
                        array = binaryReader.ReadBytes(splitImageAllPixelByteLength);
                    }

                    var currentWidthPixel = array.Length / calUmPerPixelHeightPixelByteLength;
                    var size = new Size(currentWidthPixel, heightPixel);
                    var splitImageRawBytes = CalibrationAlgorithmService.ToRawBytes(array, size);
                    var (image, _) = CalibrationAlgorithmService.ToImageInfo(splitImageRawBytes);

                    var calUmPerPixelImageLeftPixel = pointerTemp / calUmPerPixelHeightPixelByteLength;
                    using var _ = image;
                    var originImageFilePath = Path.Combine(detectImageDirectory, Path.GetFileNameWithoutExtension(Cache.Item.TemplateImageFilePath), $"{index + 1}.jpg");
                    image.Save(originImageFilePath);
                    AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum.Ncc;
                    calibrationAlgorithmService.TryTemplateMatchToOffset(algorithmTemplateTypeEnum, image, templateId, out var result, out var _, out var score, out var _);

                    var point = new Point(currentWidthPixel - result.X + calUmPerPixelImageLeftPixel, result.Y); // 水平翻转后的坐标
                    matchPoint.Add(point);
                    matchOffsetPoint.Add(new Point(result.X, result.Y) - (Vector)size / 2);
                    Logger.LogHtmlInformation($"{index + 1}", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                    {
                        score,
                        LeftPixel = calUmPerPixelImageLeftPixel,
                        Point = point,
                        RawImageFile = new HtmlDownload(splitImageRawBytes, $"{index + 1}.raw"),
                        HorizontalFlipRawImageFile = new HtmlDownload(splitImageRawBytes, $"HorizontalFlip_{index + 1}.raw"),
                        HtmlTab = new HtmlTab(new
                        {
                            OriginImage = new HtmlImage(originImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(result, templateImageSize), new HtmlImageRectangleOverlay(result, templateImageSize)]),
                            TemplateImage = new HtmlImage(Cache.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    }), HtmlLogUniqueId.LoggingHtml());
                }

                var differences = matchPoint
                    .Zip(matchPoint.Skip(1), (prev, curr) => curr.X - prev.X)
                    .ToList();
                var error = differences.Max() - differences.Min();
                Logger.LogHtmlInformation("2.2. End Split", HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
                {
                    Point = new HtmlPlot2DLinesChart([(nameof(differences), differences.ToPoints())], nameof(differences)),
                    OffsetPointX = new HtmlPlot2DLinesChart([(nameof(matchOffsetPoint), matchOffsetPoint.Select(t => t.X).ToPoints())], nameof(differences))
                    //OffsetPointY = new HtmlPlot2DLinesChart([(nameof(matchOffsetPoint), matchOffsetPoint.Select(t => t.Y).ToPoints())], nameof(differences))
                }), HtmlLogUniqueId.LoggingHtml());
                return (true, matchPoint);
            }
            catch (Exception ex)
            {
                Logger.LogHtmlError(ex, "SplitImage Error", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                matchPoint.Clear();
                // 异常时也需保持有效状态
                return (false, matchPoint);
            }
        }
    }

    private bool Save(LaserXPixelSizeItemDto itemDto, CancellationToken cancellationToken, bool isSave = true) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        itemDto.MicroscopeLensInformation = Cache.MicroscopeLensInformation;

        Calibrations =
        [
            .. Calibrations
                .Where(t => (t.ProductivityInformation.OpticsMagType == itemDto.ProductivityInformation.OpticsMagType && t.ProductivityInformation.StageSpeedType == itemDto.ProductivityInformation.StageSpeedType) == false),
            itemDto.Clone()
        ];

        if (isSave == false) return;

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(() => DarkFieldCropImageList.Clear());
        SynchronizationContextProvider.Send(() => ResultLaserXPixelSizeItemList.Clear());
    }

    // 新增的Channel处理方法
    private async Task ProcessWithChannelAndCancellationAsync(
        List<(int k, long pointerTemp, int byteLength, int currentWidthPixel, long calUmPerPixelImageLeftPixel)> blockInfos,
        FileStream fileSteam,
        BinaryReader binaryReader,
        HTuple templateId,
        int calUmPerPixelHeightPixel,
        long calUmPerPixelBodyBytesStartIndex,
        ConcurrentBag<(int k, Point matchPoint, double score, long pointerTemp, int currentWidthPixel, long calUmPerPixelImageLeftPixel)> results,
        ConcurrentBag<Point> scoreList,
        ConcurrentBag<Point> scoreCalibrateList,
        CancellationToken cancellationToken = default)
    {
        // 定义Channel中传递的数据结构
        var channel = Channel.CreateBounded<(int k, long pointerTemp, int byteLength, int currentWidthPixel, long calUmPerPixelImageLeftPixel, byte[] imageData)>(
            new BoundedChannelOptions(Environment.ProcessorCount * 2)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });

        // 创建模板匹配锁
        var templateLock = new object();

        // 生产者任务 - 读取图像数据
        var producerTask = Task.Run(async () =>
        {
            try
            {
                foreach (var blockInfo in blockInfos)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var (k, pointerTemp, byteLength, currentWidthPixel, calUmPerPixelImageLeftPixel) = blockInfo;

                    byte[] array;
                    lock (fileSteam) // 文件读取需要加锁
                    {
                        fileSteam.Seek(calUmPerPixelBodyBytesStartIndex + pointerTemp, SeekOrigin.Begin);
                        array = binaryReader.ReadBytes(byteLength);
                    }

                    // 将数据发送到Channel
                    await channel.Writer.WriteAsync((k, pointerTemp, byteLength, currentWidthPixel, calUmPerPixelImageLeftPixel, array), cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("生产者任务被取消");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "生产者任务发生错误");
            }
            finally
            {
                channel.Writer.Complete();
            }
        });

        // 消费者任务 - 处理图像匹配
        var consumerTasks = new List<Task>();
        var consumerCount = Math.Max(1, Environment.ProcessorCount - 1); // 保留一个核心给其他任务

        for (int i = 0; i < consumerCount; i++)
        {
            var consumerTask = Task.Run(async () =>
            {
                try
                {
                    await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
                    {
                        try
                        {
                            await ProcessBlockDataAsync(
                                item.k,
                                item.pointerTemp,
                                item.byteLength,
                                item.currentWidthPixel,
                                item.calUmPerPixelImageLeftPixel,
                                item.imageData,
                                templateId,
                                calUmPerPixelHeightPixel,
                                results,
                                scoreList,
                                scoreCalibrateList,
                                templateLock,
                                cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            throw; // 重新抛出取消异常
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "处理块 {Index} 时发生错误", item.k);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Logger.LogInformation("消费者任务被取消");
                }
                catch (ChannelClosedException)
                {
                    // Channel正常关闭，不是错误
                }
            });

            consumerTasks.Add(consumerTask);
        }

        // 等待所有任务完成
        try
        {
            await Task.WhenAll(consumerTasks);
            await producerTask;
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("图像处理流程被取消");
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "图像处理流程发生错误");
            throw;
        }
    }

    // 处理单个图像块的异步方法   
    private async Task ProcessBlockDataAsync(
        int k,
        long pointerTemp,
        int byteLength,
        int currentWidthPixel,
        long calUmPerPixelImageLeftPixel,
        byte[] imageData,
        HTuple templateId,
        int calUmPerPixelHeightPixel,
        ConcurrentBag<(int k, Point matchPoint, double score, long pointerTemp, int currentWidthPixel, long calUmPerPixelImageLeftPixel)> results,
        ConcurrentBag<Point> scoreList,
        ConcurrentBag<Point> scoreCalibrateList,
        object templateLock,
        CancellationToken cancellationToken)
    {
        // 立即让出控制权，避免阻塞消费者线程
        await Task.Yield();

        cancellationToken.ThrowIfCancellationRequested();
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum.Ncc;
        // 处理图像数据
        var calUmPerPixelSplitImageRawBytes = CalibrationAlgorithmService.ToRawBytes(
            imageData,
            new Size(currentWidthPixel, calUmPerPixelHeightPixel));

        var (image, _) = CalibrationAlgorithmService.ToImageInfo(calUmPerPixelSplitImageRawBytes);

        using (image)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 使用锁保护模板匹配操作
            Point result;
            double score;
            lock (templateLock)
            {
                cancellationToken.ThrowIfCancellationRequested();
                //HalconHelper.TryNccTemplateMathToOffset(image, templateId, out result, out score, out var _);
                calibrationAlgorithmService.TryTemplateMatchToOffset(algorithmTemplateTypeEnum, image, templateId, out result, out var _, out score, out _);
            }

            var point = new Point(currentWidthPixel - result.X + calUmPerPixelImageLeftPixel, result.Y);
            var pointScore = new Point(calUmPerPixelImageLeftPixel * 1.0, score);

            results.Add((k, point, score, pointerTemp, currentWidthPixel, calUmPerPixelImageLeftPixel));
            scoreList.Add(pointScore);
            scoreCalibrateList.Add(new Point(currentWidthPixel - result.X + calUmPerPixelImageLeftPixel * 1.0, score));

            // 可选：记录处理日志
            if (k % 10 == 0) // 每10个块记录一次，避免日志过多
            {
                Logger.LogDebug("已处理块 {BlockIndex}, 分数: {Score}", k, score);
            }
        }
    }

    #endregion 校准

    #region 自动化校准

    public override void GetAutoCalibrationStep()
    {
        _enableProductiveInformationList.Clear();
        var autoCalibrationStepList = new ObservableCollection<CalibrationItemStep>();
        WindowManagerService.ShowDialog(enableProductiveInformationWindowViewModel);
        _enableProductiveInformationList = [.. enableProductiveInformationWindowViewModel.ProductiveInformationEnableList.Select(t => (t.ProductivityInformation, t.IsEnable))];
        if (_enableProductiveInformationList.Count == 0) return;
        autoCalibrationStepList.Add(new() { StepName = "Loading" });
        foreach (var productivity in _enableProductiveInformationList)
        {
            var calibrationItemStep = new CalibrationItemStep
            {
                StepName = productivity.ToString()
            };
            autoCalibrationStepList.Add(calibrationItemStep);
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

            foreach (var productivity in _enableProductiveInformationList)
            {
                CalibrationStepIndex = 4;
                Cache.ProductivityInformation = productivity.productiveInformation;
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
        if (CalibrationRecipeService.GetLaserReticleMaskMachineInfo(Cache.WaferMaskTypeEnum, Cache.MicroscopeLensInformation, Cache.ProductivityInformation, out var brightFieldMaskInfo) == false)
            return false;
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(originReticle, brightFieldMaskInfo, out var brightFieldMaskPosition);
        Cache.Item.FindTemplatePosition = brightFieldMaskPosition;
        Cache.FindStartPosition = new Point(Cache.Item.FindTemplatePosition.X - Cache.DieWidthUm * Cache.ColumnNumber, Cache.Item.FindTemplatePosition.Y);
        Cache.FindEndPosition = new Point(Cache.Item.FindTemplatePosition.X + Cache.DieWidthUm * Cache.ColumnNumber, Cache.Item.FindTemplatePosition.Y);
        Cache.SplitImageCount = Cache.ColumnNumber * 2;

        // Dark Field
        if (CalibrationRecipeService.GetLaserReticleMaskMachineInfo(Cache.WaferMaskTypeEnum, null, Cache.ProductivityInformation, out var darkFieldMaskInfo) == false)
            return false;
        Cache.Item.TemplateFilePath = darkFieldMaskInfo.RecipeDarkFieldTemplateDto.TemplateFilePath;
        Cache.Item.TemplateImageFilePath = darkFieldMaskInfo.RecipeDarkFieldTemplateDto.TemplateImageFilePath;
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
            if (_enableProductiveInformationList.Count == 0) return false;
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
                    foreach (var productivity in _enableProductiveInformationList)
                    {
                        var itemReview = ReviewList.FirstOrDefault(t => t.ProductivityInformation == productivity.productiveInformation);
                        if (itemReview is not null)
                        {
                            Cache.ProductivityInformation = productivity.productiveInformation;
                            if (await AutomationRecipeInformationAsync() == false) return false;
                            if (await GetTemplateImagePathAsync() == false) return false;
                            itemReview.FindStartPosition = Cache.FindStartPosition;
                            itemReview.FindEndPosition = Cache.FindEndPosition;
                            itemReview.FilePath = Cache.Item.TemplateFilePath;
                            itemReview.FileTemplatePath = Cache.Item.TemplateImageFilePath;
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