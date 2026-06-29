using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Core.Utilities;
using Core.Utilities.SourceGenerators.Attributes;
using Local.SQL.Cache.Providers.Bases;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
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
    public partial LaserLightInformation LaserLightInformation { get; set; } = null!;

    [ObservableProperty]
    public partial int PMTId { get; set; } = 8;

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial Point FindPosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial double Threshold { get; set; } = 1 / 1500d;
}

public sealed partial class CollectionCrossTalkResult : ObservableObject
{
    [ObservableProperty]
    public partial bool IsOk { get; set; } = false;

    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial string ImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double GrayValue { get; set; }

    [ObservableProperty]
    public partial double RadioResult { get; set; }

    public object ToHtmlAnonymous(string title, bool isResult) => isResult
        ? new
        {
            title,
            IsOk,
            CIBInformation,
            GrayValue,
            RadioResult,
            ResultImageh = new HtmlImage(ImageFilePath),
        }
        : new
        {
            title,
            CIBInformation,
            GrayValue,
            ResultImageh = new HtmlImage(ImageFilePath)
        };
}

[IOCAppService(ServiceType = typeof(CollectionCrossTalkWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CollectionCrossTalkWindowViewModel(
    ICacheProvider cacheProvider,
    StageViewModel stageViewModel,
    CIBViewModel cibViewModel,
    OpticsViewModel opticsViewModel,
    IOptions<ApplicationSetting> options,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    ApplicationCookie applicationCookie,
    IDialogWindowProvider dialogWindowProvider,
    ILogger<CollectionCrossTalkWindowViewModel> logger) : ViewModelBase
{
    public string Name => "Collection Cross Talk";

    public string ImageDirectory => Path.Combine(options.Value.AppHomeDirectory, "Images", nameof(CollectionCrossTalkWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public ApplicationCookie ApplicationCookie => applicationCookie;

    public Guid HtmlLogUniqueId { get; private set; }

    [DefaultCache]
    [ObservableProperty]
    public partial CollectionCrossTalkCache Cache { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<CollectionCrossTalkResult> ZoosOnResults { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<CollectionCrossTalkResult> ZoosOffResults { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<CollectionCrossTalkResult> Results { get; set; } = [];

    [RelayCommand]
    private void Loaded()
    {
        Cache = cacheProvider.GetOrDefault<CollectionCrossTalkCache>();
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ActionAsync(CancellationToken cancellationToken)
    {
        await InvokeAsync(async () =>
        {
            ZoosOnResults = [];
            ZoosOffResults = [];
            Results = [];

            CIBConfiguration cibConfiguration = new()
            {
                IsAutoGainControl = true,
                IsL0K = false,
                CIBProfileMode = CIBProfileModeEnum.PMTLog
            };

            logger.LogHtmlInformation("Diagnosis Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.OpticsIlluminationModeEnum,
                Cache.ProductivityInformation,
                Cache.CalChipSiteModelEnum,
                Cache.PMTId,
                Cache.ImageWidth,
                Cache.FindPosition,
                Cache.LaserLightInformation,
                Cache.Threshold,
                CibConfiguration = new HtmlQuote(cibConfiguration.ToHtmlAnonymous()),
                OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            try
            {
                stageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(Cache.FindPosition);

                opticsViewModel.ToggleZoosClinder(Cache.OpticsIlluminationModeEnum, true);
                var zoosOnDarkFieldImages = await cibViewModel.GetPMTImagesAsync(
                    Cache.ProductivityInformation,
                    StageCoordinateSystemEnum.Bright,
                    Cache.FindPosition,
                    Cache.ImageWidth,
                    [.. ApplicationCookie.CIBInformations.Where(t => t.PMTId == Cache.PMTId)],
                    (false, Cache.CalChipSiteModelEnum),
                    (false, Cache.OpticsConfiguration),
                    (false, cibConfiguration),
                    (false, Cache.LaserLightInformation),
                    false,
                    cancellationToken);
                ZoosOnResults = GetResult([.. zoosOnDarkFieldImages.Select(t => (t.Image, t.CIBInformation))]);

                opticsViewModel.ToggleZoosClinder(Cache.OpticsIlluminationModeEnum, false);
                var zoosOffDarkFieldImages = await cibViewModel.GetPMTImagesAsync(
                    Cache.ProductivityInformation,
                    StageCoordinateSystemEnum.Bright,
                    Cache.FindPosition,
                    Cache.ImageWidth,
                    [.. ApplicationCookie.CIBInformations.Where(t => t.PMTId == Cache.PMTId)],
                    (false, Cache.CalChipSiteModelEnum),
                    (false, Cache.OpticsConfiguration),
                    (false, cibConfiguration),
                    (false, Cache.LaserLightInformation),
                    false,
                    cancellationToken);
                ZoosOffResults = GetResult([.. zoosOffDarkFieldImages.Select(t => (t.Image, t.CIBInformation))]);

                var resultImages = zoosOnDarkFieldImages
                    .Select((t, i) =>
                    {
                        var image = zoosOffDarkFieldImages[i].Image.SubImage(t.Image);
                        return (image, t.CIBInformation);
                    }).ToList();
                Results = GetResult(resultImages);

                logger.LogHtmlInformation("Result", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                foreach (var (index, result) in Results.Select((t, i) => (i, t)))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    result.RadioResult = result.GrayValue / ZoosOffResults.Single(t => t.CIBInformation == result.CIBInformation).GrayValue;
                    result.IsOk = result.RadioResult <= Cache.Threshold;

                    logger.LogHtmlInformation($"{result.CIBInformation} {(result.IsOk ? "Success" : "Failed")}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                    {
                        Results = new HtmlTable([
                            ZoosOnResults[index].ToHtmlAnonymous("ZoosOn", false),
                            ZoosOffResults[index].ToHtmlAnonymous("ZoosOff", false),
                            result.ToHtmlAnonymous("Diff", true)
                        ])
                    }), HtmlLogUniqueId.LoggingHtml());
                }

                return true;
            }
            finally
            {
                opticsViewModel.ToggleZoosClinder(Cache.OpticsIlluminationModeEnum, false);
            }

            IReadOnlyList<CollectionCrossTalkResult> GetResult(IReadOnlyList<(BitmapImage image, CIBInformation cibInformation)> images)
            {
                List<CollectionCrossTalkResult> results = [];
                foreach (var (image, cibInformation) in images)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using var _ = image;

                    var filePath = Path.Combine(ImageDirectory, $"{cibInformation}_GUID{Guid.NewGuid()}.png");
                    image.SaveImage(filePath);

                    results =
                    [
                        ..results, new CollectionCrossTalkResult
                        {
                            CIBInformation = cibInformation,
                            GrayValue = image.GetIntensity().Average,
                            ImageFilePath = filePath
                        }
                    ];
                }

                return results;
            }
        });
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

    private async Task InvokeAsync(Func<Task<bool>> func)
    {
        await Task.Run(async () =>
        {
            HtmlLogUniqueId = Guid.NewGuid();

            logger.LogHtmlInformation(Name, HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());

            var isSuccess = false;
            try
            {
                isSuccess = await func().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
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
                logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{(isSuccess ? "OK" : "Failed")}"));
            }

            if (isSuccess)
            {
                dialogWindowProvider.ShowDialog($"{Name}: Success");
            }
            else
                dialogWindowProvider.ShowDialog($"{Name}: Error", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            return isSuccess;
        }).ConfigureAwait(false);
    }
}