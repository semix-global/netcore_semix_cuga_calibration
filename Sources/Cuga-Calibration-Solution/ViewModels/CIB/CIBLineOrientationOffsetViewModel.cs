using Core.Models.Models.Common.Cookies;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.CIB.LineOrientationOffset;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.PixelSize;
using Core.Utilities;
using Core.Utilities.SourceGenerators.Attributes;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WaferMap.WPF.Primitives.Builders;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using System.Text;

namespace CugaCalibration.ViewModels.CIB;

[IOCAppService(ServiceType = typeof(CIBLineOrientationOffsetViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBLineOrientationOffsetViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override IReadOnlyList<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "Image Param" },
        new() { StepName = "Alignment" },
        new() { StepName = "Find Position" },
        new() { StepName = "Find Template" },
        new() { StepName = "Calibration" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private IReadOnlyList<CIBLineOrientationOffsetDTO> _calibratingItems = [];

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationStatus> _calibratingStatuses = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private IReadOnlyList<CIBLineOrientationOffsetDTO> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<CIBLineOrientationOffsetDTO> _selectedReviewItems = [];

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private CIBLineOrientationOffsetCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private CIBLineOrientationOffsetDTO[] _calibrations = [];

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _microscopePixelSizes = [];

    [ObservableProperty]
    private CreateDarkImageTemplateWindowViewModel _createDarkImageTemplateWindowViewModel = HostApplication.GetRequiredService<CreateDarkImageTemplateWindowViewModel>();

    [ObservableProperty]
    public partial AlignmentUserControlViewModel AlignmentUserControlViewModel { get; set; } = HostApplication.GetRequiredService<AlignmentUserControlViewModel>();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false) return false;

        MicroscopePixelSizes = ApplicationCookieService.GetCalibrations<MicroscopePixelSizeItemDto>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<CIBLineOrientationOffsetCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<CIBLineOrientationOffsetDTO>(cancellationToken);

        if (Cache.Item.MicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.Item.MicroscopeLensInformation = CalibrationSetting.SettingCommonParam.HighMicroscopeLensInformation.Clone();

        Cache.PmtInterval = CalibrationSetting.SettingCommonParam.PMTInterval;

        UpdateEntryStatus([..Calibrations], cancellationToken);

        return true;
    }

    public override void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var temp = Guard.IsAssignableToTypeAndReturn<CIBLineOrientationOffsetDTO[]>(calibrations);
        var status = Entry.Status;
        var infos = ApplicationCookie.ProductivityInformations;

        CalibratingStatuses = [.. infos.Select(t => new ProductivityInformationStatus { SelectedItem = t, IsCalibrated = false })];

        Calibrations = [.. temp
            .Where(t => infos.Contains(t.ProductivityInformation))
            .Select(t => {
                CalibratingStatuses.Single(tt => tt.SelectedItem == t.ProductivityInformation).IsCalibrated = t.IsCalibrated;
                return t;
            })];

        status.TotalCalibrationCount = infos.Count;
        status.CalibratedCount = Calibrations.Count(t => t.IsCalibrated);
        status.ReviewCount = Calibrations.Count(t => t.IsVerified);
        status.Details = [.. infos.Select(t => {
            var item = Calibrations.SingleOrDefault(tt => tt.ProductivityInformation == t);
            return new CalibrationViewModelStatus.Detail(t.ToString(), item?.IsCalibrated, item?.IsVerified);
        })];
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Cache.Item.FindPosition = Cache.Item.FindPosition.ToOriginLength >= Cache.Item.WaferRadius
            ? new Point(0, 0)
            : Cache.Item.FindPosition;
        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
        StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.Item.FindPosition);
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

        if (Reviews.All(t => t.IsCalibrated == false))
            return false;

        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
        StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.Item.FindPosition);

        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 1:
                DialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment ?", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
                Cache.Item.IsDarkFieldAlignment = dialogResult == DialogResultEnum.Yes;
                return true;

            case 2:
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.Item.FindPosition);
                return true;

            case 3:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.Item.FindPosition);
                return true;

            case 5:
                CalibratingStatuses
                    .Single(t => t.SelectedItem == Cache.ProductivityInformation)
                    .IsCalibrated = true;

                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

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
                Cache.ProductivityInformation
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
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.CIBInformation,
                Cache.Item.LaserLightInformation,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
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
        return InvokeCalibrateAsync(async () =>
        {
            AlignmentUserControlViewModel.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
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
                AlignmentUserControlViewModel.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(alignmentResult.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step3Async(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(() =>
        {
            var result = StageViewModel.GetBrightFieldStagePosition();

            Cache.Item.FindPosition = result;

            Cache.Item.BrightTemplateFilePath = $"{TemplateFileDirectory}\\{Cache.Item.MicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            var generateTemplateHigh = ReviewViewModel.TryGenerateTemplate(Cache.Item.AlgorithmTemplateTypeEnum, Cache.Item.BrightTemplateFilePath, Cache.Item.AlgorithmTemplateSizeEnum);
            if (generateTemplateHigh == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            else Cache.Item.BrightTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.Item.BrightTemplateFilePath);

            var waferMapDieBuilder = new WaferMapDieBuilder
            {
                DiePitchSize = new Size(Cache.Item.DiePitchWidth * Cache.Item.ReticleDieCountX, Cache.Item.DiePitchWidth * Cache.Item.ReticleDieCountX),
                OriginalDiePoint = Cache.Item.FindPosition
            };

            var dies = waferMapDieBuilder.BuildDie(new Circle(Point.Origin, Cache.Item.WaferRadius));

            var currentRowDies = dies
                .Where(t => t.Index.Y == 0)
                .OrderBy(t => t.Index.X).ToArray();
            Cache.Item.ImageCount = currentRowDies.Length;
            Guard.IsGreaterThan(Cache.Item.ImageCount, 2);

            Cache.Item.StartPosition = currentRowDies[0].Rect.Point;
            Cache.Item.EndPosition = currentRowDies[^1].Rect.Point;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
            {
                Cache.Item.AlgorithmTemplateTypeEnum,
                Cache.Item.MicroscopeLensInformation.LensName,
                Cache.Item.WaferRadius,
                Cache.Item.DiePitchWidth,
                Cache.Item.ReticleDieCountX,
                Cache.Item.ImageCount,
                Cache.Item.FindPosition,
                Cache.Item.StartPosition,
                Cache.Item.EndPosition,
                Cache.Item.BrightTemplateFilePath,
                HtmlTab = new HtmlTab(new
                {
                    HighTemplateImage = new HtmlImage(Cache.Item.BrightTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }


    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step4Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            if (Cache.Item.FindPosition.ToOriginLength >= Cache.Item.WaferRadius)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment("The Bright Field Position Out Of The Wafer!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            var darkFieldImageDto = await CIBViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Bright,
                Cache.Item.FindPosition,
                Cache.Item.XWidthPixel,
                Cache.Item.CIBInformation,
                (false, CalChipSiteModelEnum.ChuckModel),
                (false, Cache.Item.OpticsConfiguration),
                (false, Cache.Item.CIBConfiguration),
                (false, Cache.Item.LaserLightInformation),
                false,
                cancellationToken);

            var detectImageDirectory = ImageFileDirectory;
            using var _ = darkFieldImageDto;

            Cache.Item.TemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.Item.MicroscopeLensInformation.LensName}_{Guid.NewGuid()}";

            var filePath = $"{detectImageDirectory}\\Guid({HtmlLogUniqueId}_{Guid.NewGuid()}).jpg";
            darkFieldImageDto.Image.SaveImage(filePath);
            CreateDarkImageTemplateWindowViewModel.ImageFilePath = filePath;
            CreateDarkImageTemplateWindowViewModel.TemplateFilePath = Cache.Item.TemplateFilePath;

            var showDialog = WindowManagerService.ShowDialog(CreateDarkImageTemplateWindowViewModel);

            if (showDialog == false)
            {
                DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            Cache.Item.TemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.Item.TemplateFilePath);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.FindPosition,
                HtmlTab = new HtmlTab(new
                {
                    TemplateImage = new HtmlImage(Cache.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }


    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step5Async(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(async () =>
        {
            var detectImageDirectory = ImageFileDirectory;
            var tempImageDirectory = TemplateFileDirectory;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBInformation,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(Cache.Item.AlignmentResult.ToHtmlAnonymous()),
                Cache.Item.XWidthPixel,
                Cache.PmtInterval,
                Cache.AlgorithmTemplateTypeEnum,
                Cache.AlgorithmTemplateSizeEnum,
                Cache.Item.FindPosition,
                Cache.Item.StartPosition,
                Cache.Item.EndPosition,
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            var calibratingItems = ApplicationCookie.CIBInformationPMTIds
                .OrderBy(t => t)
                .Select(t =>
                    new CIBLineOrientationOffsetDTO()
                    {
                        MicroscopeLensInformation = Cache.Item.MicroscopeLensInformation,
                        ProductivityInformation = Cache.ProductivityInformation,
                        PmtId = t,
                        FindPosition = Cache.Item.FindPosition + (Vector)new Point(0, (t - CalibrationConstantsHelper.MainPmtId) * Cache.PmtInterval),
                        StartPosition = Cache.Item.StartPosition + (Vector)new Point(0, (t - CalibrationConstantsHelper.MainPmtId) * Cache.PmtInterval),
                        EndPosition = Cache.Item.EndPosition + (Vector)new Point(0, (t - CalibrationConstantsHelper.MainPmtId) * Cache.PmtInterval),
                        ForwardFilePath = detectImageDirectory + "Forward",
                        ReverseFilePath = detectImageDirectory + "Reverse",
                        TemplateFilePath = tempImageDirectory
                    }).ToList();

            Guard.IsNotEmpty(calibratingItems);

            // 当前只校准中心光斑，多光斑暂不实现
            CalibratingItems = [calibratingItems.Single(t => t.PmtId == Cache.Item.CIBInformation.PMTId)];
            foreach (var calibrationItem in CalibratingItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await GetLineOrientationOffsetAsync(calibrationItem, cancellationToken);

                calibrationItem.IsCalibrated = true;
            }

            Guard.IsTrue(Save(CalibratingItems, cancellationToken));

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

            var centerDto = Reviews.SingleOrDefault(t => t.ProductivityInformation == Cache.ProductivityInformation
                                                         && t.PmtId == Cache.Item.CIBInformation.PMTId);
            Guard.IsNotNull(centerDto);

            AlignmentUserControlViewModel.IsDarkFieldAlignment = Cache.Item.IsDarkFieldAlignment;
            AlignmentUserControlViewModel.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
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
                var tempImageDirectory = TemplateFileDirectory;

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    Cache.ProductivityInformation,
                    Cache.Item.MicroscopeLensInformation,
                    Cache.Item.LaserLightInformation,
                    OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                    CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                    Cache.Item.CIBInformation,
                    Cache.Item.IsDarkFieldAlignment,
                    AlignmentResult = new HtmlQuote(Cache.Item.AlignmentResult.ToHtmlAnonymous()),
                    Cache.Item.XWidthPixel,
                    Cache.AlgorithmTemplateTypeEnum,
                    Cache.AlgorithmTemplateSizeEnum,
                    Cache.Item.FindPosition,
                    Cache.Item.StartPosition,
                    Cache.Item.EndPosition,
                    detectImageDirectory,
                    Cache.Item.Threshold
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("Verify", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    selectedReviewItem.XOffset
                }), HtmlLogUniqueId.LoggingHtml());

                var verifyItem = selectedReviewItem.Clone();
                verifyItem.ForwardFilePath = detectImageDirectory + "Forward";
                verifyItem.ReverseFilePath = detectImageDirectory + "Reverse";
                verifyItem.TemplateFilePath = tempImageDirectory;

                await GetLineOrientationOffsetAsync(verifyItem, cancellationToken, true);

                var isOk = Math.Abs(verifyItem.XOffset) < Cache.Item.Threshold;

                var htmlQuote = new HtmlQuote(new
                {
                    CalibrationOffset = selectedReviewItem.XOffset,
                    VerifyOffset = verifyItem.XOffset
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

    private async Task GetLineOrientationOffsetAsync(CIBLineOrientationOffsetDTO lineOrientationOffsetDTO, CancellationToken cancellationToken, bool isVerify = false)
    {
        Logger.LogHtmlInformation($"PMT ID :{lineOrientationOffsetDTO.PmtId}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

        await GetMatchResultAsync(lineOrientationOffsetDTO, true).ConfigureAwait(false);

        if (isVerify)
        {
            var (xDirection, _) = StageViewModel.GetMachineDirection();
            lineOrientationOffsetDTO.StartPosition += (Vector)new Point(lineOrientationOffsetDTO.XOffset * xDirection, 0);
            lineOrientationOffsetDTO.EndPosition += (Vector)new Point(lineOrientationOffsetDTO.XOffset * xDirection, 0);
        }

        await GetMatchResultAsync(lineOrientationOffsetDTO, false).ConfigureAwait(false);

        Logger.LogHtmlInformation($"Success: {lineOrientationOffsetDTO.PmtId}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
        {
            lineOrientationOffsetDTO.PmtId,
            lineOrientationOffsetDTO.FindPosition,
            lineOrientationOffsetDTO.StartPosition,
            lineOrientationOffsetDTO.EndPosition,
            lineOrientationOffsetDTO.ForwardFindDarkMachinePosition,
            lineOrientationOffsetDTO.ReverseFindDarkMachinePosition,
            lineOrientationOffsetDTO.XOffset,
            lineOrientationOffsetDTO.TemplateFilePath,
            lineOrientationOffsetDTO.ForwardFilePath,
            lineOrientationOffsetDTO.ReverseFilePath
        }), HtmlLogUniqueId.LoggingHtml());

        return;

        async Task GetMatchResultAsync(CIBLineOrientationOffsetDTO lineOrientationOffsetDto, bool isForward)
        {
            var positions = Enumerable.Range(0, Cache.Item.ImageCount).Select(t => Cache.Item.StartPosition + new Vector(t * Cache.Item.DiePitchWidth * Cache.Item.ReticleDieCountX, 0)).ToArray();
            if (isForward == false) positions = [.. positions.AsEnumerable().Reverse()];

            Logger.LogHtmlInformation($"{(isForward ? "Forward" : "Reverse")} Param", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
            {
                isForward,
                lineOrientationOffsetDTO.StartPosition,
                lineOrientationOffsetDTO.EndPosition,
                lineOrientationOffsetDTO.XOffset,
                Points = new HtmlTable([.. positions.Select(t => t)])
            }), HtmlLogUniqueId.LoggingHtml());

            var darkFieldImages = await CIBViewModel.GetPMTImagesAsync(
                lineOrientationOffsetDto.ProductivityInformation,
                StageCoordinateSystemEnum.Bright,
                positions,
                Cache.Item.XWidthPixel,
                ApplicationCookie.CIBInformations.Single(t => t.PMTId == lineOrientationOffsetDto.PmtId && t.ChannelId == Cache.Item.CIBInformation.ChannelId),
                (false, CalChipSiteModelEnum.ChuckModel),
                (false, Cache.Item.OpticsConfiguration),
                (false, Cache.Item.CIBConfiguration),
                (false, Cache.Item.LaserLightInformation),
                false,
                cancellationToken);

            var findPosition = positions.Select((t, i) => (Index: i, Point: t))
                .Minima(t => Math.Abs((t.Point.X - lineOrientationOffsetDto.FindPosition.X)))
                .First();
            var imageDto = isForward ? darkFieldImages[findPosition.Index] : darkFieldImages[^findPosition.Index];

            if (CIBViewModel.TryGetMatchPosition(
                    lineOrientationOffsetDto.ProductivityInformation,
                    StageCoordinateSystemEnum.Dark,
                    findPosition.Point,
                    ApplicationCookie.CIBInformations.Single(t => t.PMTId == lineOrientationOffsetDto.PmtId && t.ChannelId == Cache.Item.CIBInformation.ChannelId),
                    Cache.Item.AlgorithmTemplateTypeEnum,
                    imageDto,
                    Cache.Item.TemplateFilePath,
                    isForward ? lineOrientationOffsetDto.ForwardFilePath : lineOrientationOffsetDto.ReverseFilePath,
                    HtmlLogUniqueId,
                    out var position,
                    out _,
                    out _,
                    out var resultImageFilePath) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, new HtmlComment("Error: Get Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
                ThrowHelper.ThrowArgumentException($"Get Match Position Failed! PMT ID: {lineOrientationOffsetDto.PmtId}, IsForward: {isForward}");
            }

            var resultMachinePosition = StageViewModel.DarkFieldToMachinePosition(position);
            if (isForward)
            {
                lineOrientationOffsetDto.ForwardFilePath = resultImageFilePath;
                lineOrientationOffsetDto.ForwardFindDarkMachinePosition = resultMachinePosition;
            }
            else
            {
                lineOrientationOffsetDto.ReverseFilePath = resultImageFilePath;
                lineOrientationOffsetDto.ReverseFindDarkMachinePosition = resultMachinePosition;
            }

            foreach (var darkFieldImage in darkFieldImages)
            {
                using var _ = darkFieldImage;
            }
        }
    }

    private bool Save(IReadOnlyList<CIBLineOrientationOffsetDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
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

        ApplicationCookieService.SetCalibrations(Calibrations, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    #endregion 校准
}