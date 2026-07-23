using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.CIB.XPixelSize;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using Core.Utilities;
using Core.Utilities.SourceGenerators.Attributes;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using HalconDotNet;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WaferMap.WPF.Primitives.Builders;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

namespace CugaCalibration.ViewModels.CIB;

[IOCAppService(ServiceType = typeof(CIBXPixelSizeViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBXPixelSizeViewModel : CalibrationViewModelBase<CIBXPixelSizeCache>
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "Image Param" },
        new() { StepName = "Alignment" },
        new() { StepName = "Find Position" },
        new() { StepName = "X Pixel Size" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial CIBXPixelSizeDTO CalibratingItem { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<ProductivityInformationStatus> CalibratingStatuses { get; set; } = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    public partial IReadOnlyList<CIBXPixelSizeDTO> Reviews { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<CIBXPixelSizeDTO> SelectedReviewItems { get; set; } = [];

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    public partial CreateDarkImageTemplateWindowViewModel CreateDarkImageTemplateWindowViewModel { get; set; } = HostApplication.GetRequiredService<CreateDarkImageTemplateWindowViewModel>();

    [ObservableProperty]
    public partial AlignmentUserControlViewModel AlignmentUserControlViewModel { get; set; } = HostApplication.GetRequiredService<AlignmentUserControlViewModel>();

    [RecipeCache]
    [ObservableProperty]
    public override partial CIBXPixelSizeCache Cache { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCalChipCache MicroscopeCalChipCache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial CIBXPixelSizeDTO[] Calibrations { get; set; } = [];

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);


        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);
        MicroscopeCalChipCache = ApplicationCookieService.GetCache<MicroscopeCalChipCache>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<CIBXPixelSizeCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<CIBXPixelSizeDTO>(cancellationToken);

        UpdateEntryStatus(Unsafe.As<CalibrationDTOBase[]>(Calibrations), cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

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
            case 0:
                CalibratingItem = new CIBXPixelSizeDTO();

                return true;

            case 1:
                return true;

            case 2:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);

                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.CalChipSiteModelEnum switch
                    {
                        CalChipSiteModelEnum.ChuckModel => Cache.Item.FindBFMachinePosition,
                        CalChipSiteModelEnum.DswModel => MicroscopeCalChip.DSWBrightFieldMachineAffinePosition,
                        _ => ThrowHelper.ThrowNotSupportedException<Point>("Current CalChip Mode Is Not Supported!")
                    }), Cache.CalChipSiteModelEnum);

                return true;

            case 3:

                return true;

            case 4:
                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

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
        return InvokeCalibrateAsync(async () =>
        {
            Guard.IsEqualTo(Cache.Item.MicroscopeLensInformation, MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(Cache.Item.AlignmentResult.ToHtmlAnonymous()),
                Cache.Item.ImageWidth
            }), HtmlLogUniqueId.LoggingHtml());

            Cache.Item.FindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            var darkFieldImageDto = await CIBViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Bright,
                StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition),
                Cache.Item.ImageWidth,
                Cache.Item.CIBInformation,
                (false, Cache.CalChipSiteModelEnum),
                (false, Cache.Item.OpticsConfiguration),
                (false, Cache.Item.CIBConfiguration),
                (false, Cache.Item.LaserLightInformation),
                false,
                cancellationToken);

            using var _ = darkFieldImageDto;

            var originImageFilePath = Path.Combine(TemplateFileDirectory, Cache.Item.MicroscopeLensInformation.ToString(), $"{Guid.NewGuid():N}.jpg");
            Cache.Item.TemplateFilePath = $"{originImageFilePath}_Template";
            darkFieldImageDto.Image.SaveImage(originImageFilePath);

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
                Cache.Item.CIBInformation,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
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
            var templateMatchScoreThreshold = Cache.AlgorithmTemplateTypeEnum.ToTemplateMatchScoreThreshold(CalibrationSetting);

            var waferMapDieBuilder = new WaferMapDieBuilder
            {
                DiePitchSize = new Size(Cache.Item.DiePitchWith * Cache.Item.ReticleDieCountX, Cache.Item.DiePitchWith * Cache.Item.ReticleDieCountX),
                OriginalDiePoint = StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition)
            };

            var dies = waferMapDieBuilder.BuildDie(new Circle(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition), Cache.Item.WaferRadius));

            var currentRowDies = dies
                .Where(t => t.Index.Y == 0)
                .OrderBy(t => t.Index.X).ToArray();
            var imageCount = currentRowDies.Length;
            Guard.IsGreaterThan(imageCount, 2);

            var startPosition = currentRowDies[0].Rect.Point - new Vector(Cache.Item.DiePitchWith * Cache.Item.ReticleDieCountX / 2d, 0);
            var endPosition = currentRowDies[^1].Rect.Point + new Vector(Cache.Item.DiePitchWith * Cache.Item.ReticleDieCountX / 2d, 0);

            var darkFieldRawScanImage = await CIBViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Bright,
                startPosition,
                endPosition,
                Cache.Item.CIBInformation,
                (false, Cache.CalChipSiteModelEnum),
                (false, Cache.Item.OpticsConfiguration),
                (false, Cache.Item.CIBConfiguration),
                (false, Cache.Item.LaserLightInformation),
                false,
                cancellationToken);

            StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition), Cache.CalChipSiteModelEnum);

            CalibratingItem.RawImageFilePath = darkFieldRawScanImage.RawImageFilePath;

            Logger.LogHtmlInformation("Split", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                imageCount,
                CalibratingItem.RawImageFilePath
            }), HtmlLogUniqueId.LoggingHtml());

#if NET
            await
#endif
            using var fileSteam = File.OpenRead(CalibratingItem.RawImageFilePath);
            using var binaryReader = new BinaryReader(fileSteam, Encoding.UTF8, true);

            var (size, bodyBytesStartIndex, bodyBytesLength) = RAWImageFactory.GetSize(binaryReader);
            var (_, heightPixel) = size;
            var heightPixelByteLength = heightPixel * 2;

            #region RealUmPerPixel

            var windowWidthSize = templateImageSize.Width * 3;
            var windowWidthStep = templateImageSize.Width;
            var windowImageAllPixelByteLength = windowWidthSize * heightPixelByteLength;
            var windowStepAllPixelByteLength = windowWidthStep * heightPixelByteLength;
            var totalCount = Math.SlideCount(windowImageAllPixelByteLength, windowStepAllPixelByteLength, bodyBytesLength);

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
                            templateMatchScoreThreshold,
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

            Logger.LogHtmlInformation("Matches", HtmlHeaderLevelEnum.Header3, new HtmlPlot2DLinesChart(
            [
                ("All",
                [
                    .. CalibratingItem.SlideItems
                        .OrderBy(t => t.MatchPoint.X)
                        .Select(t => new Point(t.MatchPoint.X, t.Score))
                ], string.Empty)
            ], string.Empty), HtmlLogUniqueId.LoggingHtml());

            var matches = CalibratingItem.SlideItems
                .Select(t => (ScorePoint: new Point(t.MatchPoint.X, t.Score), t.MatchPoint, t.IsMatchOk))
                .GroupBy(t => t.ScorePoint.X)
                .Select(g => g.MaxBy(t => t.ScorePoint.Y))
                .OrderBy(t => t.ScorePoint.X)
                .ToArray();

            var (indexes, _) = Extremumor.FindMaxima([.. matches.Select(t => t.ScorePoint)]);
            var filterIndexes = indexes.Where(t => matches[t].IsMatchOk).ToArray();
            filterIndexes = Filter.NMS([.. filterIndexes.Select(t => matches[t].ScorePoint)], templateImageSize.Width)
                .Indexes
                .Select(t => filterIndexes[t])
                .ToArray();
            filterIndexes = filterIndexes
                .OrderByDescending(t => matches[t].ScorePoint.Y)
                .Take(imageCount)
                .OrderBy(t => matches[t].ScorePoint.X)
                .ToArray();

            var matchPoints = filterIndexes.Select(t => matches[t].MatchPoint).ToArray();

            var xDifferences = matchPoints
                .Zip(matchPoints.Skip(1), (prev, next) => next.X - prev.X)
                .ToArray();
            CalibratingItem.SlideSplitDifferences = Filter.MAD(xDifferences).Results;

            var isOk = matchPoints.Length >= 2 && CalibratingItem.SlideSplitDifferences.Count >= 1;

            var htmlAnonymous = new
            {
                Score = new HtmlPlot2DLinesChart([
                    ("All", [.. matches.Select(t => t.ScorePoint)], string.Empty),
                    ("Maxima", [.. indexes.Select(t => matches[t].ScorePoint)], MarkerShape.FilledTriangleDown.ToPlotJsMarker()),
                    ("Filter Maxima", [.. filterIndexes.Select(t => matches[t].ScorePoint)], MarkerShape.Asterisk.ToPlotJsMarker())
                ], string.Empty),
                MatchPoints = new HtmlPlot2DLinesChart([(string.Empty, matchPoints)], string.Empty),
                XDifferences = new HtmlPlot2DLinesChart([(string.Empty, xDifferences.ToPoints())], string.Empty),
                XFilterDifferences = new HtmlPlot2DLinesChart([(string.Empty, CalibratingItem.SlideSplitDifferences.ToPoints())], string.Empty)
            };

            if (isOk == false)
            {
                Logger.LogHtmlError("Error: Match Count < 2", HtmlHeaderLevelEnum.Header3, new HtmlBullet(htmlAnonymous), HtmlLogUniqueId.LoggingHtml());

                return false;
            }

            CalibratingItem.XPixelSize = Cache.Item.DiePitchWith * Cache.Item.ReticleDieCountX / CalibratingItem.SlideSplitDifferences.Average();
            CalibratingItem.XPixelSizeDelta = CalibratingItem.SlideSplitDifferences.Max() - CalibratingItem.SlideSplitDifferences.Min();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                CalibratingItem.XPixelSize,
                CalibratingItem.XPixelSizeDelta,
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

                AlignmentUserControlViewModel.IsDarkFieldAlignment = Cache.Item.IsDarkFieldAlignment;
                AlignmentUserControlViewModel.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum;
                AlignmentUserControlViewModel.ProductivityInformation = Cache.ProductivityInformation;
                await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

                Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var detectImageDirectory = ImageFileDirectory;

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
                var templateMatchScoreThreshold = Cache.AlgorithmTemplateTypeEnum.ToTemplateMatchScoreThreshold(CalibrationSetting);

                var waferMapDieBuilder = new WaferMapDieBuilder
                {
                    DiePitchSize = new Size(Cache.Item.DiePitchWith * Cache.Item.ReticleDieCountX, Cache.Item.DiePitchWith * Cache.Item.ReticleDieCountX),
                    OriginalDiePoint = StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition)
                };

                var dies = waferMapDieBuilder.BuildDie(new Circle(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition), Cache.Item.WaferRadius));

                var currentRowDies = dies
                    .Where(t => t.Index.Y == 0)
                    .OrderBy(t => t.Index.X).ToArray();
                var imageCount = currentRowDies.Length;
                Guard.IsGreaterThan(imageCount, 2);

                var verifyStartPosition = currentRowDies[0].Rect.Point - new Vector(Cache.Item.ImageWidth * selectedReviewItem.XPixelSize / 2d, 0);
                var verifyEndPosition = currentRowDies[^1].Rect.Point + new Vector(Cache.Item.DiePitchWith * Cache.Item.ReticleDieCountX / 2d, 0);

                var verifyDarkFieldRawScanImage = await CIBViewModel.GetPMTImageAsync(
                    Cache.ProductivityInformation,
                    StageCoordinateSystemEnum.Bright,
                    verifyStartPosition,
                    verifyEndPosition,
                    Cache.Item.CIBInformation,
                    (false, Cache.CalChipSiteModelEnum),
                    (false, Cache.Item.OpticsConfiguration),
                    (false, Cache.Item.CIBConfiguration),
                    (false, Cache.Item.LaserLightInformation),
                    false,
                    cancellationToken);

                selectedReviewItem.VerifyRawImageFilePath = verifyDarkFieldRawScanImage.RawImageFilePath;

                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition), Cache.CalChipSiteModelEnum);

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

                var (verifySize, bodyBytesStartIndex, bodyBytesLength) = RAWImageFactory.GetSize(binaryReader);
                var (_, heightPixel) = verifySize;
                var heightPixelByteLength = heightPixel * 2;

                using var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);

                var imageAllPixelByteLength = Cache.Item.ImageWidth * heightPixelByteLength;
                var verifyStepAllPixelByteLength = Cache.Item.DiePitchWith * Cache.Item.ReticleDieCountX / selectedReviewItem.XPixelSize * heightPixelByteLength;

                var slideCount = Math.SlideCountFull(imageAllPixelByteLength, verifyStepAllPixelByteLength, bodyBytesLength);
                Guard.IsLessThanOrEqualTo(slideCount, imageCount);
                Guard.IsGreaterThanOrEqualTo(slideCount, 2);

                var verifyItemItems = new CIBXPixelSizeDTOItem[slideCount];
                foreach (var (index, pointer) in Enumerable
                             .Range(0, slideCount)
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
                        templateMatchScoreThreshold,
                        detectImageDirectory,
                        semaphore,
                        cancellationToken,
                        false).ConfigureAwait(false);

                    if (verifyItemItems[index].IsMatchOk == false) return false;
                }

                selectedReviewItem.VerifyItems = [.. verifyItemItems];

                selectedReviewItem.VerifySplitDifferences = selectedReviewItem.VerifyItems
                    .Zip(selectedReviewItem.VerifyItems.Skip(1), (prev, next) => next.MatchPoint.X - prev.MatchPoint.X)
                    .ToArray();

                var verifyRealUmPerPixel = Cache.Item.DiePitchWith * Cache.Item.ReticleDieCountX / selectedReviewItem.VerifySplitDifferences.Average();

                var waferDiameter = Cache.Item.WaferRadius * 2d;
                var distancePixel = Math.Abs(selectedReviewItem.VerifySplitDifferences.Max() - selectedReviewItem.VerifySplitDifferences.Min());
                var errorPixel = Math.Abs(waferDiameter / verifyRealUmPerPixel - waferDiameter / selectedReviewItem.XPixelSize);
                var isOk = distancePixel <= Cache.Threshold && errorPixel <= Cache.Threshold;

                var htmlQuote = new HtmlQuote(new
                {
                    Cache.Threshold,
                    Score = new HtmlPlot2DLinesChart([(string.Empty, [.. selectedReviewItem.VerifyItems.Select(t => new Point(t.MatchPoint.X, t.Score))], string.Empty)], string.Empty),
                    verifyItems = new HtmlPlot2DLinesChart([(string.Empty, [.. selectedReviewItem.VerifyItems.Select(t => t.MatchPoint)])], string.Empty),
                    verifyXDifferences = new HtmlPlot2DLinesChart([(string.Empty, selectedReviewItem.VerifySplitDifferences.ToPoints())], string.Empty),
                    CalibratedXPixelSize = selectedReviewItem.XPixelSize,
                    verifyRealUmPerPixel,
                    errorPixel = $"({errorPixel:0.###}px)/({waferDiameter:0.###}um)"
                });

                if (isOk)
                {
                    selectedReviewItem.XPixelSize = verifyRealUmPerPixel;
                    selectedReviewItem.XPixelSizeDelta = selectedReviewItem.VerifySplitDifferences.Max() - selectedReviewItem.VerifySplitDifferences.Min();
                    CIBViewModel.SetXPixelSize(selectedReviewItem.ProductivityInformation, selectedReviewItem.XPixelSize);

                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlQuote, HtmlLogUniqueId.LoggingHtml());
                }
                else
                {
                    errorMessageStringBuilder.AppendLine($"{title}: Error ({distancePixel:0.###} or {errorPixel:0.###}) > {Cache.Threshold:0.###}");
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
        double templateMatchScoreThreshold,
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

            using var resultImage = RAWImageFactory.CreateImage(buffer, sizeI, Cache.Item.CIBConfiguration.CIBProfileMode == CIBProfileModeEnum.PMTLog);
            using var bitmapImage = resultImage.ToBitmapImage();
            var isMathOk = CalibrationAlgorithmService.TryTemplateMatchToOffset(Cache.Item.AlgorithmTemplateTypeEnum, bitmapImage, templateId, out var matchPoint, out _, out var score, out _);

            itemItem.IsMatchOk = isMathOk;
            itemItem.MatchPoint = new Point(itemItem.IsMatchOk ? startPixel + matchPoint.X : startPixel, matchPoint.Y);
            itemItem.Score = score;

            if (isOkLog == false || itemItem.IsMatchOk)
            {
                var title = itemItem.IsMatchOk ? $"{itemItem.MatchPoint.X:0.###}px" : $"{startPixel}px";

                itemItem.ImageFilePath = Path.Combine(detectImageDirectory, Path.GetFileNameWithoutExtension(Cache.Item.TemplateImageFilePath), $"{startPixel}_{Guid.NewGuid():N}.jpg");
                resultImage.Save(itemItem.ImageFilePath);

                var bullet = new HtmlBullet(new
                {
                    templateMatchScoreThreshold,
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
                dto,
                .. Calibrations.Where(t => t.ProductivityInformation != dto.ProductivityInformation)
            ];
        }

        ApplicationCookieService.SetCalibrations(Calibrations, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var temps = Guard.IsAssignableToTypeAndReturn<CIBXPixelSizeDTO[]>(calibrations);
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
}