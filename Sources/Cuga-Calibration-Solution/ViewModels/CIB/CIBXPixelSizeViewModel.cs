using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Ads.PressureGains;
using Core.Models.Models.CIB.XPixelSize;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Status;
using Core.Utilities;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using HalconDotNet;
using Local.NoSQL.DB.Providers.Extensions;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WaferMap.WPF.Primitives.Builders;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Channels;

namespace CugaCalibration.ViewModels.CIB;

[IOCAppService(ServiceType = typeof(CIBXPixelSizeViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBXPixelSizeViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Productivity" },
        new() { StepName = "Image Param" },
        new() { StepName = "Alignment" },
        new() { StepName = "Find Position" },
        new() { StepName = "X Pixel Size" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private CIBXPixelSizeDTO _calibratingItem = new();

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationCalibrationStatus> _calibrationStatuses = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private IReadOnlyList<CIBXPixelSizeDTO> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<CIBXPixelSizeDTO> _selectedReviewItems = [];

    #endregion Review

    #endregion 界面相关

    #region 缓存

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

    [ObservableProperty]
    private CIBXPixelSizeCache _cache = new();

    [ObservableProperty]
    private CIBXPixelSizeDTO[] _calibrations = [];

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

        if (CalibrationStatuses.Count == 0)
            CalibrationStatuses = [.. ApplicationCookie.ProductivityInformations.Select(t => new ProductivityInformationCalibrationStatus { SelectedItem = t })];

        AlignmentCacheDarkFields = RecipeCacheProvider.GetOrDefaultArray<AlignmentCacheDarkField>();
        AlignmentCacheBrightField = RecipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<CIBXPixelSizeCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<CIBXPixelSizeDTO>();

        Calibrations =
        [
            .. Calibrations
                .Where(t => ApplicationCookie.ProductivityInformations.Contains(t.ProductivityInformation))
                .Select(t =>
                {
                    CalibrationStatuses
                        .Single(tt => tt.SelectedItem == t.ProductivityInformation)
                        .IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

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

        Reviews =
        [
            .. Calibrations
                .Select(t => t.Clone())
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
                return true;

            case 4:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
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
                CalibratingItem = new CIBXPixelSizeDTO();

                return true;

            case 1:
                return true;

            case 2:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.Item.FindBFMachinePosition != Point.Origin
                    ? StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition)
                    : Point.Origin);

                return true;

            case 3:

                return true;

            case 4:
                CalibrationStatuses
                    .Single(t => t.SelectedItem == Cache.ProductivityInformation)
                    .IsCalibrated = true;

                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

                IsCalibrated = CalibrationStatuses.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

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
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.CIBInformation
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

            var alignmentResult = new AlignmentResultDto();
            if (Cache.Item.IsDarkFieldAlignment)
            {
                AlignmentCacheDarkField = AlignmentCacheDarkFields.SingleOrDefault(t => t.OpticsIlluminationModeEnum == Cache.ProductivityInformation.OpticsIlluminationModeEnum
                                                                                        && t.ProductivityInformation == Cache.ProductivityInformation, new AlignmentCacheDarkField());
                if (AlignmentCacheDarkField.IsOk)
                    alignmentResult = StageViewModel.AlignmentDarkField(
                        AlignmentCacheDarkField.LowSite1,
                        AlignmentCacheDarkField.LowSite2,
                        AlignmentCacheDarkField.HighSite1,
                        AlignmentCacheDarkField.HighSite2,
                        Cache.ProductivityInformation,
                        AlignmentCacheDarkField.LowMag,
                        AlignmentCacheDarkField.AlgorithmWaferTypeEnum,
                        opticsIlluminationModeEnum: Cache.ProductivityInformation.OpticsIlluminationModeEnum);
                else
                {
                    var alignmentWindowDarkFieldViewModel = AlignmentWindowDarkFieldViewModel;
                    Guard.IsTrue(WindowManagerService.ShowDialog(alignmentWindowDarkFieldViewModel) == true, nameof(alignmentWindowDarkFieldViewModel));
                    AlignmentCacheDarkFields = [.. AlignmentCacheDarkFields, alignmentWindowDarkFieldViewModel.Cache];
                }
            }
            else
            {
                if (AlignmentCacheBrightField.IsOk)
                    alignmentResult = StageViewModel.Alignment(
                        AlignmentCacheBrightField.LowSite1,
                        AlignmentCacheBrightField.LowSite2,
                        AlignmentCacheBrightField.HighSite1,
                        AlignmentCacheBrightField.HighSite2,
                        AlignmentCacheBrightField.LowMag,
                        AlignmentCacheBrightField.HighMag,
                        AlignmentCacheBrightField.AlgorithmWaferTypeEnum);
                else
                {
                    var alignmentWindowBrightFieldViewModel = AlignmentWindowBrightFieldViewModel;
                    Guard.IsTrue(WindowManagerService.ShowDialog(alignmentWindowBrightFieldViewModel) == true, nameof(alignmentWindowBrightFieldViewModel));
                    AlignmentCacheBrightField = alignmentWindowBrightFieldViewModel.Cache;
                }
            }

            Cache.Item.AlignmentResult = alignmentResult;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
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
                Cache.Item.CIBInformation,
                Cache.Item.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(Cache.Item.AlignmentResult.ToHtmlAnonymous()),
                Cache.Item.ImageWidth
            }), HtmlLogUniqueId.LoggingHtml());

            Cache.Item.FindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            var darkFieldImageDto = await CIBViewModel.GetPMTImagesAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Bright,
                StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition),
                Cache.Item.CIBInformation,
                Cache.Item.ImageWidth,
                (false, CalChipSiteModelEnum.ChuckModel),
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
                templateFilePath = Cache.Item.TemplateFilePath,
                HtmlTab = new HtmlTab(new
                {
                    TemplateImage = new HtmlImage(Cache.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    OriginImage = new HtmlImage(originImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step4Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var detectImageDirectory = ImageFileDirectory;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
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
                Cache.Item.TemplateFilePath,
                TemplateImage = new HtmlImage(Cache.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                Cache.Item.WaferRadius,
                Cache.Item.DiePitchWith,
                Cache.Item.ReticleDieCountX,
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.ProductivityInformation = Cache.ProductivityInformation;
            CalibratingItem.MicroscopeLensInformation = Cache.Item.MicroscopeLensInformation;
            CalibratingItem.CIBInformation = Cache.Item.CIBInformation;
            CalibratingItem.XPixelSize = 0d;
            CalibratingItem.RawImageFilePath = string.Empty;
            CalibratingItem.VerifyRawImageFilePath = string.Empty;
            CalibratingItem.SlideItems = [];
            CalibratingItem.SlideSplitDifferences = [];
            CalibratingItem.VerifyItems = [];
            CalibratingItem.VerifySplitDifferences = [];
            CalibratingItem.IsCalibrated = false;

            Guard.IsTrue(CalibrationAlgorithmService.TryReadTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.Item.TemplateFilePath, out var templateId), nameof(CalibrationAlgorithmService.TryReadTemplate));
            using var _ = templateId;

            var (_, templateImageSize) = ImageHelper.GetImageInfo(Cache.Item.TemplateImageFilePath);

            var waferMapDieBuilder = new WaferMapDieBuilder
            {
                DiePitchSize = new Size(Cache.Item.DiePitchWith * Cache.Item.ReticleDieCountX, Cache.Item.DiePitchWith * Cache.Item.ReticleDieCountX),
                OriginalDiePoint = StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition)
            };

            var dies = waferMapDieBuilder.BuildDie(new Circle(Point.Origin, Cache.Item.WaferRadius));

            var currentRowDies = dies
                .Where(t => t.Index.Y == 0)
                .OrderBy(t => t.Index.X).ToArray();
            var imageCount = currentRowDies.Length;
            Guard.IsGreaterThan(imageCount, 2);

            var startPosition = currentRowDies[0].Rect.Point - new Vector(Cache.Item.DiePitchWith * Cache.Item.ReticleDieCountX / 2d, 0);
            var endPosition = currentRowDies[^1].Rect.Point + new Vector(Cache.Item.DiePitchWith * Cache.Item.ReticleDieCountX / 2d, 0);

            var darkFieldRawScanImage = await CIBViewModel.GetPMTImagesAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Bright,
                startPosition,
                endPosition,
                Cache.Item.CIBInformation,
                (false, CalChipSiteModelEnum.ChuckModel),
                (false, Cache.Item.CIBConfiguration),
                (false, Cache.Item.LaserLightInformation),
                false,
                cancellationToken);

            StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition));

            CalibratingItem.RawImageFilePath = darkFieldRawScanImage.RawImageFilePath;

            Logger.LogHtmlInformation("Split", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new { CalibratingItem.RawImageFilePath }), HtmlLogUniqueId.LoggingHtml());

#if NET
            await
#endif
            using var fileSteam = File.OpenRead(CalibratingItem.RawImageFilePath);
            using var binaryReader = new BinaryReader(fileSteam, Encoding.UTF8, true);

            var (size, bodyBytesStartIndex, bodyBytesLength) = RawImageFactory.GetSize(binaryReader);
            var (_, heightPixel) = (SizeI)size;
            var heightPixelByteLength = heightPixel * 2;

            #region RealUmPerPixel

            var windowWidthSize = ((SizeI)templateImageSize).Width * 3;
            var windowWidthStep = ((SizeI)templateImageSize).Width;
            var windowImageAllPixelByteLength = windowWidthSize * heightPixelByteLength;
            var windowStepAllPixelByteLength = windowWidthStep * heightPixelByteLength;
            var totalCount = MathHelper.SlideCount(windowImageAllPixelByteLength, windowStepAllPixelByteLength, bodyBytesLength);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                size,
                windowWidthSize,
                windowWidthStep,
                totalCount
            }), HtmlLogUniqueId.LoggingHtml());

            var itemItems = new CIBXPixelSizeDTOItem[totalCount];

            #region Channel

            using var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
            var channel = Channel.CreateBounded<CIBXPixelSizeDTOItem>(new BoundedChannelOptions(totalCount) { SingleReader = true, SingleWriter = true, AllowSynchronousContinuations = true });

            #region Reader

            // ReSharper disable AccessToDisposedClosure

            var tasks = new Task[totalCount];
            var channelReaderTask = Task.Run(async () =>
            {
                var index = 0;
                await foreach (var itemItem in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                        tasks[index] = ResolveCIBXPixelSizeSlideItemAsync(
                            itemItem,
                            templateId,
                            templateImageSize,
                            detectImageDirectory,
                            semaphore,
                            cancellationToken);

                        index++;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }
            }, cancellationToken);

            // ReSharper restore AccessToDisposedClosure

            #endregion Reader

            #region Writer

            foreach (var (index, pointer) in Enumerable
                         .Range(0, totalCount)
                         .Select(t => (long)t * windowStepAllPixelByteLength).Index())
            {
                cancellationToken.ThrowIfCancellationRequested();

                itemItems[index] = GetCIBXPixelSizeSlideItem(
                    pointer,
                    windowImageAllPixelByteLength,
                    fileSteam,
                    binaryReader,
                    bodyBytesStartIndex,
                    bodyBytesLength,
                    heightPixel,
                    heightPixelByteLength);

                await channel.Writer.WriteAsync(itemItems[index], cancellationToken).ConfigureAwait(false);
            }

            channel.Writer.Complete();

            #endregion Writer

            #endregion Channel

            await channelReaderTask.ConfigureAwait(false);
            await Task.WhenAll(tasks).ConfigureAwait(false);

            CalibratingItem.SlideItems = [.. itemItems];

            var matchPoints = CalibratingItem.SlideItems.Where(t => t.IsMatchOk).Select(t => t.MatchPoint).ToArray();

            var xDifferences = matchPoints
                .Zip(matchPoints.Skip(1), (prev, next) => next.X - prev.X)
                .ToArray();
            var average = xDifferences.Average();
            var xFilterDifferences = xDifferences.Where(t => t >= average).ToArray();
            CalibratingItem.SlideSplitDifferences = xFilterDifferences;

            var isOk = xFilterDifferences.Length == imageCount - 1;

            var htmlAnonymous = new
            {
                AllScore = new HtmlPlot2DLinesChart([(string.Empty, [.. CalibratingItem.SlideItems.Select(t => new Point(t.MatchPoint.X, t.Score))])], string.Empty),
                matchPoints = new HtmlPlot2DLinesChart([(string.Empty, matchPoints)], string.Empty),
                xDifferences = new HtmlPlot2DLinesChart([(string.Empty, [.. xDifferences.Index().Select(t => new Point(t.Index, t.Item))])], string.Empty),
                xFilterDifferences = new HtmlPlot2DLinesChart([(string.Empty, [.. xFilterDifferences.Index().Select(t => new Point(t.Index, t.Item))])], string.Empty)
            };

            if (isOk == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlBullet(htmlAnonymous), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            CalibratingItem.XPixelSize = Cache.Item.DiePitchWith * Cache.Item.ReticleDieCountX / xFilterDifferences.Average();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                CalibratingItem.XPixelSize,
                Plot = new HtmlQuote(htmlAnonymous)
            }), HtmlLogUniqueId.LoggingHtml());

            #endregion RealUmPerPixel

            CalibratingItem.IsCalibrated = true;
            Guard.IsTrue(Save([CalibratingItem], cancellationToken));

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

            var errorMessageStringBuilder = new StringBuilder();

            foreach (var selectedReviewItem in SelectedReviewItems.OrderBy(t => t.ProductivityInformation))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var title = selectedReviewItem.ProductivityInformation.ToString();

                if (selectedReviewItem.IsCalibrated == false)
                {
                    errorMessageStringBuilder.AppendLine($"{title}: Error");
                    continue;
                }

                Cache.ProductivityInformation = selectedReviewItem.ProductivityInformation;

                Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var detectImageDirectory = ImageFileDirectory;

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
                    Cache.Item.TemplateFilePath,
                    TemplateImage = new HtmlImage(Cache.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    Cache.Item.WaferRadius,
                    Cache.Item.DiePitchWith,
                    Cache.Item.ReticleDieCountX,
                    detectImageDirectory,
                    Cache.Threshold
                }), HtmlLogUniqueId.LoggingHtml());

                Guard.IsTrue(CalibrationAlgorithmService.TryReadTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.Item.TemplateFilePath, out var templateId), nameof(CalibrationAlgorithmService.TryReadTemplate));
                using var _ = templateId;

                var (_, templateImageSize) = ImageHelper.GetImageInfo(Cache.Item.TemplateImageFilePath);

                var waferMapDieBuilder = new WaferMapDieBuilder
                {
                    DiePitchSize = new Size(Cache.Item.DiePitchWith * Cache.Item.ReticleDieCountX, Cache.Item.DiePitchWith * Cache.Item.ReticleDieCountX),
                    OriginalDiePoint = StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition)
                };

                var dies = waferMapDieBuilder.BuildDie(new Circle(Point.Origin, Cache.Item.WaferRadius));

                var currentRowDies = dies
                    .Where(t => t.Index.Y == 0)
                    .OrderBy(t => t.Index.X).ToArray();
                var imageCount = currentRowDies.Length;
                Guard.IsGreaterThan(imageCount, 2);

                var verifyStartPosition = currentRowDies[0].Rect.Point - new Vector(Cache.Item.ImageWidth * selectedReviewItem.XPixelSize / 2d, 0);
                var verifyEndPosition = currentRowDies[^1].Rect.Point + new Vector(Cache.Item.DiePitchWith * Cache.Item.ReticleDieCountX / 2d, 0);

                var verifyDarkFieldRawScanImage = await CIBViewModel.GetPMTImagesAsync(
                    Cache.ProductivityInformation,
                    StageCoordinateSystemEnum.Bright,
                    verifyStartPosition,
                    verifyEndPosition,
                    Cache.Item.CIBInformation,
                    (false, CalChipSiteModelEnum.ChuckModel),
                    (false, Cache.Item.CIBConfiguration),
                    (false, Cache.Item.LaserLightInformation),
                    false,
                    cancellationToken);

                selectedReviewItem.VerifyRawImageFilePath = verifyDarkFieldRawScanImage.RawImageFilePath;

                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition));

                Logger.LogHtmlInformation("Verify", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    selectedReviewItem.VerifyRawImageFilePath,
                    imageCount
                }), HtmlLogUniqueId.LoggingHtml());

#if NET
                await
#endif
                using var fileSteam = File.OpenRead(selectedReviewItem.VerifyRawImageFilePath);
                using var binaryReader = new BinaryReader(fileSteam, Encoding.UTF8, true);

                var (verifySize, bodyBytesStartIndex, bodyBytesLength) = RawImageFactory.GetSize(binaryReader);
                var (_, heightPixel) = (SizeI)verifySize;
                var heightPixelByteLength = heightPixel * 2;

                using var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);

                var imageAllPixelByteLength = Cache.Item.ImageWidth * heightPixelByteLength;
                var verifyStepAllPixelByteLength = Cache.Item.DiePitchWith * Cache.Item.ReticleDieCountX / selectedReviewItem.XPixelSize * heightPixelByteLength;

                Guard.IsEqualTo(MathHelper.SlideCountFull(imageAllPixelByteLength, verifyStepAllPixelByteLength, bodyBytesLength), imageCount);

                var verifyItemItems = new CIBXPixelSizeDTOItem[imageCount];
                foreach (var (index, pointer) in Enumerable
                             .Range(0, imageCount)
                             .Select(t => t * verifyStepAllPixelByteLength)
                             .Select(Convert.ToInt64)
                             .Select(pointer => pointer - pointer % heightPixelByteLength) // verifyStepAllPixelByteLength是double, 不是整数倍, 需要对齐
                             .Index())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    verifyItemItems[index] = GetCIBXPixelSizeSlideItem(
                        pointer,
                        imageAllPixelByteLength,
                        fileSteam,
                        binaryReader,
                        bodyBytesStartIndex,
                        bodyBytesLength,
                        heightPixel,
                        heightPixelByteLength);

                    await ResolveCIBXPixelSizeSlideItemAsync(
                        verifyItemItems[index],
                        templateId,
                        templateImageSize,
                        detectImageDirectory,
                        semaphore,
                        cancellationToken,
                        false).ConfigureAwait(false);

                    if (verifyItemItems[index].IsMatchOk == false) return false;
                }

                selectedReviewItem.VerifyItems = [.. verifyItemItems];

                var verifyXDifferences = selectedReviewItem.VerifyItems
                    .Zip(selectedReviewItem.VerifyItems.Skip(1), (prev, next) => next.MatchPoint.X - prev.MatchPoint.X)
                    .ToArray();
                selectedReviewItem.VerifySplitDifferences = verifyXDifferences;

                var verifyRealUmPerPixel = Cache.Item.DiePitchWith * Cache.Item.ReticleDieCountX / verifyXDifferences.Average();

                var waferDiameter = Cache.Item.WaferRadius * 2d;
                var errorPixel = Math.Abs(waferDiameter / verifyRealUmPerPixel - waferDiameter / selectedReviewItem.XPixelSize);
                var isOk = Math.Abs(verifyXDifferences.Max() - verifyXDifferences.Min()) <= Cache.Threshold
                           && errorPixel <= Cache.Threshold;

                var htmlQuote = new HtmlQuote(new
                {
                    Cache.Threshold,
                    verifyItems = new HtmlPlot2DLinesChart([(string.Empty, [.. selectedReviewItem.VerifyItems.Select(t => t.MatchPoint)])], string.Empty),
                    verifyXDifferences = new HtmlPlot2DLinesChart([(string.Empty, [.. verifyXDifferences.Index().Select(t => new Point(t.Index, t.Item))])], string.Empty),
                    CalibratedXPixelSize = selectedReviewItem.XPixelSize,
                    verifyRealUmPerPixel,
                    errorPixel = $"({errorPixel:0.###}px)/({waferDiameter:0.###}um)"
                });

                if (isOk)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlQuote, HtmlLogUniqueId.LoggingHtml());
                else
                {
                    errorMessageStringBuilder.AppendLine($"{title}: Error");
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlQuote, HtmlLogUniqueId.LoggingHtml());
                }

                if (isOk) selectedReviewItem.XPixelSize = verifyRealUmPerPixel;
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

    #region Item

    private CIBXPixelSizeDTOItem GetCIBXPixelSizeSlideItem(
        long pointer,
        int allPixelByteLength,
        FileStream fileSteam,
        BinaryReader binaryReader,
        long bodyBytesStartIndex,
        long bodyBytesLength,
        int heightPixel,
        int heightPixelByteLength)
    {
        var isEnd = pointer + allPixelByteLength > bodyBytesLength;
        var currentImageAllPixelByteLength = isEnd
            ? Convert.ToInt32(bodyBytesLength - pointer)
            : allPixelByteLength;
        if (isEnd)
        {
            Guard.IsGreaterThan(currentImageAllPixelByteLength, 0);
            Guard.IsEqualTo(currentImageAllPixelByteLength % heightPixelByteLength, 0);
        }

        var buffer = ArrayPool<byte>.Shared.Rent(currentImageAllPixelByteLength);

        fileSteam.Seek(bodyBytesStartIndex + pointer, SeekOrigin.Begin);
        Guard.IsEqualTo(binaryReader.Read(buffer, 0, currentImageAllPixelByteLength), currentImageAllPixelByteLength);

        return new CIBXPixelSizeDTOItem { StartPixel = pointer / heightPixelByteLength, Buffer = buffer, SizeI = new SizeI(currentImageAllPixelByteLength / heightPixelByteLength, heightPixel) };
    }

    private async Task ResolveCIBXPixelSizeSlideItemAsync(
        CIBXPixelSizeDTOItem itemItem,
        HTuple templateId,
        Size templateImageSize,
        string detectImageDirectory,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken,
        bool isOkLog = true)
    {
        var startPixel = itemItem.StartPixel;
        var buffer = itemItem.Buffer;
        var sizeI = itemItem.SizeI;

        try
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            using var image = TempRawImageFactory.CreateImage(buffer, sizeI);

            var isMathOk = CalibrationAlgorithmService.TryTemplateMatchToOffset(Cache.Item.AlgorithmTemplateTypeEnum, image, templateId, out var matchPoint, out _, out var score, out _);

            itemItem.IsMatchOk = isMathOk;
            itemItem.MatchPoint = new Point(itemItem.IsMatchOk ? startPixel + matchPoint.X : startPixel, matchPoint.Y);
            itemItem.Score = score;

            if (isOkLog == false || itemItem.IsMatchOk)
            {
                var title = itemItem.IsMatchOk ? $"{itemItem.MatchPoint.X:0.###}px" : $"{startPixel}px";

                itemItem.ImageFilePath = Path.Combine(detectImageDirectory, Path.GetFileNameWithoutExtension(Cache.Item.TemplateImageFilePath), $"{startPixel}_{Guid.NewGuid():N}.jpg");
                image.Save(itemItem.ImageFilePath);

                var bullet = new HtmlBullet(new
                {
                    currentMatchPoint = matchPoint,
                    itemItem.StartPixel,
                    itemItem.SizeI,
                    itemItem.MatchPoint,
                    itemItem.Score,
                    DeltaOfCenter = (itemItem.StartPixel + itemItem.SizeI.Width / 2d) - itemItem.MatchPoint.X,
                    HtmlTab = new HtmlTab(new
                    {
                        OriginImage = new HtmlImage(itemItem.ImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(matchPoint, templateImageSize), new HtmlImageRectangleOverlay(matchPoint, templateImageSize)]),
                        TemplateImage = new HtmlImage(Cache.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                });

                if (itemItem.IsMatchOk)
                    Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header6, bullet, HtmlLogUniqueId.LoggingHtml());
                else
                    Logger.LogHtmlError(title, HtmlHeaderLevelEnum.Header6, bullet, HtmlLogUniqueId.LoggingHtml());
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            semaphore.Release();
        }
    }

    #endregion Item

    private bool Save(IReadOnlyList<CIBXPixelSizeDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
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

    #endregion 校准
}