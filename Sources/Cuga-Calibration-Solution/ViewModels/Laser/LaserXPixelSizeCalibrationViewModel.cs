using System.Buffers;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Ads.PressureGains;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.XPixelSize;
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
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Channels;
using Core.Models.Models.Common.Alignment;
using Core.Utilities;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WaferMap.WPF.Primitives.Builders;
using Net.Utilities.WPF.MVVM;
using ScottPlot;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserXPixelSizeCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserXPixelSizeCalibrationViewModel : CalibrationViewModelBase
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
    private LaserXPixelSizeItemDto _calibratingItem = new();

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationCalibrationStatus> _calibrationStatuses = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ObservableCollection<LaserXPixelSizeItemDto> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<LaserXPixelSizeItemDto> _selectedReviewItems = [];

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private AlignmentCacheBrightField _alignmentCacheBrightField = new();

    [ObservableProperty]
    private AlignmentCacheDarkField _alignmentCacheDarkField = new();

    [ObservableProperty]
    private CreateDarkImageTemplateWindowViewModel _createDarkImageTemplateWindowViewModel = HostApplication.GetRequiredService<CreateDarkImageTemplateWindowViewModel>();

    [ObservableProperty]
    private AlignmentWindowBrightFieldViewModel _alignmentWindowBrightFieldViewModel = HostApplication.GetRequiredService<AlignmentWindowBrightFieldViewModel>();

    [ObservableProperty]
    private AlignmentWindowDarkFieldViewModel _alignmentWindowDarkFieldViewModel = HostApplication.GetRequiredService<AlignmentWindowDarkFieldViewModel>();

    [ObservableProperty]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

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

        ScatterPlotControl.Configure();

        AlignmentCacheDarkField = RecipeCacheProvider.GetOrDefault<AlignmentCacheDarkField>();
        AlignmentCacheBrightField = RecipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();

        if (CalibrationStatuses.Count == 0)
            CalibrationStatuses =
            [
                .. ApplicationCookie.OpticsMagTypeProductivityInformations.Select(t => new ProductivityInformationCalibrationStatus { ProductivityInformation = t, IsCalibrated = false })
            ];

        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<LaserXPixelSizeCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<LaserXPixelSizeItemDto>();

        Calibrations =
        [
            ..Calibrations.Where(t => ApplicationCookie.ProductivityInformations.Contains(t.ProductivityInformation))
                .Select(t =>
                {
                    CalibrationStatuses.Single(tt => tt.ProductivityInformation == t.ProductivityInformation).IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        CalibratingItem = new LaserXPixelSizeItemDto();

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

        return Reviews.Any(t => t.IsCalibrated);
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
                return true;

            case 2:
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition));

                return true;

            case 3:

                return true;

            case 4:
                CalibrationStatuses.Single(t => t.ProductivityInformation == Cache.ProductivityInformation).IsCalibrated = true;
                DialogWindowProvider.ShowDialog($"X Pixel Size {Cache.ProductivityInformation} Ok!");

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
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            CalibratingItem = new LaserXPixelSizeItemDto
            {
                ProductivityInformation = Cache.ProductivityInformation
            };

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.ProductivityInformations.Contains(Cache.ProductivityInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);
            MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);

            CalibratingItem.MicroscopeLensInformation = Cache.Item.MicroscopeLensInformation;
            CalibratingItem.PMTId = Cache.Item.PMTId;
            CalibratingItem.ChannelId = Cache.Item.ChannelId;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.PMTId,
                Cache.Item.ChannelId
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.Item.MicroscopeLensInformation)
                       && ApplicationCookie.LaserLightInformations.Contains(Cache.Item.LaserLightInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step2CalibrateActionAsync(CancellationToken cancellationToken)
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
                if (AlignmentCacheDarkField.IsOk)
                    alignmentResult = StageViewModel.AlignmentDarkField(
                        AlignmentCacheDarkField.LowSite1,
                        AlignmentCacheDarkField.LowSite2,
                        AlignmentCacheDarkField.HighSite1,
                        AlignmentCacheDarkField.HighSite2,
                        AlignmentCacheDarkField.HighDarkFieldOpticsMagTypeEnum,
                        AlignmentCacheDarkField.HighDarkFieldStageSpeedEnum,
                        AlignmentCacheDarkField.LowMag,
                        AlignmentCacheDarkField.AlgorithmWaferTypeEnum);
                else
                {
                    var alignmentWindowDarkFieldViewModel = AlignmentWindowDarkFieldViewModel;
                    Guard.IsTrue(WindowManagerService.ShowDialog(alignmentWindowDarkFieldViewModel) == true, nameof(alignmentWindowDarkFieldViewModel));
                    AlignmentCacheDarkField = alignmentWindowDarkFieldViewModel.Cache;
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

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Item.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(Cache.Item.AlignmentResult.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.PMTId,
                Cache.Item.ChannelId,
                Cache.Item.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(Cache.Item.AlignmentResult.ToHtmlAnonymous()),
                Cache.Item.WidthPixel
            }), HtmlLogUniqueId.LoggingHtml());

            Cache.Item.FindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            var darkFieldImageDto = LaserViewModel.GetDarkFieldLineScanImage(
                CalChipSiteModelEnum.ChuckModel,
                StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition),
                (false, Cache.Item.LaserLightInformation),
                false,
                Cache.Item.CIBConfiguration,
                Cache.ProductivityInformation,
                xWidthPixel: Cache.Item.WidthPixel,
                pmtId: Cache.Item.PMTId,
                channelId: Cache.Item.ChannelId,
                stageCoordinateSystemEnum: StageCoordinateSystemEnum.Bright);

            using var _ = darkFieldImageDto;

            var originImageFilePath = Path.Combine(TemplateFileDirectory, Cache.ProductivityInformation.ToString(), Cache.Item.MicroscopeLensInformation.ToString(), $"{Guid.NewGuid():N}.jpg");
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
    private Task<bool> Step4CalibrateActionAsync(CancellationToken cancellationToken)
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
                Cache.Item.PMTId,
                Cache.Item.ChannelId,
                Cache.Item.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(Cache.Item.AlignmentResult.ToHtmlAnonymous()),
                Cache.Item.WidthPixel,
                Cache.AlgorithmTemplateTypeEnum,
                Cache.AlgorithmTemplateSizeEnum,
                Cache.Item.FindBFMachinePosition,
                Cache.Item.TemplateFilePath,
                TemplateImage = new HtmlImage(Cache.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                Cache.Item.WaferDiameter,
                Cache.Item.ColumnCellWidth,
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            Guard.IsTrue(CalibrationAlgorithmService.TryReadTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.Item.TemplateFilePath, out var templateId), nameof(CalibrationAlgorithmService.TryReadTemplate));
            using var _ = templateId;

            var (_, templateImageSize) = ImageHelper.GetImageInfo(Cache.Item.TemplateImageFilePath);

            var waferMapDieBuilder = new WaferMapDieBuilder
            {
                DiePitchSize = new Size(Cache.Item.ColumnCellWidth, Cache.Item.ColumnCellWidth),
                OriginalDiePoint = StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition)
            };

            var dies = waferMapDieBuilder.BuildDie(new Circle(Point.Origin, Cache.Item.WaferDiameter / 2d));

            var currentRowDies = dies
                .Where(t => t.Index.Y == 0)
                .OrderBy(t => t.Index.X).ToArray();
            var imageCount = currentRowDies.Length;
            Guard.IsGreaterThan(imageCount, 2);

            var startPosition = currentRowDies[0].Rect.Point - new Vector(Cache.Item.ColumnCellWidth / 2d, 0);
            var endPosition = currentRowDies[^1].Rect.Point + new Vector(Cache.Item.ColumnCellWidth / 2d, 0);

            var darkFieldLineScanImage = LaserViewModel.GetDarkFieldLineScanImage(
                CalChipSiteModelEnum.ChuckModel,
                startPosition,
                endPosition,
                (false, Cache.Item.LaserLightInformation),
                false,
                Cache.Item.CIBConfiguration,
                Cache.ProductivityInformation,
                xWidthPixel: Cache.Item.WidthPixel,
                pmtId: Cache.Item.PMTId,
                channelId: Cache.Item.ChannelId,
                stageCoordinateSystemEnum: StageCoordinateSystemEnum.Bright);
            var rawImageFilePath = darkFieldLineScanImage.Url;

            Logger.LogHtmlInformation("Split", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new { rawImageFilePath, }), HtmlLogUniqueId.LoggingHtml());

            using var fileSteam = File.OpenRead(rawImageFilePath);
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

            var items = new Item[totalCount];

            #region Channel

            using var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
            var channel = Channel.CreateBounded<Item>(new BoundedChannelOptions(totalCount) { SingleReader = true, SingleWriter = true, AllowSynchronousContinuations = true });

            #region Reader

            // ReSharper disable AccessToDisposedClosure

            var tasks = new Task[totalCount];
            var channelReaderTask = Task.Run(async () =>
            {
                var index = 0;
                await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    try
                    {
                        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                        tasks[index] = ResolveAsync(
                            item,
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

            #endregion

            #region Writer

            foreach (var (index, pointer) in Enumerable
                         .Range(0, totalCount)
                         .Select(t => (long)t * windowStepAllPixelByteLength).Index())
            {
                items[index] = GetItem(
                    pointer,
                    windowImageAllPixelByteLength,
                    fileSteam,
                    binaryReader,
                    bodyBytesStartIndex,
                    bodyBytesLength,
                    heightPixel,
                    heightPixelByteLength);

                await channel.Writer.WriteAsync(items[index], cancellationToken).ConfigureAwait(false);
            }

            channel.Writer.Complete();

            #endregion

            #endregion

            await channelReaderTask.ConfigureAwait(false);
            await Task.WhenAll(tasks).ConfigureAwait(false);

            CalibratingItem.SlideItems = [.. items.Select(t => (t.MatchPoint, t.Score, t.IsMatchOk))];
            Refresh(CalibratingItem);

            var matchPoints = CalibratingItem.SlideItems.Where(t => t.IsMatchOk).Select(t => t.MatchPoint).ToArray();

            var xDifferences = matchPoints
                .Zip(matchPoints.Skip(1), (prev, next) => next.X - prev.X)
                .ToArray();
            var average = xDifferences.Average();
            var xFilterDifferences = xDifferences.Where(t => t >= average).ToArray();
            Guard.IsEqualTo(xFilterDifferences.Length, imageCount - 1);
            CalibratingItem.XPixelSize = Cache.Item.ColumnCellWidth / xFilterDifferences.Average();

            Logger.LogHtmlInformation("1.3. Split Result", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                AllScore = new HtmlPlot2DLinesChart([(string.Empty, [.. CalibratingItem.SlideItems.Select(t => new Point(t.MatchPoint.X, t.Score))])], string.Empty),
                matchPoints = new HtmlPlot2DLinesChart([(string.Empty, matchPoints)], string.Empty),
                xDifferences = new HtmlPlot2DLinesChart([(string.Empty, [.. xDifferences.Index().Select(t => new Point(t.Index, t.Item))])], string.Empty),
                xFilterDifferences = new HtmlPlot2DLinesChart([(string.Empty, [.. xFilterDifferences.Index().Select(t => new Point(t.Index, t.Item))])], string.Empty),
                CalibratingItem.XPixelSize
            }), HtmlLogUniqueId.LoggingHtml());

            #endregion

            var verifyStartPosition = currentRowDies[0].Rect.Point - new Vector(Cache.Item.WidthPixel * CalibratingItem.XPixelSize / 2d, 0);
            var verifyEndPosition = currentRowDies[^1].Rect.Point + new Vector(Cache.Item.ColumnCellWidth / 2d, 0);

            var verifyDarkFieldLineScanImage = LaserViewModel.GetDarkFieldLineScanImage(
                CalChipSiteModelEnum.ChuckModel,
                verifyStartPosition,
                verifyEndPosition,
                (false, Cache.Item.LaserLightInformation),
                false,
                Cache.Item.CIBConfiguration,
                Cache.ProductivityInformation,
                xWidthPixel: Cache.Item.WidthPixel,
                pmtId: Cache.Item.PMTId,
                channelId: Cache.Item.ChannelId,
                stageCoordinateSystemEnum: StageCoordinateSystemEnum.Bright);
            var verifyRawImageFilePath = verifyDarkFieldLineScanImage.Url;

            StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition));

            Logger.LogHtmlInformation("Verify", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                verifyRawImageFilePath,
                Cache.Item.WidthPixel,
                imageCount
            }), HtmlLogUniqueId.LoggingHtml());

            using var verifyFileSteam = File.OpenRead(verifyRawImageFilePath);
            using var verifyBinaryReader = new BinaryReader(verifyFileSteam, Encoding.UTF8, true);

            var (verifySize, verifyBodyBytesStartIndex, verifyBodyBytesLength) = RawImageFactory.GetSize(verifyBinaryReader);
            var (_, verifyHeightPixel) = (SizeI)verifySize;
            var verifyHeightPixelByteLength = verifyHeightPixel * 2;

            var isOk = await VerifyAsync(
                CalibratingItem,
                verifyFileSteam,
                verifyBinaryReader,
                verifyBodyBytesStartIndex,
                verifyBodyBytesLength,
                verifyHeightPixel,
                verifyHeightPixel,
                imageCount,
                templateId,
                templateImageSize,
                detectImageDirectory,
                semaphore,
                cancellationToken,
                false).ConfigureAwait(false);

            if (isOk == false) return false;

            CalibratingItem.IsCalibrated = true;
            Guard.IsTrue(Save(CalibratingItem, cancellationToken));

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(async () =>
        {
            if (SelectedReviewItems.Count == 0)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            foreach (var item in SelectedReviewItems)
            {
                Cache.ProductivityInformation = item.ProductivityInformation;

                Logger.LogHtmlInformation(item.ProductivityInformation.ToString(), HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var detectImageDirectory = ImageFileDirectory;
                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    Cache.ProductivityInformation,
                    Cache.Item.MicroscopeLensInformation,
                    Cache.Item.LaserLightInformation,
                    CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                    Cache.Item.PMTId,
                    Cache.Item.ChannelId,
                    Cache.Item.IsDarkFieldAlignment,
                    AlignmentResult = new HtmlQuote(Cache.Item.AlignmentResult.ToHtmlAnonymous()),
                    Cache.Item.WidthPixel,
                    Cache.AlgorithmTemplateTypeEnum,
                    Cache.AlgorithmTemplateSizeEnum,
                    Cache.Item.FindBFMachinePosition,
                    Cache.Item.TemplateFilePath,
                    TemplateImage = new HtmlImage(Cache.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    Cache.Item.WaferDiameter,
                    Cache.Item.ColumnCellWidth,
                    detectImageDirectory
                }), HtmlLogUniqueId.LoggingHtml());

                Guard.IsTrue(CalibrationAlgorithmService.TryReadTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.Item.TemplateFilePath, out var templateId), nameof(CalibrationAlgorithmService.TryReadTemplate));
                using var _ = templateId;

                var (_, templateImageSize) = ImageHelper.GetImageInfo(Cache.Item.TemplateImageFilePath);

                var waferMapDieBuilder = new WaferMapDieBuilder
                {
                    DiePitchSize = new Size(Cache.Item.ColumnCellWidth, Cache.Item.ColumnCellWidth),
                    OriginalDiePoint = StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition)
                };

                var dies = waferMapDieBuilder.BuildDie(new Circle(Point.Origin, Cache.Item.WaferDiameter / 2d));

                var currentRowDies = dies
                    .Where(t => t.Index.Y == 0)
                    .OrderBy(t => t.Index.X).ToArray();
                var imageCount = currentRowDies.Length;
                Guard.IsGreaterThan(imageCount, 2);

                var verifyStartPosition = currentRowDies[0].Rect.Point - new Vector(Cache.Item.WidthPixel * CalibratingItem.XPixelSize / 2d, 0);
                var verifyEndPosition = currentRowDies[^1].Rect.Point + new Vector(Cache.Item.ColumnCellWidth / 2d, 0);

                var verifyDarkFieldLineScanImage = LaserViewModel.GetDarkFieldLineScanImage(
                    CalChipSiteModelEnum.ChuckModel,
                    verifyStartPosition,
                    verifyEndPosition,
                    (false, Cache.Item.LaserLightInformation),
                    false,
                    Cache.Item.CIBConfiguration,
                    Cache.ProductivityInformation,
                    xWidthPixel: Cache.Item.WidthPixel,
                    pmtId: Cache.Item.PMTId,
                    channelId: Cache.Item.ChannelId,
                    stageCoordinateSystemEnum: StageCoordinateSystemEnum.Bright);
                var verifyRawImageFilePath = verifyDarkFieldLineScanImage.Url;

                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition));

                Logger.LogHtmlInformation("Verify", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    verifyRawImageFilePath,
                    Cache.Item.WidthPixel,
                    imageCount
                }), HtmlLogUniqueId.LoggingHtml());

                using var verifyFileSteam = File.OpenRead(verifyRawImageFilePath);
                using var verifyBinaryReader = new BinaryReader(verifyFileSteam, Encoding.UTF8, true);

                var (verifySize, verifyBodyBytesStartIndex, verifyBodyBytesLength) = RawImageFactory.GetSize(verifyBinaryReader);
                var (_, verifyHeightPixel) = (SizeI)verifySize;
                var verifyHeightPixelByteLength = verifyHeightPixel * 2;

                using var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);

                var isOk = await VerifyAsync(
                    CalibratingItem,
                    verifyFileSteam,
                    verifyBinaryReader,
                    verifyBodyBytesStartIndex,
                    verifyBodyBytesLength,
                    verifyHeightPixel,
                    verifyHeightPixelByteLength,
                    imageCount,
                    templateId,
                    templateImageSize,
                    detectImageDirectory,
                    semaphore,
                    cancellationToken,
                    false).ConfigureAwait(false);

                if (isOk == false) return false;

                item.IsVerified = true;
                Guard.IsTrue(Save(item, cancellationToken));
            }

            return true;
        }).ConfigureAwait(false);
    }

    private async Task<bool> VerifyAsync(
        LaserXPixelSizeItemDto item,
        FileStream fileSteam,
        BinaryReader binaryReader,
        long bodyBytesStartIndex,
        long bodyBytesLength,
        int heightPixel,
        int heightPixelByteLength,
        int imageCount,
        HTuple templateId,
        Size templateImageSize,
        string detectImageDirectory,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken,
        bool isOkLog = true)
    {
        var imageAllPixelByteLength = Cache.Item.WidthPixel * heightPixelByteLength;
        var verifyStepAllPixelByteLength = (Cache.Item.ColumnCellWidth / item.XPixelSize) * heightPixelByteLength;

        Guard.IsEqualTo(MathHelper.SlideCountFull(imageAllPixelByteLength, verifyStepAllPixelByteLength, bodyBytesLength), imageCount);

        var verifyItems = new Item[imageCount];
        foreach (var (index, pointer) in Enumerable
                     .Range(0, imageCount)
                     .Select(t => t * verifyStepAllPixelByteLength)
                     .Select(Convert.ToInt64)
                     .Select(pointer => pointer - pointer % heightPixelByteLength) // verifyStepAllPixelByteLength是double, 不是整数倍, 需要对齐
                     .Index())
        {
            verifyItems[index] = GetItem(
                pointer,
                imageAllPixelByteLength,
                fileSteam,
                binaryReader,
                bodyBytesStartIndex,
                bodyBytesLength,
                heightPixel,
                heightPixelByteLength);

            await ResolveAsync(
                verifyItems[index],
                templateId,
                templateImageSize,
                detectImageDirectory,
                semaphore,
                cancellationToken,
                isOkLog).ConfigureAwait(false);

            if (verifyItems[index].IsMatchOk == false) return false;
        }

        item.VerifyItems = [.. verifyItems.Select(t => (t.MatchPoint, t.Score, t.ImageFilePath))];

        var verifyXDifferences = item.VerifyItems
            .Zip(item.VerifyItems.Skip(1), (prev, next) => next.MatchPoint.X - prev.MatchPoint.X)
            .ToArray();
        var verifyRealUmPerPixel = Cache.Item.ColumnCellWidth / verifyXDifferences.Average();

        Refresh(item);

        var errorPixel = Math.Abs(Cache.Item.WaferDiameter / verifyRealUmPerPixel - Cache.Item.WaferDiameter / item.XPixelSize);
        var isOk = Math.Abs(verifyXDifferences.Max() - verifyXDifferences.Min()) <= Cache.Threshold
                   && errorPixel <= Cache.Threshold;

        var htmlQuote = new HtmlQuote(new
        {
            Cache.Threshold,
            verifyItems = new HtmlPlot2DLinesChart([(string.Empty, [.. item.VerifyItems.Select(t => t.MatchPoint)])], string.Empty),
            verifyXDifferences = new HtmlPlot2DLinesChart([(string.Empty, [.. verifyXDifferences.Index().Select(t => new Point(t.Index, t.Item))])], string.Empty),
            verifyRealUmPerPixel,
            errorPixel = $"{errorPixel:0.###}px/{Cache.Item.WaferDiameter:0.###}um"
        });

        if (isOk)
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlQuote, HtmlLogUniqueId.LoggingHtml());
        else
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlQuote, HtmlLogUniqueId.LoggingHtml());

        return isOk;
    }

    private void Refresh(LaserXPixelSizeItemDto item)
    {
        ScatterPlotControl.GetOrAddScatterLine("Slide Score", [.. item.SlideItems.Select(t => new Point(t.MatchPoint.X, t.Score))], Colors.Blue);
        ScatterPlotControl.GetOrAddScatterLine("Verify Score", [.. item.VerifyItems.Select(t => new Point(t.MatchPoint.X, t.Score))], Colors.Red);

        ScatterPlotControl.AutoScaleRefresh();
    }

    private bool Save(LaserXPixelSizeItemDto item, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(item);
        update(Cache);

        Calibrations =
        [
            .. Calibrations.Where(t => t.ProductivityInformation != item.ProductivityInformation),
            item.Clone()
        ];

        CacheProvider.SetArray(Calibrations, cancellationToken);
        CacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准

    #region Item

    private Item GetItem(
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

        var item = new Item(pointer / heightPixelByteLength, buffer, new SizeI(currentImageAllPixelByteLength / heightPixelByteLength, heightPixel));

        return item;
    }

    private async Task ResolveAsync(
        Item item,
        HTuple templateId,
        Size templateImageSize,
        string detectImageDirectory,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken,
        bool isOkLog = true)
    {
        var (startPixel, buffer, sizeI) = item;

        try
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            using var image = TempRawImageFactory.CreateImage(buffer, sizeI);

            var isMathOk = CalibrationAlgorithmService.TryTemplateMatchToOffset(Cache.Item.AlgorithmTemplateTypeEnum, image, templateId, out var matchPoint, out _, out var score, out _);

            item.IsMatchOk = isMathOk;
            item.MatchPoint = new Point(item.IsMatchOk ? startPixel + matchPoint.X : startPixel, matchPoint.Y);
            item.Score = score;

            if (isOkLog == false || item.IsMatchOk)
            {
                var title = item.IsMatchOk ? $"{item.MatchPoint.X:0.###}px" : $"{startPixel}px";

                item.ImageFilePath = Path.Combine(detectImageDirectory, Path.GetFileNameWithoutExtension(Cache.Item.TemplateImageFilePath), $"{startPixel}.jpg");
                image.Save(item.ImageFilePath);

                var bullet = new HtmlBullet(new
                {
                    currentMatchPoint = matchPoint,
                    item.StartPixel,
                    item.Size,
                    item.MatchPoint,
                    item.Score,
                    DeltaOfCenter = (item.StartPixel + item.Size.Width / 2d) - item.MatchPoint.X,
                    HtmlTab = new HtmlTab(new
                    {
                        OriginImage = new HtmlImage(item.ImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(matchPoint, templateImageSize), new HtmlImageRectangleOverlay(matchPoint, templateImageSize)]),
                        TemplateImage = new HtmlImage(Cache.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                });

                if (item.IsMatchOk)
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

    private sealed record Item(long StartPixel, byte[] Buffer, SizeI Size)
    {
        public Point MatchPoint { get; set; }

        public double Score { get; set; }

        public string ImageFilePath { get; set; } = string.Empty;

        public bool IsMatchOk { get; set; }
    }

    #endregion
}