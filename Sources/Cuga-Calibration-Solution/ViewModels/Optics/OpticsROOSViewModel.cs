using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.CIB;
using Core.Models.Enums.HardwareType;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.CIB.YPixelSize;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Optics.ROOS;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Hosting;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Optics;

[IOCAppService(ServiceType = typeof(OpticsROOSViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsROOSViewModel : CalibrationViewModelBase<OpticsROOSCache>
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select Productivity" },
        new() { StepName = "Image Param" },
        new() { StepName = "Find Position" },
        new() { StepName = "ROOS" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial OpticsROOSDTO CalibratingItem { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<ProductivityInformationStatus> CalibratingStatuses { get; set; } = [];

    #endregion Calibrate

    [ObservableProperty]
    public partial IReadOnlyList<OpticsROOSDTO> Reviews { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<OpticsROOSDTO> SelectedReviews { get; set; } = [];

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial OpticsROOSCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial OpticsROOSDTO[] Calibrations { get; set; } = [];

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    [ObservableProperty]
    public partial CIBYPixelSizeDTO[] CIBYPixelSizes { get; set; } = [];

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (ApplicationCookie.HardwareStateConfig.MotorHardwares[HardwareMotorTypeEnum.ROOS].Enabled == false)
        {
            DialogWindowProvider.ShowDialog("Please enable the ROOS motor!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);
        CIBYPixelSizes = ApplicationCookieService.GetCalibrations<CIBYPixelSizeDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<OpticsROOSCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<OpticsROOSDTO>(cancellationToken);

        UpdateEntryStatus(Unsafe.As<CalibrationDTOBase[]>(Calibrations), cancellationToken);

        return true;
    }


    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Reviews =
        [
            .. Calibrations
                .OrderBy(t => t.ProductivityInformation)
        ];

        return Reviews.Count > 0;
    }

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
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
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition));

                return true;

            default:
                return false;
        }
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:

                return true;

            case 1:
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken);

                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.Item.FindBFMachinePosition == Point.Origin
                        ? MicroscopeCalChip.GetBFMachinePosition(Cache.CalChipSiteModelEnum)
                        : Cache.Item.FindBFMachinePosition), Cache.CalChipSiteModelEnum);

                return true;

            case 2:

            case 3:
                return true;

            default:
                return false;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.ProductivityInformations.Contains(Cache.ProductivityInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            (Cache.Item.ConfigImageYPixelHeight, Cache.Item.ConfigCropImageStartYPixel, Cache.Item.ConfigCropImageEndYPixel) = ConfigureViewModel.GetDefaultImageYPixelHeight(Cache.ProductivityInformation);

            Cache.Item.CurrentConfigROOSPos = OpticsViewModel.GetROOSMotorAbsoluteValue();
            (Cache.Item.StartROOSPos, Cache.Item.StopROOSPos, Cache.Item.StepROOSPos) = OpticsViewModel.GetROOSMotorRouteRange();
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation,
                Cache.ProductivityInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBInformation,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                Cache.Item.CurrentConfigROOSPos,
                Cache.Item.ConfigImageYPixelHeight,
                Cache.Item.ConfigCropImageStartYPixel,
                Cache.Item.ConfigCropImageEndYPixel,
                ROOSLimitMinPos = Cache.Item.StartROOSPos,
                ROOSLimitMaxPos = Cache.Item.StopROOSPos,
                ROOSMotorAccuracy = Cache.Item.StepROOSPos
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.MicroscopeLensInformation)
                   && ApplicationCookie.ProductivityInformations.Contains(Cache.ProductivityInformation)
                   && ApplicationCookie.LaserLightInformations.Contains(Cache.Item.LaserLightInformation)
                   && ApplicationCookie.CIBInformations.Contains(Cache.Item.CIBInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Cache.Item.FindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Item.FindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var detectImageDirectory = ImageFileDirectory;

            var (limitMinPos, limitMaxPos, motorAccuracy) = OpticsViewModel.GetROOSMotorRouteRange();
            Guard.IsInRange(Cache.Item.StartROOSPos, limitMinPos, limitMaxPos);
            Guard.IsInRange(Cache.Item.StopROOSPos, limitMinPos, limitMaxPos);
            Guard.IsGreaterThanOrEqualTo(Cache.Item.StepROOSPos, motorAccuracy);
            Guard.IsGreaterThan(Cache.Item.StartROOSPos, Cache.Item.StopROOSPos);

            var yPixelSize = GuardExtensions.IsNotNullAndReturn(
                CIBYPixelSizes.SingleOrDefault(t => t.ProductivityInformation == Cache.ProductivityInformation && t.PmtId == Cache.Item.CIBInformation.PMTId)?.YPixelSize,
                $"CIB {Cache.Item.CIBInformation} y pixel size calibration result is empty!");

            Cache.Item.IdealImageYPixelHeight = Cache.Item.IdealImageYUmHeight / yPixelSize;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation,
                Cache.ProductivityInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBInformation,
                Cache.Item.FindBFMachinePosition,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                ImageConfig = new HtmlQuote(new
                {
                    Cache.Item.CIBInformation,
                    Cache.Item.ImageWidth,
                    yPixelSize,
                    Cache.Item.IdealImageYUmHeight,
                    Cache.Item.IdealImageYPixelHeight,
                    Cache.Item.ConfigImageYPixelHeight,
                    Cache.Item.ConfigCropImageStartYPixel,
                    Cache.Item.ConfigCropImageEndYPixel
                }),
                ROOSMotorParam = new HtmlQuote(new
                {
                    Cache.Item.CurrentConfigROOSPos,
                    Cache.Item.StartROOSPos,
                    Cache.Item.StopROOSPos,
                    Cache.Item.StepROOSPos
                }),
                Cache.Item.ROOSAlignOffsetThreshold,
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem = new OpticsROOSDTO
            {
                ProductivityInformation = Cache.ProductivityInformation
            };

            try
            {
                #region 硬件安装对中验证

                var initialOpticsROOSDTOItem = new OpticsROOSDTOItem { ROOSPos = (limitMinPos + limitMaxPos) / 2d };

                await GetImageCropYPixelPositionByCurrentROOSAsync(initialOpticsROOSDTOItem, false, cancellationToken);

                var roosCenterYPixelPos = (initialOpticsROOSDTOItem.EndImageYPixel + initialOpticsROOSDTOItem.StartImageYPixel) / 2d;
                var alignOffsetPix = roosCenterYPixelPos - Cache.Item.IdealImageYPixelHeight / 2d;
                var alignOffsetUm = alignOffsetPix * yPixelSize;

                Logger.LogHtmlInformation("Align Verify", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    roosCenterYPixelPos,
                    alignOffsetPix,
                    alignOffsetUm
                }), HtmlLogUniqueId.LoggingHtml());

                if (Math.Abs(alignOffsetUm) > Cache.Item.ROOSAlignOffsetThreshold && HostEnvironment.IsDevelopment() == false)
                {
                    Logger.LogHtmlError("ROOS center offset is out of threshold!", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                #endregion

                Logger.LogHtmlInformation("Calibration", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var roosMotorAbsoluteValues = Generate.LinearRange(Cache.Item.StartROOSPos, Cache.Item.StepROOSPos, Cache.Item.StopROOSPos);
                foreach (var roosMotorAbsoluteValue in roosMotorAbsoluteValues)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var opticsROOSDTOItem = new OpticsROOSDTOItem { ROOSPos = roosMotorAbsoluteValue };

                    await GetImageCropYPixelPositionByCurrentROOSAsync(opticsROOSDTOItem, false, cancellationToken);

                    CalibratingItem.Items = [.. CalibratingItem.Items, opticsROOSDTOItem];
                }


                var illegalPoints = CalibratingItem.Items.Where(t => t.StartImageYPixel == 0 || Math.Abs(t.EndImageYPixel - Cache.Item.ConfigImageYPixelHeight) < Constants.Tolerance).ToList();
                CalibratingItem.IllegalPoints = [.. illegalPoints.Select(t => new Point(t.ROOSPos, t.CropImageYPixelHeight))];

                if (illegalPoints.Count != 0 && HostEnvironment.IsDevelopment() == false)
                {
                    Logger.LogHtmlError("Calibration Failed:ROOS is not aligned!", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        ScatterPlotControl = new HtmlContainer([.. CalibratingItem.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])
                    }), HtmlLogUniqueId.LoggingHtml());
                }

                Point[] fitPoints = [..CalibratingItem.Items
                    .Where(t => t.StartImageYPixel > 0 && t.EndImageYPixel < Cache.Item.ConfigImageYPixelHeight)
                    .Select(tt => new Point(tt.ROOSPos, tt.CropImageYPixelHeight))];

                var (slope, intercept, rSquared, yPredicted) = PolynomialCurve.Fit1(
                    Vector<double>.Build.DenseOfEnumerable(fitPoints.Select(t => t.X)),
                    Vector<double>.Build.DenseOfEnumerable(fitPoints.Select(t => t.Y)));

                CalibratingItem.Slope = slope;
                CalibratingItem.Intercept = intercept;
                CalibratingItem.RSquared = rSquared;
                CalibratingItem.FitPoints = [.. fitPoints.Index().Select(t => new Point(t.Item.X, yPredicted[t.Index]))];

                var resultOpticsROOSDTOItem = new OpticsROOSDTOItem
                {
                    ROOSPos = (Cache.Item.IdealImageYPixelHeight - intercept) / slope
                };
                await GetImageCropYPixelPositionByCurrentROOSAsync(resultOpticsROOSDTOItem, false, cancellationToken);

                CalibratingItem.ResultDTOItem = resultOpticsROOSDTOItem.Clone();
                CalibratingItem.ResultDTOItem.ConvertYPixelToNearestEvenPrecise(); // 取偶数
                var result = HostEnvironment.IsDevelopment() || resultOpticsROOSDTOItem.ROOSPos >= Cache.Item.StartROOSPos && resultOpticsROOSDTOItem.ROOSPos <= Cache.Item.StopROOSPos;
                CalibratingItem.IsCalibrated = result;

                Guard.IsTrue(Save([CalibratingItem], cancellationToken));

                Logger.LogHtmlInformation($"Calibration {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    CalibratingItem.Slope,
                    CalibratingItem.Intercept,
                    CalibratingItem.RSquared,
                    alignOffsetPix,
                    CalibratingItem.ResultDTOItem.ROOSPos,
                    CalibratingItem.ResultDTOItem.StartImageYPixel,
                    CalibratingItem.ResultDTOItem.EndImageYPixel,
                    CalibratingItem.ResultDTOItem.CropImageYPixelHeight,
                    CornerPoints = CalibratingItem.IllegalPoints,
                    ScatterPlotControl = new HtmlContainer([.. CalibratingItem.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])
                }), HtmlLogUniqueId.LoggingHtml());

                return result;
            }
            finally
            {
                OpticsViewModel.SetROOSMotorAbsoluteValue(Cache.Item.CurrentConfigROOSPos);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition), Cache.CalChipSiteModelEnum);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        if (SelectedReviews.Count == 0)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(async () =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.VerifyThreshold
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlInformation("Details", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            var errorMessageStringBuilder = new StringBuilder();

            foreach (var selectedReviewItem in SelectedReviews)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Cache.ProductivityInformation = selectedReviewItem.ProductivityInformation;

                    var opticsROOSDTOItem = selectedReviewItem.ResultDTOItem.Clone();
                    await GetImageCropYPixelPositionByCurrentROOSAsync(opticsROOSDTOItem, true, cancellationToken);

                    var imageCropYPixelOffset = Cache.Item.IdealImageYPixelHeight - opticsROOSDTOItem.CropImageYPixelHeight;

                    var verifyResult = Math.Abs(imageCropYPixelOffset) <= Cache.VerifyThreshold;

                    selectedReviewItem.IsVerified = HostEnvironment.IsDevelopment() || verifyResult;
                    selectedReviewItem.ResultDTOItem.FilePath = opticsROOSDTOItem.FilePath;
                    selectedReviewItem.ResultDTOItem.DrawFilePath = opticsROOSDTOItem.DrawFilePath;

                    var htmlBullet = new HtmlBullet(new
                    {
                        Cache.Item.IdealImageYPixelHeight,
                        calibrationDetails = new HtmlQuote(new
                        {
                            selectedReviewItem.ResultDTOItem.ROOSPos,
                            selectedReviewItem.ResultDTOItem.StartImageYPixel,
                            selectedReviewItem.ResultDTOItem.EndImageYPixel,
                            selectedReviewItem.ResultDTOItem.CropImageYPixelHeight
                        }),
                        VerifyResult = new HtmlQuote(new
                        {
                            opticsROOSDTOItem.ROOSPos,
                            opticsROOSDTOItem.StartImageYPixel,
                            opticsROOSDTOItem.EndImageYPixel,
                            opticsROOSDTOItem.CropImageYPixelHeight,
                        }),
                    });

                    if (selectedReviewItem.IsOk)
                        Logger.LogHtmlInformation($"OK: {selectedReviewItem.ProductivityInformation}", HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                    else
                    {
                        errorMessageStringBuilder.AppendLine($"{selectedReviewItem.ProductivityInformation}: {selectedReviewItem.IsOk}");
                        Logger.LogHtmlError($"Error: {selectedReviewItem.ProductivityInformation},ImageCropYPixelOffset: {imageCropYPixelOffset}", HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                    }

                    Logger.LogHtmlInformation("Applied Result:", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                    {
                        opticsROOSDTOItem.ROOSPos,
                        opticsROOSDTOItem.StartImageYPixel,
                        opticsROOSDTOItem.EndImageYPixel,
                        opticsROOSDTOItem.CropImageYPixelHeight,
                    }), HtmlLogUniqueId.LoggingHtml());
                }
                finally
                {
                    OpticsViewModel.SetROOSMotorAbsoluteValue(Cache.Item.CurrentConfigROOSPos);
                }
            }

            Guard.IsTrue(Save(SelectedReviews, cancellationToken));

            var result = SelectedReviews.All(t => t.IsOk);

            DialogWindowProvider.ShowDialog($"""
                                             Verify : {(result ? "OK" : "Failed")}
                                             {errorMessageStringBuilder}
                                             """,
                DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition), Cache.CalChipSiteModelEnum);

            return result;
        }).ConfigureAwait(false);
    }

    private bool Save(IReadOnlyList<OpticsROOSDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                .. Calibrations.Where(t => t.ProductivityInformation != dto.ProductivityInformation),
                dto.Clone()
            ];
        }

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var temps = Guard.IsAssignableToTypeAndReturn<OpticsROOSDTO[]>(calibrations);
        var status = Entry.Status;

        CalibratingStatuses =
        [
            .. ApplicationCookie.ProductivityInformations.Select(t => new ProductivityInformationStatus { SelectedItem = t, IsCalibrated = false })
        ];

        Calibrations =
        [
            .. temps
                .Where(t => ApplicationCookie.ProductivityInformations.Contains(t.ProductivityInformation))
                .DistinctBy(t => t.ProductivityInformation)
                .Select(t =>
                {
                    CalibratingStatuses.Single(tt => tt.SelectedItem == t.ProductivityInformation).IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        status.TotalCalibrationCount = ApplicationCookie.ProductivityInformations.Count;
        status.CalibratedCount = Calibrations.Count(t => t.IsCalibrated);
        status.VerifiedCount = Calibrations.Count(t => t.IsVerified);
        status.Details =
        [
            .. ApplicationCookie.ProductivityInformations.Select(productivityInformation =>
            {
                var item = Calibrations.SingleOrDefault(t => t.ProductivityInformation == productivityInformation);

                return new CalibrationViewModelStatus.Detail(
                    productivityInformation.ToString(),
                    item?.IsCalibrated,
                    item?.IsVerified);
            })
        ];
    }

    #endregion 校准

    #region 算法

    private async Task GetImageCropYPixelPositionByCurrentROOSAsync(OpticsROOSDTOItem opticsROOSDTOItem, bool isReview, CancellationToken cancellationToken)
    {
        OpticsViewModel.SetROOSMotorAbsoluteValue(opticsROOSDTOItem.ROOSPos);

        using var darkFieldImageDto = HostEnvironment.IsDevelopment()
            ? GetMockImages(opticsROOSDTOItem.ROOSPos)
            : await CIBViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Dark,
                StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition),
                Cache.Item.ImageWidth,
                Cache.Item.CIBInformation,
                (false, Cache.CalChipSiteModelEnum),
                (false, Cache.Item.OpticsConfiguration),
                (false, Cache.Item.CIBConfiguration),
                (false, Cache.Item.LaserLightInformation),
                false,
                cancellationToken,
                customImageHeight: isReview ? (opticsROOSDTOItem.StartImageYPixel, opticsROOSDTOItem.EndImageYPixel) : null);

        var imageFileDirectory = Path.Combine(ImageFileDirectory, Cache.ProductivityInformation.ToString());
        var fileName = $"ROOS_POS_({opticsROOSDTOItem.ROOSPos:f3})_Guid({HtmlLogUniqueId}).jpg";

        opticsROOSDTOItem.FilePath = Path.Combine(imageFileDirectory, fileName);
        opticsROOSDTOItem.OriginFilePath = darkFieldImageDto.RawImageFilePath;
        darkFieldImageDto.Image.SaveImage(opticsROOSDTOItem.FilePath);

        (opticsROOSDTOItem.StartImageYPixel, opticsROOSDTOItem.EndImageYPixel) = CalibrationAlgorithmService.GetOpticsROOSResult(darkFieldImageDto.Image, out var drawImageObject);

        opticsROOSDTOItem.DrawFilePath = Path.Combine(imageFileDirectory, "Draw", fileName);
        drawImageObject.Save(opticsROOSDTOItem.DrawFilePath);

        Logger.LogHtmlInformation($"ROOS Pos: {opticsROOSDTOItem.ROOSPos:0.###} mm", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
        {
            darkFieldImageDto.CIBInformation,
            darkFieldImageDto.Size,
            opticsROOSDTOItem.OriginFilePath,
            opticsROOSDTOItem.ROOSPos,
            opticsROOSDTOItem.StartImageYPixel,
            opticsROOSDTOItem.EndImageYPixel,
            opticsROOSDTOItem.CropImageYPixelHeight,
            RawFilePath = opticsROOSDTOItem.OriginFilePath,
            HtmlTab = new HtmlTab(new
            {
                DrawImage = new HtmlImage(opticsROOSDTOItem.DrawFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                OriginImage = new HtmlImage(opticsROOSDTOItem.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
            }),
        }), HtmlLogUniqueId.LoggingHtml());
    }

    private DarkFieldImageDTO GetMockImages(double pos)
    {
        //mock
        var mockImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"Assets\Data\ROOS\Pos_{pos}.raw");

        using var hImage = RAWImageFactory.CreateImage(mockImagePath, false);

        var image = new DarkFieldImageDTO().AdaptIn(new DarkFieldRawScanImageDTO { CIBInformation = Cache.Item.CIBInformation, Size = hImage.GetSize(), IsForward = true, RawImageCIBProfileModeEnum = CIBProfileModeEnum.PMTLog, RawImageFilePath = mockImagePath, IsKeepRawImageCIBProfileModeEnum = true });

        return image;
    }

    #endregion
}