using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.CIB.YPixelSize;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using Core.Utilities;
using Core.Utilities.SourceGenerators.Attributes;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using System.IO;
using System.Text;
namespace CugaCalibration.ViewModels.CIB;

[IOCAppService(ServiceType = typeof(CIBYPixelSizeViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBYPixelSizeViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => $"{Cache.ProductivityInformation.ToString()}";

    public override string CalibrateFileName => $"{Cache.ProductivityInformation.ToString()}";

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
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
    private IReadOnlyList<CIBYPixelSizeDTO> _calibratingItems = [];

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationStatus> _calibratingStatuses = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private IReadOnlyList<CIBYPixelSizeDTO> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<CIBYPixelSizeDTO> _selectedReviewItems = [];

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private CIBYPixelSizeCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private CIBYPixelSizeDTO[] _calibrations = [];

    [ObservableProperty]
    private MicroscopeCalChipDTO _microscopeCalChip = new();

    [ObservableProperty]
    private MicroscopeCalChipCache _microscopeCalChipCache = new();

    [ObservableProperty]
    private AlignmentCacheBrightField _alignmentCacheBrightField = new();

    [ObservableProperty]
    private AlignmentCacheDarkField _alignmentCacheDarkField = new();

    [ObservableProperty]
    private AlignmentCacheDarkField[] _alignmentCacheDarkFields = [];

    [ObservableProperty]
    private AlignmentWindowBrightFieldViewModel _alignmentWindowBrightFieldViewModel = HostApplication.GetRequiredService<AlignmentWindowBrightFieldViewModel>();

    [ObservableProperty]
    private AlignmentWindowDarkFieldViewModel _alignmentWindowDarkFieldViewModel = HostApplication.GetRequiredService<AlignmentWindowDarkFieldViewModel>();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false) return false;

        MicroscopeCalChip = CalibrationStatusService.GetCalibration<MicroscopeCalChipDTO>();

        AlignmentCacheDarkFields = RecipeCacheProvider.GetOrDefaultArray<AlignmentCacheDarkField>();
        AlignmentCacheBrightField = RecipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();
        MicroscopeCalChipCache = RecipeCacheProvider.GetOrDefault<MicroscopeCalChipCache>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<CIBYPixelSizeCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<CIBYPixelSizeDTO>();

        if (CalibratingStatuses.Count == 0)
            CalibratingStatuses = [.. ApplicationCookie.OpticsMagTypeProductivityInformations.Select(t => new ProductivityInformationStatus { SelectedItem = t })];

        Calibrations =
        [
            .. Calibrations
                .Where(t => ApplicationCookie.OpticsMagTypeProductivityInformations.Contains(t.ProductivityInformation))
                .Select(t =>
                {
                    CalibratingStatuses
                        .Single(tt => tt.SelectedItem == t.ProductivityInformation)
                        .IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        Cache.PmtInterval = CalibrationSetting.SettingCommonParam.PMTInterval;
        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

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
                CalibratingStatuses
                    .Single(t => t.SelectedItem == Cache.ProductivityInformation)
                    .IsCalibrated = true;

                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

                IsCalibrated = CalibratingStatuses.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;
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
        return InvokeCalibrateAsync(() =>
        {
            DialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment ?",
                out var dialogResult,
                DialogButtonsEnum.YesNo,
                DialogIconEnum.Question);

            Cache.Item.IsDarkFieldAlignment = dialogResult == DialogResultEnum.Yes;

            AlignmentResultDto alignmentResult;
            if (Cache.Item.IsDarkFieldAlignment)
            {
                AlignmentCacheDarkField = AlignmentCacheDarkFields.SingleOrDefault(t => t.OpticsIlluminationModeEnum == Cache.ProductivityInformation.OpticsIlluminationModeEnum
                                                                                        && t.ProductivityInformation == Cache.ProductivityInformation, new AlignmentCacheDarkField());
                if (AlignmentCacheDarkField.IsOk == false)
                {
                    var alignmentWindowDarkFieldViewModel = AlignmentWindowDarkFieldViewModel;
                    Guard.IsTrue(WindowManagerService.ShowDialog(alignmentWindowDarkFieldViewModel) == true, nameof(alignmentWindowDarkFieldViewModel));
                    AlignmentCacheDarkFields = [.. AlignmentCacheDarkFields, alignmentWindowDarkFieldViewModel.Cache];
                }

                alignmentResult = StageViewModel.AlignmentDarkField(
                    AlignmentCacheDarkField.LowSite1,
                    AlignmentCacheDarkField.LowSite2,
                    AlignmentCacheDarkField.HighSite1,
                    AlignmentCacheDarkField.HighSite2,
                    Cache.ProductivityInformation,
                    AlignmentCacheDarkField.LowMag,
                    AlignmentCacheDarkField.AlgorithmWaferTypeEnum,
                    opticsIlluminationModeEnum: Cache.ProductivityInformation.OpticsIlluminationModeEnum);
            }
            else
            {
                if (Cache.CalChipSiteModelEnum is CalChipSiteModelEnum.ChuckModel)
                {
                    if (AlignmentCacheBrightField.IsOk == false)
                    {
                        var alignmentWindowBrightFieldViewModel = AlignmentWindowBrightFieldViewModel;
                        Guard.IsTrue(WindowManagerService.ShowDialog(alignmentWindowBrightFieldViewModel) == true, nameof(alignmentWindowBrightFieldViewModel));
                        AlignmentCacheBrightField = alignmentWindowBrightFieldViewModel.Cache;
                    }

                    alignmentResult = StageViewModel.Alignment(
                        AlignmentCacheBrightField.LowSite1,
                        AlignmentCacheBrightField.LowSite2,
                        AlignmentCacheBrightField.HighSite1,
                        AlignmentCacheBrightField.HighSite2,
                        AlignmentCacheBrightField.LowMag,
                        AlignmentCacheBrightField.HighMag,
                        AlignmentCacheBrightField.AlgorithmWaferTypeEnum,
                        Cache.CalChipSiteModelEnum);
                }
                else
                {
                    alignmentResult = StageViewModel.Alignment(
                        MicroscopeCalChipCache.LowSite1,
                        MicroscopeCalChipCache.LowSite2,
                        MicroscopeCalChipCache.HighSite1,
                        MicroscopeCalChipCache.HighSite2,
                        MicroscopeCalChipCache.LowMicroscopeLensInformation,
                        MicroscopeCalChipCache.HighMicroscopeLensInformation,
                        AlignmentCacheBrightField.AlgorithmWaferTypeEnum,
                        Cache.CalChipSiteModelEnum);
                }
            }

            Cache.Item.AlignmentResult = alignmentResult;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.CalChipSiteModelEnum,
                Cache.Item.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(Cache.Item.AlignmentResult.ToHtmlAnonymous())
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
                Cache.Item.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(Cache.Item.AlignmentResult.ToHtmlAnonymous()),
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
                        ("PMT Y Pixel Size(Y:um,X:PMT ID)", [..CalibratingItems.OrderBy(t => t.PmtId).Select(t => new Point(t.PmtId, t.YPixelSize))])
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

            if (Cache.Item.IsDarkFieldAlignment == false)
            {
                if (Cache.CalChipSiteModelEnum is CalChipSiteModelEnum.ChuckModel)
                {
                    StageViewModel.Alignment(
                        AlignmentCacheBrightField.LowSite1,
                        AlignmentCacheBrightField.LowSite2,
                        AlignmentCacheBrightField.HighSite1,
                        AlignmentCacheBrightField.HighSite2,
                        AlignmentCacheBrightField.LowMag,
                        AlignmentCacheBrightField.HighMag,
                        AlignmentCacheBrightField.AlgorithmWaferTypeEnum,
                        Cache.CalChipSiteModelEnum);
                }
                else
                {
                    StageViewModel.Alignment(
                        MicroscopeCalChipCache.LowSite1,
                        MicroscopeCalChipCache.LowSite2,
                        MicroscopeCalChipCache.HighSite1,
                        MicroscopeCalChipCache.HighSite2,
                        MicroscopeCalChipCache.LowMicroscopeLensInformation,
                        MicroscopeCalChipCache.HighMicroscopeLensInformation,
                        AlignmentCacheBrightField.AlgorithmWaferTypeEnum,
                        Cache.CalChipSiteModelEnum);
                }
            }
            else
            {
                StageViewModel.AlignmentDarkField(
                    AlignmentCacheDarkField.LowSite1,
                    AlignmentCacheDarkField.LowSite2,
                    AlignmentCacheDarkField.HighSite1,
                    AlignmentCacheDarkField.HighSite2,
                    Cache.ProductivityInformation,
                    AlignmentCacheDarkField.LowMag,
                    AlignmentCacheDarkField.AlgorithmWaferTypeEnum,
                    opticsIlluminationModeEnum: Cache.ProductivityInformation.OpticsIlluminationModeEnum);
            }

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
                    Cache.Item.IsDarkFieldAlignment,
                    AlignmentResult = new HtmlQuote(Cache.Item.AlignmentResult.ToHtmlAnonymous()),
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
                .. Calibrations
                    .Where(t => (t.ProductivityInformation == dto.ProductivityInformation && t.PmtId == dto.PmtId) == false)
                    .Where(t =>
                        t.ProductivityInformation != dto.ProductivityInformation
                        || ApplicationCookie.CIBInformations.Any(tt => tt.PMTId == t.PmtId)),
                dto
            ];
        }

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}