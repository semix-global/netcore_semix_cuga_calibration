using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Chuck.CenterAndTheta;
using Core.Models.Models.CIB.LineCentricity;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.PixelSize;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.SQL.Cache.Providers.Extensions;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using System.IO;
using System.Text;

namespace CugaCalibration.ViewModels.CIB;

[IOCAppService(ServiceType = typeof(CIBLineCentricityViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBLineCentricityViewModel(IApplicationCookieService applicationCookieService) : CalibrationViewModelBase
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
        new() { StepName = "Line Centricity" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private IReadOnlyList<CIBLineCentricityDTO> _calibratingItems = [];

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationStatus> _calibratingStatuses = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private IReadOnlyList<CIBLineCentricityDTO> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<CIBLineCentricityDTO> _selectedReviewItems = [];

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private CIBLineCentricityCache _cache = new();

    [ObservableProperty]
    private CIBLineCentricityDTO[] _calibrations = [];

    [ObservableProperty]
    private MicroscopeCalChipDTO _microscopeCalChip = new();

    [ObservableProperty]
    private MicroscopeCalChipCache _microscopeCalChipCache = new();

    [ObservableProperty]
    private ChuckCenterAndThetaItemDto _chuckCenter = new();

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _microscopePixelSizeItems = [];

    [ObservableProperty]
    private AlignmentCacheBrightField _alignmentCacheBrightField = new();

    [ObservableProperty]
    private AlignmentCacheDarkField _alignmentCacheDarkField = new();

    [ObservableProperty]
    private AlignmentCacheDarkField[] _alignmentCacheDarkFields = [];

    [ObservableProperty]
    private CreateDarkImageTemplateWindowViewModel _createDarkImageTemplateWindowViewModel = HostApplication.GetRequiredService<CreateDarkImageTemplateWindowViewModel>();

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

        MicroscopeCalChip = CalibrationStatusService.GetCalibration<MicroscopeCalChipDTO>();

        if (LoadDepends() == false) return false;

        AlignmentCacheDarkFields = RecipeCacheProvider.GetOrDefaultArray<AlignmentCacheDarkField>();
        AlignmentCacheBrightField = RecipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();
        MicroscopeCalChipCache = RecipeCacheProvider.GetOrDefault<MicroscopeCalChipCache>();
        ChuckCenter = CacheProvider.GetOrDefault<ChuckCenterAndThetaItemDto>();
        MicroscopePixelSizeItems = CacheProvider.GetOrDefaultArray<MicroscopePixelSizeItemDto>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<CIBLineCentricityCache>();

        Calibrations = CacheProvider.GetOrDefaultArray<CIBLineCentricityDTO>();

        if (CalibratingStatuses.Count == 0)
            CalibratingStatuses = [.. ApplicationCookie.ProductivityInformations.Select(t => new ProductivityInformationStatus { SelectedItem = t })];

        Calibrations =
        [
            .. Calibrations
                .Where(t => ApplicationCookie.ProductivityInformations.Contains(t.ProductivityInformation))
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
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.Item.MicroscopeLensInformation)
                   && ApplicationCookie.LaserLightInformations.Contains(Cache.Item.LaserLightInformation)
                   && ApplicationCookie.CIBInformations.Contains(Cache.Item.CIBInformation);
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
        return InvokeCalibrateAsync(async () =>
        {
            Guard.IsEqualTo(Cache.Item.MicroscopeLensInformation, MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(Cache.Item.AlignmentResult.ToHtmlAnonymous()),
                Cache.Item.ImageWidth
            }), HtmlLogUniqueId.LoggingHtml());

            Cache.Item.FindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Cache.Item.BrightTemplateFilePath = Path.Combine(TemplateFileDirectory, Cache.Item.MicroscopeLensInformation.LensName, Guid.NewGuid().ToString());
            var generateTemplateHigh = ReviewViewModel.TryGenerateTemplate(Cache.Item.AlgorithmTemplateTypeEnum, Cache.Item.BrightTemplateFilePath, Cache.Item.AlgorithmTemplateSizeEnum);
            if (generateTemplateHigh == false)
            {
                DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            Cache.Item.BrightTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.Item.BrightTemplateFilePath);

            var darkFieldImageDto = await CIBViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Bright,
                StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition),
                Cache.Item.ImageWidth,
                Cache.Item.CIBInformation,
                (false, Cache.CalChipSiteModelEnum),
                (false, Cache.Item.CIBConfiguration),
                (false, Cache.Item.LaserLightInformation),
                false,
                cancellationToken);

            using var _ = darkFieldImageDto;

            var originImageFilePath = Path.Combine(TemplateFileDirectory, Cache.Item.MicroscopeLensInformation.ToString(), $"{Guid.NewGuid():N}.jpg");
            Cache.Item.TemplateFilePath = $"{originImageFilePath}_Template";
            darkFieldImageDto.Image.Save(originImageFilePath);

            Guard.IsTrue(Cache.AlgorithmTemplateTypeEnum != AlgorithmTemplateTypeEnum.Projection);

            CreateDarkImageTemplateWindowViewModel.ImageFilePath = originImageFilePath;
            CreateDarkImageTemplateWindowViewModel.TemplateFilePath = Cache.Item.TemplateFilePath;
            CreateDarkImageTemplateWindowViewModel.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
            CreateDarkImageTemplateWindowViewModel.AlgorithmTemplateSizeEnum = Cache.AlgorithmTemplateSizeEnum;

            Guard.IsTrue(WindowManagerService.ShowDialog(CreateDarkImageTemplateWindowViewModel) == true, nameof(CreateDarkImageTemplateWindowViewModel));

            Cache.Item.TemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.Item.TemplateFilePath);

            Logger.LogHtmlInformation("TemplateImage", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.AlgorithmTemplateTypeEnum,
                Cache.AlgorithmTemplateSizeEnum,
                Cache.Item.FindBFMachinePosition,
                BFTemplateFilePath = Cache.Item.BrightTemplateFilePath,
                DFTemplateFilePath = Cache.Item.TemplateFilePath,
                BFHtmlTab = new HtmlTab(new
                {
                    TemplateImage = new HtmlImage(Cache.Item.BrightTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                }),
                DFHtmlTab = new HtmlTab(new
                {
                    TemplateImage = new HtmlImage(Cache.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    OriginImage = new HtmlImage(originImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
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
                Cache.Item.CIBInformation,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(Cache.Item.AlignmentResult.ToHtmlAnonymous()),
                Cache.Item.ImageWidth,
                Cache.PmtInterval,
                Cache.AlgorithmTemplateTypeEnum,
                Cache.AlgorithmTemplateSizeEnum,
                Cache.Item.FindBFMachinePosition,
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            var (_, yDirection) = StageViewModel.GetMachineDirection();
            CalibratingItems = ApplicationCookie.CIBInformationPMTIds
                .OrderBy(t => t)
                .Select(t =>
                    new CIBLineCentricityDTO()
                    {
                        MicroscopeLensInformation = Cache.Item.MicroscopeLensInformation,
                        ProductivityInformation = Cache.ProductivityInformation,
                        PmtId = t,
                        FindDFMachinePosition = StageViewModel.DarkFieldToMachinePosition(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition)) + (Vector)new Point(0, yDirection * (t - CalibrationConstantsHelper.MainPmtId) * Cache.PmtInterval),
                        FilePath = detectImageDirectory,
                        RawFilePath = detectImageDirectory
                    }).ToList();

            Guard.IsNotEmpty(CalibratingItems);

            Logger.LogHtmlInformation("Get Line Centricity", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

            foreach (var item in CalibratingItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                item.IsCalibrated = await GetLineCentricityAsync(item, cancellationToken);
            }

            Guard.IsTrue(Save(CalibratingItems, cancellationToken));

            if (CalibratingItems.Count > 3)
            {
                var calibrationOffsets = applicationCookieService.GetLineCentricityMachineOffsetList([.. CalibratingItems], Cache.ProductivityInformation);
                LineCentricityOffsetsFit(calibrationOffsets);
            }

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

            var centerLineCentricityDTO = Reviews.SingleOrDefault(t => t.ProductivityInformation == Cache.ProductivityInformation
                                                                       && t.PmtId == CalibrationConstantsHelper.MainPmtId);
            Guard.IsNotNull(centerLineCentricityDTO);

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

            var detectImageDirectory = ImageFileDirectory;

            var (xDirection, yDirection) = StageViewModel.GetMachineDirection();

            if (ReviewViewModel.TryGetMatchPosition(
                    Cache.Item.AlgorithmTemplateTypeEnum,
                    MicroscopePixelSizeItems,
                    StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition),
                    Cache.Item.MicroscopeLensInformation,
                    Cache.Item.BrightTemplateFilePath,
                    detectImageDirectory,
                    HtmlLogUniqueId,
                    Name,
                    string.Empty,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    Cache.CalChipSiteModelEnum) == false) return false;

            var bfToDfMachinePositionOffset = centerLineCentricityDTO.FindDFMachinePosition - Cache.Item.FindBFMachinePosition;
            var bfMatchResultMachinePosition = StageViewModel.GetMachineStagePosition();
            var ideaCenterDFMachinePosition = bfMatchResultMachinePosition + bfToDfMachinePositionOffset;

            var calibrationOffsets = applicationCookieService.GetLineCentricityMachineOffsetList(Calibrations, Cache.ProductivityInformation);

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

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    Cache.ProductivityInformation,
                    Cache.Item.MicroscopeLensInformation,
                    Cache.Item.LaserLightInformation,
                    CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                    Cache.Item.CIBInformation,
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
                    selectedReviewItem.DFMachineCenterPosition
                }), HtmlLogUniqueId.LoggingHtml());

                selectedReviewItem.IsVerified = false;

                var verifyItem = selectedReviewItem.Clone();
                verifyItem.RawFilePath = detectImageDirectory;
                verifyItem.FilePath = detectImageDirectory;

                var calibrationOffset = calibrationOffsets.Single(t => t.Pmt == verifyItem.PmtId).Offset;

                var ideaDFMachinePosition = new Point(ideaCenterDFMachinePosition.X, ideaCenterDFMachinePosition.Y + yDirection * Cache.PmtInterval * (verifyItem.PmtId - CalibrationConstantsHelper.MainPmtId))
                                            + (Vector)new Point(xDirection * calibrationOffset.X, yDirection * calibrationOffset.Y);

                verifyItem.FindDFMachinePosition = ideaDFMachinePosition;

                await GetLineCentricityAsync(verifyItem, cancellationToken);
                selectedReviewItem.FilePath = verifyItem.FilePath;
                selectedReviewItem.DFMatchPositionOffset = verifyItem.DFMatchPositionOffset;

                var error = verifyItem.DFMatchPositionOffset;
                var isOk = Math.Abs(error.X) < Cache.Threshold.X
                           && Math.Abs(error.Y) < Cache.Threshold.Y;

                var htmlQuote = new HtmlQuote(new
                {
                    CalibrationBFMachinePosition = Cache.Item.FindBFMachinePosition,
                    VerifyBFMachinePosition = bfMatchResultMachinePosition,
                    CalibrationLineCentricity = selectedReviewItem.DFMachineCenterPosition,
                    VerifyLineCentricity = verifyItem.DFMachineCenterPosition,
                    calibrationOffset,
                    ideaDFMachinePosition,
                    VerifyOffset = error
                });

                if (isOk)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlQuote, HtmlLogUniqueId.LoggingHtml());
                else
                {
                    errorMessageStringBuilder.AppendLine($"{title}: Error");
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlQuote, HtmlLogUniqueId.LoggingHtml());
                }

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

    private async Task<bool> GetLineCentricityAsync(CIBLineCentricityDTO cibLineCentricityDTO, CancellationToken cancellationToken)
    {
        Logger.LogHtmlInformation($"PMT ID :{cibLineCentricityDTO.PmtId}", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());

        Guard.IsNotEqualTo(ChuckCenter.NewBFCenterStagePosition, Point.Origin);

        using var darkFieldImage = await CIBViewModel.GetPMTImageAsync(
            Cache.ProductivityInformation,
            StageCoordinateSystemEnum.Dark,
            StageViewModel.MachineToDarkFieldPosition(cibLineCentricityDTO.FindDFMachinePosition),
            Cache.Item.ImageWidth,
            Cache.Item.CIBInformation,
            (false, Cache.CalChipSiteModelEnum),
            (false, Cache.Item.CIBConfiguration),
            (false, Cache.Item.LaserLightInformation),
            false,
            cancellationToken);

        if (CIBViewModel.TryGetMatchPosition(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Machine,
                cibLineCentricityDTO.FindDFMachinePosition,
                Cache.Item.CIBInformation,
                Cache.AlgorithmTemplateTypeEnum,
                darkFieldImage,
                Cache.Item.TemplateFilePath,
                Cache.Item.TemplateImageFilePath,
                HtmlLogUniqueId,
                out var position,
                out _,
                out _,
                out var resultImageFilePath) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, new HtmlComment("Error: Get Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        cibLineCentricityDTO.RawFilePath = darkFieldImage.RawImageFilePath;
        cibLineCentricityDTO.FilePath = resultImageFilePath;
        cibLineCentricityDTO.DFMatchPositionOffset = position - (Vector)cibLineCentricityDTO.FindDFMachinePosition;
        cibLineCentricityDTO.FindDFMachinePosition = position;
        var machineOffset = cibLineCentricityDTO.FindDFMachinePosition - Cache.Item.FindBFMachinePosition; // 明暗场offset(暗-明)
        cibLineCentricityDTO.DFMachineCenterPosition = ChuckCenter.NewBFCenterStagePosition + machineOffset;

        Logger.LogHtmlInformation($"Success: {cibLineCentricityDTO.PmtId}", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            cibLineCentricityDTO.PmtId,
            cibLineCentricityDTO.FindDFMachinePosition,
            cibLineCentricityDTO.DFMatchPositionOffset,
            ForwardFindDFMachinePosition = cibLineCentricityDTO.FindDFMachinePosition,
            ForwardDFMachineCenterPosition = cibLineCentricityDTO.DFMachineCenterPosition
        }), HtmlLogUniqueId.LoggingHtml());

        return true;
    }

    private bool Save(IReadOnlyList<CIBLineCentricityDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                .. Calibrations.Where(t => (t.ProductivityInformation == dto.ProductivityInformation && t.PmtId == dto.PmtId) == false),
                dto
            ];
        }

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    private void LineCentricityOffsetsFit(IReadOnlyCollection<(int Pmt, Point offsets)> results)
    {
        var pmtXErrorCoordinates = results.OrderBy(t => t.Pmt)
            .Select(t => new Point((t.Pmt - CalibrationConstantsHelper.MainPmtId) * CalibrationSetting.SettingCommonParam.PMTInterval, t.offsets.X)).ToArray();

        var (slopeXError, interceptXError, rSquaredXError, _) = PolynomialCurve.Fit1(
            Vector<double>.Build.DenseOfEnumerable(pmtXErrorCoordinates.Select(t => t.X)),
            Vector<double>.Build.DenseOfEnumerable(pmtXErrorCoordinates.Select(t => t.Y)));

        var pmtXErrorTitle = $"y ={slopeXError:0.######}x + {interceptXError:0.######} r^2 = {rSquaredXError:0.######} angle = {MathUtils.RadianAngleToDegreeAngle(Math.Atan(slopeXError))}";

        var pmtYErrorCoordinates = results.OrderBy(t => t.Pmt)
            .Select(t => new Point((t.Pmt - CalibrationConstantsHelper.MainPmtId) * CalibrationSetting.SettingCommonParam.PMTInterval, t.offsets.Y)).ToArray();

        var (slopeYError, interceptYError, rSquaredYError, _) = PolynomialCurve.Fit1(
            Vector<double>.Build.DenseOfEnumerable(pmtYErrorCoordinates.Select(t => t.X)),
            Vector<double>.Build.DenseOfEnumerable(pmtYErrorCoordinates.Select(t => t.Y)));

        var pmtYErrorTitle = $"y ={slopeYError:0.######}x + {interceptYError:0.######} r^2 = {rSquaredYError:0.######} angle = {MathUtils.RadianAngleToDegreeAngle(Math.Atan(slopeYError))}";

        Logger.LogHtmlInformation("Calibration OK", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            PmtXErrors = new HtmlPlot2DLinesChart(
                [
                    ("PMT X Errors(Y:um,X:PMT ID(um))", pmtXErrorCoordinates),
                    (pmtXErrorTitle, pmtXErrorCoordinates.Select(t => new Point(t.X, slopeXError * t.X + interceptXError)).ToArray())
                ],
                "PMT X Errors"),
            PmtYErrors = new HtmlPlot2DLinesChart(
                [
                    ("PMT Y Errors(Y:um,X:PMT ID(um))", pmtYErrorCoordinates),
                    (pmtYErrorTitle, pmtYErrorCoordinates.Select(t => new Point(t.X, slopeYError * t.X + interceptYError)).ToArray())
                ],
                "PMT Y Errors")
        }), HtmlLogUniqueId.LoggingHtml());
    }

    #endregion 校准
}