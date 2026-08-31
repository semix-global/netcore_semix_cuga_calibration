using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Setting;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.SQL.Cache.Providers.Bases;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Collection;

public sealed partial class CollectionCrossTalkCache : ObservableCacheBase
{
    [ObservableProperty]
    public partial OpticsIlluminationModeEnum OpticsIlluminationModeEnum { get; set; } = OpticsIlluminationModeEnum.OI;

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; } = CalChipSiteModelEnum.DswModel;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new()
    {
        IsAutoGainControl = true,
        IsL0K = false,
        CIBProfileMode = CIBProfileModeEnum.PMTLog
    };

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = null!;

    [ObservableProperty]
    public partial IReadOnlyList<CIBInformation> CIBInformations { get; set; } = [];

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial Point ScribeFindPosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point SignalFindPosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial double Threshold { get; set; } = 1500d;

    [ObservableProperty]
    public partial Rect SignalROI { get; set; } = Rect.Empty;

    [ObservableProperty]
    public partial Rect QuietROI { get; set; } = Rect.Empty;

    [ObservableProperty]
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

    [ObservableProperty]
    public partial string CropSignalFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CropQuietFilePath { get; set; } = string.Empty;
}

public sealed partial class CollectionCrossTalkDTO : ObservableObject
{
    [ObservableProperty]
    public partial bool IsOk { get; set; } = false;

    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Diff))]
    [NotifyPropertyChangedFor(nameof(Radio))]
    [NotifyPropertyChangedFor(nameof(Log10))]
    public partial CollectionCrossTalkDTOItem ScribeResult { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Diff))]
    [NotifyPropertyChangedFor(nameof(Radio))]
    [NotifyPropertyChangedFor(nameof(Log10))]
    public partial CollectionCrossTalkDTOItem SignalResult { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Diff))]
    [NotifyPropertyChangedFor(nameof(Radio))]
    [NotifyPropertyChangedFor(nameof(Log10))]
    public partial double BackgroundNoise { get; set; }

    public double Diff => ScribeResult.SubGray - (SignalResult.SignalGray - BackgroundNoise);

    public double Radio => Math.Pow(2, -Diff / 128);

    public double Log10 => Math.Log10(Math.Pow(2, Diff / 128));

    public object ToHtmlAnonymous() => new
    {
        CIBInformation,
        IsOk,
        ScribeResult = new HtmlBullet(ScribeResult.ToHtmlAnonymous()),
        SignalResult = new HtmlBullet(SignalResult.ToHtmlAnonymous()),
        BackgroundNoise,
        Diff,
        Radio,
        Log10
    };
}

public sealed partial class CollectionCrossTalkDTOItem : ObservableObject
{
    [ObservableProperty]
    public partial string RawImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SubImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double SubGray { get; set; }

    [ObservableProperty]
    public partial double QuietGray { get; set; }

    [ObservableProperty]
    public partial double SignalGray { get; set; }

    [ObservableProperty]
    public partial Rect QuietRoi { get; set; } = Rect.Empty;

    [ObservableProperty]
    public partial Rect SignalRoi { get; set; } = Rect.Empty;

    public object ToHtmlAnonymous() => new
    {
        QuietGray,
        SignalGray,
        SubGray,
        RawImageFilePath,
        SubImage = new HtmlImage(SubImageFilePath),
        OriginImage = new HtmlImage(ImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(QuietRoi), new HtmlImageRectangleOverlay(SignalRoi)])
    };
}

[IOCAppService(ServiceType = typeof(CollectionCrossTalkWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CollectionCrossTalkWindowViewModel(
    MicroscopeViewModel microscopeViewModel,
    ICacheProvider cacheProvider,
    StageViewModel stageViewModel,
    CIBViewModel cibViewModel,
    LaserViewModel laserViewModel,
    IOptions<ApplicationSetting> options,
    ApplicationCookie applicationCookie,
    CreateRoiWindowViewModel createRoiWindowViewModel,
    IWindowManagerService windowManagerService,
    IDialogWindowProvider dialogWindowProvider,
    IHostEnvironment hostEnvironment,
    CalibrationSetting calibrationSetting,
    ILogger<CollectionCrossTalkWindowViewModel> logger) : ViewModelBase
{
    public string Name => "Collection Cross Talk";

    public IReadOnlyList<string> Steps { get; } =
    [
        "Step 1 Alignment",
        "Step 2 Quiet Region ROI",
        "Step 3 Signal Region ROI",
        "Step 4 Cross Talk"
    ];

    public string ImageDirectory => Path.Combine(options.Value.AppHomeDirectory, "Images", nameof(CollectionCrossTalkWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public ApplicationCookie ApplicationCookie => applicationCookie;

    public Guid HtmlLogUniqueId { get; private set; }

    [ObservableProperty]
    public partial AlignmentUserControlViewModel AlignmentUserControlViewModel { get; set; } = HostApplication.GetRequiredService<AlignmentUserControlViewModel>();

    [DefaultCache]
    [ObservableProperty]
    public partial CollectionCrossTalkCache Cache { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<CollectionCrossTalkDTO> Results { get; set; } = [];

    [RelayCommand]
    private void Loaded()
    {
        Cache = cacheProvider.GetOrDefault<CollectionCrossTalkCache>();
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step0Async(CancellationToken cancellationToken)
    {
        await InvokeAsync(0, async () =>
        {
            AlignmentUserControlViewModel.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum;
            AlignmentUserControlViewModel.ProductivityInformation = Cache.ProductivityInformation;

            dialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment ?",
                out var dialogResult,
                DialogButtonsEnum.YesNo,
                DialogIconEnum.Question);

            AlignmentUserControlViewModel.IsDarkFieldAlignment = dialogResult == DialogResultEnum.Yes;

            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

            var alignmentResult = AlignmentUserControlViewModel.AlignmentResult;

            logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.CalChipSiteModelEnum,
                AlignmentUserControlViewModel.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(alignmentResult.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            stageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(stageViewModel.MachineToBrightFieldPosition((alignmentResult.MarkPoint1 + (Vector)alignmentResult.MarkPoint2) / 2d));

            Cache.AlignmentResult = alignmentResult;

            return true;
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1Async(CancellationToken cancellationToken)
    {
        await InvokeAsync(1, async () =>
        {
            await DrawROIAsync(true, cancellationToken).ConfigureAwait(false);

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step2Async(CancellationToken cancellationToken)
    {
        await InvokeAsync(2, async () =>
        {
            await DrawROIAsync(false, cancellationToken).ConfigureAwait(false);

            return true;
        });
    }

    private async Task DrawROIAsync(bool isQuietRegion, CancellationToken cancellationToken)
    {
        var darkFieldImageDto = await cibViewModel.GetPMTImageAsync(
            Cache.ProductivityInformation,
            StageCoordinateSystemEnum.Bright,
            Cache.ScribeFindPosition,
            Cache.ImageWidth,
            ApplicationCookie.CIBInformations.Single(t => t == calibrationSetting.SettingCommonParam.MainCIBInformation),
            (false, Cache.CalChipSiteModelEnum),
            (false, Cache.OpticsConfiguration),
            (false, Cache.CIBConfiguration),
            (false, Cache.LaserLightInformation),
            false,
            cancellationToken,
            isKeepRawImageCIBProfileModeEnum: true);

        if (hostEnvironment.IsDevelopment())
            darkFieldImageDto = GetMockImages([calibrationSetting.SettingCommonParam.MainCIBInformation])[0];
        using var _ = darkFieldImageDto;

        var directory = Path.Combine(ImageDirectory, "ROI", $"{(isQuietRegion ? "Quiet" : "Signal")}");
        var filePath = Path.Combine(directory, $"Origin_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
        darkFieldImageDto.Image.SaveImage(filePath);
        createRoiWindowViewModel.ImageFilePath = filePath;

        if (windowManagerService.ShowDialog(createRoiWindowViewModel) == false) ThrowHelper.ThrowOperationCanceledException<bool>("Generate ROI");

        using var drawImage = darkFieldImageDto.Image.ToRoi(createRoiWindowViewModel.Rect);
        var drawFilePath = Path.Combine(directory, $"Draw_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
        drawImage.SaveImage(drawFilePath);

        if (isQuietRegion)
        {
            Cache.QuietROI = createRoiWindowViewModel.Rect;
            Cache.CropQuietFilePath = drawFilePath;
        }
        else
        {
            Cache.SignalROI = createRoiWindowViewModel.Rect;
            Cache.CropSignalFilePath = drawFilePath;
        }

        logger.LogHtmlInformation("Create ROI", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
        {
            createRoiWindowViewModel.Rect,
            Image = new HtmlImage(filePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(createRoiWindowViewModel.Rect)])
        }), HtmlLogUniqueId.LoggingHtml());
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step3Async(CancellationToken cancellationToken)
    {
        await InvokeAsync(3, async () =>
        {
            logger.LogHtmlInformation("Diagnosis Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.OpticsIlluminationModeEnum,
                Cache.ProductivityInformation,
                Cache.CalChipSiteModelEnum,
                CIBInformations = string.Join(", ", Cache.CIBInformations),
                Cache.ImageWidth,
                Cache.ScribeFindPosition,
                Cache.SignalFindPosition,
                Cache.LaserLightInformation,
                Cache.QuietROI,
                Cache.SignalROI,
                Cache.Threshold,
                CibConfiguration = new HtmlQuote(Cache.CIBConfiguration.ToHtmlAnonymous()),
                OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            laserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, 0);
            var darkFieldImages = await cibViewModel.GetPMTImagesAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Dark,
                Cache.ScribeFindPosition,
                Cache.ImageWidth,
                [.. Cache.CIBInformations],
                (false, Cache.CalChipSiteModelEnum),
                (false, Cache.OpticsConfiguration),
                (false, Cache.CIBConfiguration),
                (true, null),
                false,
                cancellationToken,
                isKeepRawImageCIBProfileModeEnum: true);
            var backgroundGrays = darkFieldImages.Select(t => (t.CIBInformation, t.Image.GetIntensity().Average)).ToArray();

            Results = [];

            foreach (var cibInformations in Cache.CIBInformations.GroupBy(t => t.PMTId)
                         .OrderBy(t => t.Key)
                         .Select(gg => gg.OrderBy(t => t).ToArray()))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var scribeResults = await ActionAsync(Cache.ScribeFindPosition, cibInformations, cancellationToken, true).ConfigureAwait(false);
                var signalResults = await ActionAsync(Cache.SignalFindPosition, cibInformations, cancellationToken, false).ConfigureAwait(false);

                CollectionCrossTalkDTO[] items =
                [
                    ..cibInformations.Select((t, i) =>
                    {
                        var dto = new CollectionCrossTalkDTO
                        {
                            CIBInformation = t,
                            BackgroundNoise = backgroundGrays.Single(o => o.CIBInformation == t).Average,
                            ScribeResult = scribeResults[i],
                            SignalResult = signalResults[i]
                        };
                        dto.IsOk = -dto.Diff < Cache.Threshold;
                        return dto;
                    })
                ];

                Results =
                [
                    ..Results,
                    ..items
                ];

                logger.LogHtmlInformation($"PMT{cibInformations[0].PMTId}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Details = new HtmlContainer([.. items.Select(t => new HtmlTable([t.ToHtmlAnonymous()]))])
                }), HtmlLogUniqueId.LoggingHtml());
            }

            var result = Results.All(t => t.IsOk);

            var htmlBullet = new HtmlBullet(new
            {
                Results = new HtmlContainer([
                    new HtmlTable([
                        ..Results
                            .Select(o =>
                                new
                                {
                                    o.CIBInformation,
                                    ScribeQuietRegion = o.ScribeResult.QuietGray,
                                    ScribeCrossTalkRegion = o.ScribeResult.SignalGray,
                                    CrossTalk = o.ScribeResult.SubGray,
                                    SignalQuietRegion = o.SignalResult.QuietGray,
                                    SignalLightRegion = o.SignalResult.SignalGray,
                                    BackgroundNoise = backgroundGrays.Single(t => t.CIBInformation == o.CIBInformation).Average,
                                    o.Diff,
                                    o.Log10,
                                    o.Radio,
                                    o.IsOk
                                })
                    ])
                ])
            });
            if (result)
                logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
            else
                logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());

            return result;
        });
    }

    private async Task<IReadOnlyList<CollectionCrossTalkDTOItem>> ActionAsync(
        Point position,
        CIBInformation[] cibInformations,
        CancellationToken cancellationToken,
        bool isScribe)
    {
        return await Task.Run(async () =>
        {
            var crossTalkDTOItems = new List<CollectionCrossTalkDTOItem>();
            var currentStartPosition = cibViewModel.GetCIBInformationPosition(
                StageCoordinateSystemEnum.Dark,
                Cache.ProductivityInformation,
                cibInformations[0],
                position,
                microscopeViewModel.GetCurrentMicroscopeLensInformation());

            var darkFieldImages = await cibViewModel.GetPMTImagesAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Dark,
                currentStartPosition,
                Cache.ImageWidth,
                [.. cibInformations],
                (false, Cache.CalChipSiteModelEnum),
                (false, Cache.OpticsConfiguration),
                (false, Cache.CIBConfiguration),
                (false, Cache.LaserLightInformation),
                false,
                cancellationToken,
                isKeepRawImageCIBProfileModeEnum: true);

            if (hostEnvironment.IsDevelopment()) darkFieldImages = GetMockImages([.. cibInformations], isScribe); // mock

            foreach (var dto in darkFieldImages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var imageFileName = $"{dto.CIBInformation}_GUID{Guid.NewGuid()}.png";
                var filePath = Path.Combine(ImageDirectory, imageFileName);
                var subFilePath = Path.Combine(ImageDirectory, "SubImages", imageFileName);

                using var signalDrawImage = dto.Image.ToRoi(Cache.SignalROI);
                using var quietDrawImage = signalDrawImage.ToRoi(Cache.QuietROI);
                quietDrawImage.Save(filePath);

                var (signalRoi, quietRoi) = Cache.SignalROI.Intersection(Cache.QuietROI);

                using var signalImage = dto.Image.ToRoi(signalRoi);
                using var quietImage = dto.Image.ToRoi(quietRoi);

                using var subImage = signalImage.SubImage(quietImage);
                subImage.SaveImage(subFilePath);

                var (grayValue, _) = subImage.GetIntensity();
                var (signalGrayValue, _) = signalImage.GetIntensity();
                var (quietGrayValue, _) = quietImage.GetIntensity();

                crossTalkDTOItems =
                [
                    ..crossTalkDTOItems, new CollectionCrossTalkDTOItem
                    {
                        QuietGray = quietGrayValue,
                        SignalGray = signalGrayValue,
                        SubGray = grayValue,
                        RawImageFilePath = dto.RawImageFilePath,
                        ImageFilePath = filePath,
                        SubImageFilePath = subFilePath,
                        SignalRoi = signalRoi,
                        QuietRoi = quietRoi
                    }
                ];

                using var _ = dto.Image;
            }

            return crossTalkDTOItems;
        }, cancellationToken);
    }

    [RelayCommand]
    private void Close()
    {
        try
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            cacheProvider.Set(Cache, cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name}: Failed to save cache", Name);
        }

        CloseView(true);
    }

    private async Task InvokeAsync(int stepIndex, Func<Task<bool>> func)
    {
        await Task.Run(async () =>
        {
            var isEndHtml = stepIndex == Steps.Count - 1;
            var isInitHtmlLog = stepIndex == 0 || isEndHtml;

            HtmlLogUniqueId = isInitHtmlLog ? Guid.NewGuid() : HtmlLogUniqueId;

            if (isInitHtmlLog) logger.LogHtmlInformation(Name, HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation(Steps[stepIndex], HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());

            var isSuccess = false;
            try
            {
                isSuccess = await func().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                isEndHtml = true;

                if (ex is OperationCanceledException)
                {
                    dialogWindowProvider.ShowDialog($"{Name}: Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    return false;
                }

                dialogWindowProvider.ShowDialog($"""
                                                 {Name}: Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                logger.LogHtmlError(ex, Name, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            }
            finally
            {
                if (isEndHtml)
                    logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{Name}_{(isSuccess ? "OK" : "Failed")}"));
            }

            if (isSuccess)
            {
                if (isEndHtml) dialogWindowProvider.ShowDialog($"{Name}:{Steps[stepIndex]} Success");
            }
            else
                dialogWindowProvider.ShowDialog($"{Name}: {Steps[stepIndex]} Error", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            return isSuccess;
        }).ConfigureAwait(false);
    }

    private static IReadOnlyList<DarkFieldImageDTO> GetMockImages(CIBInformation[] cibInformations, bool isScribe = true)
    {
        var mockImageDirectoryPath = Path.Combine(@"Assets\Data\CrossTalk", $"{(isScribe ? "Scribe" : "Signal")}");

        var results = new List<DarkFieldImageDTO>();
        foreach (var cibInformation in cibInformations)
        {
            var searchPattern = $"*CH{cibInformation.ChannelId}_{cibInformation.PMTId}.raw*";
            var files = Directory.GetFiles(mockImageDirectoryPath, searchPattern);

            if (files.Length == 0)
                throw new FileNotFoundException($"{searchPattern}");

            // 取第一个匹配的文件
            var filePath = files[0];
            var bytes = System.IO.File.ReadAllBytes(filePath);

            var (size, _, _) = RAWImageFactory.GetSize(bytes);

            results = [.. results, new DarkFieldImageDTO().AdaptIn(new DarkFieldRawScanImageDTO { CIBInformation = cibInformation, Size = size, IsForward = true, RawImageCIBProfileModeEnum = CIBProfileModeEnum.PMTLog, RawImageFilePath = filePath, IsKeepRawImageCIBProfileModeEnum = true })];
        }

        return results;
    }
}