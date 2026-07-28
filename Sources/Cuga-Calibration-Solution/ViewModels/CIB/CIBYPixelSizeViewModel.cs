using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.CIB.YPixelSize;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace CugaCalibration.ViewModels.CIB;

[IOCAppService(ServiceType = typeof(CIBYPixelSizeViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBYPixelSizeViewModel : CalibrationViewModelBase<CIBYPixelSizeCache>
{
    #region 属性

    public override string CalibrateDirectoryName => $"{Cache.ProductivityInformation.ToString()}";

    public override string CalibrateFileName => $"{Cache.ProductivityInformation.ToString()}";

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "Image Param" },
        new() { StepName = "Alignment" },
        new() { StepName = "Find Position" },
        new() { StepName = "Y Pixel Size" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial IReadOnlyList<CIBYPixelSizeDTO> CalibratingItems { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<ProductivityInformationStatus> CalibratingStatuses { get; set; } = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    public partial IReadOnlyList<CIBYPixelSizeDTO> Reviews { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<CIBYPixelSizeDTO> SelectedReviewItems { get; set; } = [];

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial CIBYPixelSizeCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial CIBYPixelSizeDTO[] Calibrations { get; set; } = [];

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCalChipCache MicroscopeCalChipCache { get; set; } = new();

    [ObservableProperty]
    public partial AlignmentUserControlViewModel AlignmentUserControlViewModel { get; set; } = HostApplication.GetRequiredService<AlignmentUserControlViewModel>();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);


        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);
        MicroscopeCalChipCache = ApplicationCookieService.GetCache<MicroscopeCalChipCache>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<CIBYPixelSizeCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<CIBYPixelSizeDTO>(cancellationToken);

        UpdateEntryStatus(Unsafe.As<CalibrationDTOBase[]>(Calibrations), cancellationToken);

        Cache.PmtInterval = CalibrationSetting.SettingCommonParam.PMTInterval;

        return true;
    }

    protected override async Task<bool> CancelingAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);

        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Reviews =
        [
            .. Calibrations
                .OrderBy(t => t.ProductivityInformation)
                .ThenBy(t => t.PmtId)
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
                return true;

            case 4:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition), Cache.CalChipSiteModelEnum);

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
            case 1:
                return true;

            case 2:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.CalChipSiteModelEnum switch
                    {
                        CalChipSiteModelEnum.ChuckModel => StageViewModel.BrightFieldToMachinePosition(Cache.Item.FindBFMachinePosition),
                        CalChipSiteModelEnum.DswModel => MicroscopeCalChip.DSWBrightFieldMachineAffinePosition,
                        _ => ThrowHelper.ThrowNotSupportedException<Point>("Current CalChip Mode Is Not Supported!")
                    }), Cache.CalChipSiteModelEnum);

                return true;

            case 3:
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition), Cache.CalChipSiteModelEnum);

                return true;

            case 4:
                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

                return true;

            default:
                return true;
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
                Cache.CalChipSiteModelEnum
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.ProductivityInformations.Contains(Cache.ProductivityInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBChannelId,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.Item.MicroscopeLensInformation)
                   && ApplicationCookie.LaserLightInformations.Contains(Cache.Item.LaserLightInformation)
                   && ApplicationCookie.CIBInformationChannelIds.Contains(Cache.Item.CIBChannelId);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step2Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            AlignmentUserControlViewModel.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum;
            AlignmentUserControlViewModel.ProductivityInformation = Cache.ProductivityInformation;

            DialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment ?",
                out var dialogResult,
                DialogButtonsEnum.YesNo,
                DialogIconEnum.Question);

            AlignmentUserControlViewModel.IsDarkFieldAlignment = Cache.Item.IsDarkFieldAlignment = dialogResult == DialogResultEnum.Yes;

            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

            var alignmentResult = AlignmentUserControlViewModel.AlignmentResult;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.CalChipSiteModelEnum,
                AlignmentUserControlViewModel.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(alignmentResult.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Guard.IsEqualTo(Cache.Item.MicroscopeLensInformation, MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            Cache.Item.FindBFMachinePosition = StageViewModel.GetMachineStagePosition();
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Item.FindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step4Async(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(async () =>
        {
            var detectImageDirectory = ImageFileDirectory;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBChannelId,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.ImageWidth,
                Cache.AlgorithmTemplateTypeEnum,
                Cache.AlgorithmTemplateSizeEnum,
                Cache.Item.FindBFMachinePosition,
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            var (_, yDirection) = StageViewModel.GetMachineDirection();
            CalibratingItems = ApplicationCookie.CIBInformationPMTIds
                .OrderBy(t => t)
                .Select(t =>
                    new CIBYPixelSizeDTO
                    {
                        ProductivityInformation = Cache.ProductivityInformation,
                        PmtId = t,
                        FindBFMachinePosition = Cache.Item.FindBFMachinePosition + (Vector)new Point(0, yDirection * (t - CalibrationConstantsHelper.MainPmtId) * Cache.PmtInterval),
                        FilePath = detectImageDirectory,
                        RawFilePath = detectImageDirectory
                    }).ToList();

            Guard.IsNotEmpty(CalibratingItems);

            Logger.LogHtmlInformation("Get Y Pixel Size", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

            foreach (var item in CalibratingItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await GetYPixelSizeAsync(item, cancellationToken);
                item.IsCalibrated = true;
            }

            Guard.IsTrue(Save(CalibratingItems, cancellationToken));

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                PmtYPixelSize = new HtmlPlot2DLinesChart(
                    [
                        ("PMT Y Pixel Size(Y:um,X:PMT ID)", [.. CalibratingItems.OrderBy(t => t.PmtId).Select(t => new Point(t.PmtId, t.YPixelSize))])
                    ],
                    "PMT Y Pixel Size")
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(async () =>
        {
            if (SelectedReviewItems.Count == 0)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var productiveGroups = SelectedReviewItems.GroupBy(t => t.ProductivityInformation).ToList();
            if (productiveGroups.Count > 1)
            {
                DialogWindowProvider.ShowDialog("Please select same optics mag items!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            Cache.ProductivityInformation = productiveGroups.First().Key;

            AlignmentUserControlViewModel.IsDarkFieldAlignment = Cache.Item.IsDarkFieldAlignment;
            AlignmentUserControlViewModel.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum;
            AlignmentUserControlViewModel.ProductivityInformation = Cache.ProductivityInformation;
            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

            var errorMessageStringBuilder = new StringBuilder();

            foreach (var selectedReviewItem in SelectedReviewItems.OrderBy(t => t.PmtId))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var title = selectedReviewItem.ProductivityInformation.ToString();

                if (selectedReviewItem.IsCalibrated == false)
                {
                    errorMessageStringBuilder.AppendLine($"{title}: Error");
                    continue;
                }

                Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var detectImageDirectory = ImageFileDirectory;

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    Cache.ProductivityInformation,
                    Cache.Item.MicroscopeLensInformation,
                    Cache.Item.LaserLightInformation,
                    OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                    CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                    Cache.Item.CIBChannelId,
                    Cache.Item.ImageWidth,
                    Cache.AlgorithmTemplateTypeEnum,
                    Cache.AlgorithmTemplateSizeEnum,
                    Cache.Item.FindBFMachinePosition,
                    detectImageDirectory,
                    Cache.Threshold
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("Verify", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    selectedReviewItem.YPixelSize,
                    selectedReviewItem.FindBFMachinePosition
                }), HtmlLogUniqueId.LoggingHtml());

                var verifyItem = selectedReviewItem.Clone();
                await GetYPixelSizeAsync(verifyItem, cancellationToken);

                var error = selectedReviewItem.YPixelSize - verifyItem.YPixelSize;
                var isOk = Math.Abs(error) < Cache.Threshold;

                var htmlQuote = new HtmlQuote(new
                {
                    Cache.Threshold,
                    CalibrationPixelSize = selectedReviewItem.YPixelSize,
                    VerifyPixelSize = verifyItem.YPixelSize,
                    ErrorPixelSize = error
                });

                if (isOk)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlQuote, HtmlLogUniqueId.LoggingHtml());
                else
                {
                    errorMessageStringBuilder.AppendLine($"{title}: Error");
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlQuote, HtmlLogUniqueId.LoggingHtml());
                }

                if (isOk) selectedReviewItem.YPixelSize = verifyItem.YPixelSize;
                selectedReviewItem.IsVerified = isOk;
            }

            Guard.IsTrue(Save(SelectedReviewItems, cancellationToken));

            var result = SelectedReviewItems.All(t => t.IsOk);

            DialogWindowProvider.ShowDialog($"""
                                             Verify : {(result ? "OK" : "Failed")}
                                             {errorMessageStringBuilder}
                                             """,
                DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        }).ConfigureAwait(false);
    }

    private async Task GetYPixelSizeAsync(CIBYPixelSizeDTO cibYPixelSizeDTO, CancellationToken cancellationToken)
    {
        using var darkFieldImage = await CIBViewModel.GetPMTImageAsync(
            Cache.ProductivityInformation,
            StageCoordinateSystemEnum.Dark,
            StageViewModel.MachineToBrightFieldPosition(cibYPixelSizeDTO.FindBFMachinePosition),
            Cache.Item.ImageWidth,
            ApplicationCookie.CIBInformations.Single(t => t.PMTId == cibYPixelSizeDTO.PmtId && t.ChannelId == Cache.Item.CIBChannelId),
            (false, Cache.CalChipSiteModelEnum),
            (false, Cache.Item.OpticsConfiguration),
            (false, Cache.Item.CIBConfiguration),
            (false, Cache.Item.LaserLightInformation),
            false,
            cancellationToken);

        var yPixelSize = CalibrationAlgorithmService.GetYPixelSize(darkFieldImage.Image, AlgorithmStandardMaskSquareSizeEnum.Size10.ToSize().Height, out var drawImageObj);
        cibYPixelSizeDTO.YPixelSize = yPixelSize;

        cibYPixelSizeDTO.FilePath = Path.Combine(ImageFileDirectory, $"PMTId({cibYPixelSizeDTO.PmtId})_YPixelSize({cibYPixelSizeDTO.YPixelSize:f3})_Guid({HtmlLogUniqueId}).jpg");
        cibYPixelSizeDTO.DrawImageFilePath = Path.Combine(ImageFileDirectory, $"PMTId({cibYPixelSizeDTO.PmtId})_YPixelSize({cibYPixelSizeDTO.YPixelSize:f3})_DrawImage_Guid({HtmlLogUniqueId}).jpg");
        cibYPixelSizeDTO.RawFilePath = darkFieldImage.RawImageFilePath;

        using var _ = drawImageObj;
        darkFieldImage.Image.SaveImage(cibYPixelSizeDTO.FilePath);
        drawImageObj.SaveImage(cibYPixelSizeDTO.DrawImageFilePath);

        Logger.LogHtmlInformation($"Get Y Pixel Size Success:PMT ID {cibYPixelSizeDTO.PmtId}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
        {
            cibYPixelSizeDTO.PmtId,
            cibYPixelSizeDTO.ProductivityInformation,
            cibYPixelSizeDTO.FindBFMachinePosition,
            cibYPixelSizeDTO.YPixelSize,
            OriginFilePath = cibYPixelSizeDTO.RawFilePath,
            HtmlTab = new HtmlTab(new
            {
                DrawImage = new HtmlImage(cibYPixelSizeDTO.DrawImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(false)]),
                Image = new HtmlImage(cibYPixelSizeDTO.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(false)])
            })
        }), HtmlLogUniqueId.LoggingHtml());
    }

    private bool Save(IReadOnlyList<CIBYPixelSizeDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                dto,
                .. Calibrations
                    .Where(t => (t.ProductivityInformation == dto.ProductivityInformation && t.PmtId == dto.PmtId) == false)
            ];
        }

        ApplicationCookieService.SetCalibrations(Calibrations, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var temps = Guard.IsAssignableToTypeAndReturn<CIBYPixelSizeDTO[]>(calibrations);
        var status = Entry.Status;

        CalibratingStatuses =
        [
            .. ApplicationCookie.OpticsMagTypeProductivityInformations.Select(t => new ProductivityInformationStatus { SelectedItem = t, IsCalibrated = false })
        ];

        Calibrations =
        [
            .. temps
                .Where(t => ApplicationCookie.OpticsMagTypeProductivityInformations.Contains(t.ProductivityInformation)
                            && ApplicationCookie.CIBInformationPMTIds.Contains(t.PmtId))
                .DistinctBy(t => (t.ProductivityInformation, t.PmtId))
        ];

        foreach (var calibratingStatus in CalibratingStatuses) calibratingStatus.IsCalibrated = Calibrations.Count(t => t.ProductivityInformation == calibratingStatus.SelectedItem && t.IsCalibrated) == ApplicationCookie.CIBInformationPMTIds.Count;

        status.TotalCalibrationCount = ApplicationCookie.OpticsMagTypeProductivityInformations.Count * ApplicationCookie.CIBInformationPMTIds.Count;
        status.CalibratedCount = Calibrations.Count(t => t.IsCalibrated);
        status.VerifiedCount = Calibrations.Count(t => t.IsVerified);
        status.Details =
        [
            .. ApplicationCookie.OpticsMagTypeProductivityInformations.SelectMany(productivityInformation =>
                ApplicationCookie.CIBInformationPMTIds.Select(pmtId =>
                {
                    var item = Calibrations.SingleOrDefault(t => t.ProductivityInformation == productivityInformation && t.PmtId == pmtId);

                    return new CalibrationViewModelStatus.Detail(
                        $"{productivityInformation}/{nameof(CIBInformation.PMTId)}({pmtId})",
                        item?.IsCalibrated,
                        item?.IsVerified);
                }))
        ];
    }

    #endregion 校准
}