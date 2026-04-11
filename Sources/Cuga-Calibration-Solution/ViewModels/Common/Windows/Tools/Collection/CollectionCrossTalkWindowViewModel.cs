using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Core.Utilities;
using CugaCalibration.ViewModels.Common.Windows.View;
using Local.SQL.Cache.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Collection;

[IOCAppService(ServiceType = typeof(CollectionCrossTalkWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CollectionCrossTalkWindowViewModel(
    IServiceProvider serviceProvider,
    StageViewModel stageViewModel,
    StageWindowViewModel stageWindowViewModel,
    AfViewModel afViewModel,
    LaserViewModel laserViewModel,
    CIBViewModel cibViewModel,
    IOptions<ApplicationSetting> options,
    ICalibrationOpticsService calibrationOpticsService,
    CreateRoiWindowViewModel createRoiWindowViewModel,
    ApplicationCookie applicationCookie,
    ICacheProvider cacheProvider,
    IDialogWindowProvider dialogWindowProvider,
    IWindowManagerService windowManagerService,
    ILogger<CollectionCrossTalkWindowViewModel> logger) : ViewModelBase
{
    private const string DSW = nameof(DSW);

    private const string Haze = nameof(Haze);

    public string ImageDirectory => Path.Combine(options.Value.AppHomeDirectory, "Images", nameof(CollectionCrossTalkWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public static string LogHtmlFileName => "CollectionCrossTalk_Diagnosis";

    public string DiagnosisHtmlLogFileName => string.IsNullOrWhiteSpace(LogHtmlFileName) ? "Diagnosis" : $"Diagnosis-{FileHelper.RemoveInvalidFileName(LogHtmlFileName)}";

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private OpticsConfiguration _opticsConfiguration = new();

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private Point _brightFieldPosition;

    [ObservableProperty]
    private int _imageWidthPixel = 1000;

    [ObservableProperty]
    private double[] _pMT8ChPixselBefore = new double[3];

    [ObservableProperty]
    private double[] _pMT8ChPixselAfter = new double[3];

    [ObservableProperty]
    private string[] _pMT8ChCenterOpticsImagePath = new string[3];

    [ObservableProperty]
    private string[] _pMT8ChAllOpticsImagePath = new string[3];

    [ObservableProperty]
    private double _pMT8Ch1PixselCompare = 0;

    [ObservableProperty]
    private double _pMT8Ch2PixselCompare = 0;

    [ObservableProperty]
    private double _pMT8Ch3PixselCompare = 0;

    public ApplicationCookie ApplicationCookie => applicationCookie;

    public Guid HtmlLogUniqueId { get; private set; }

    [RelayCommand]
    private void Loaded()
    {
        BrightFieldPosition = stageViewModel.GetBrightFieldStagePosition();

        HtmlLogUniqueId = Guid.NewGuid();
        logger.LogHtmlInformation("Result Params", HtmlHeaderLevelEnum.Header1, new HtmlBullet(new
        {
            BrightFieldPosition,
            ImageWidthPixel
        }), HtmlLogUniqueId.LoggingHtml());
    }

    [RelayCommand]
    private Task Step00Async()
    {
        logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            ProductivityInformation
        }), HtmlLogUniqueId.LoggingHtml());
        return Task.CompletedTask;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step01Async(CancellationToken cancellationToken)
    {
        calibrationOpticsService.ClinderEXC(OpticsYGhostModeEnum.NI_Zoos, true);
        BrightFieldPosition = stageViewModel.GetBrightFieldStagePosition();
        CalChipSiteModelEnum calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
        stageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(BrightFieldPosition, calChipSiteModelEnum);

        Point DarkFieldPosition = stageViewModel.GetDarkFieldStagePosition();
        {
            var centerOpticsPaths = new string[3]; // 创建新数组
            for (int i = 1; i <= 3; i++)
            {
                var CIBInfor = ApplicationCookie.CIBInformations.Single(x => x.PMTId == 8 && x.ChannelId == i);
                using var darkFieldImage = await cibViewModel.GetPMTImageAsync(
                    ApplicationCookie.NILowProductivityInformation,
                    StageCoordinateSystemEnum.Dark,
                    DarkFieldPosition,
                    ImageWidthPixel,
                    CIBInfor,
                    (false, CalChipSiteModelEnum.DswModel),
                    (false, OpticsConfiguration),
                    (false, CIBConfiguration),
                    (false, LaserLightInformation),
                    false,
                    cancellationToken);

                var path1 = Path.Combine(ImageDirectory, $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                PMT8ChPixselBefore[i - 1] = darkFieldImage.Image.GetIntensity().Average;
                darkFieldImage.Image.Save(path1);
                centerOpticsPaths[i - 1] = path1;
            }

            PMT8ChCenterOpticsImagePath = centerOpticsPaths;
        }

        logger.LogHtmlInformation("Get PMT of 8 Pictures", HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
        {
            BrightFieldPosition,
            DarkFieldPosition,
            ImageWidthPixel,
            HtmlTab = new HtmlTab(new
            {
                PMT8Ch1Image = new HtmlImage(PMT8ChCenterOpticsImagePath[0]),
                PMT8Ch2Image = new HtmlImage(PMT8ChCenterOpticsImagePath[1]),
                PMT8Ch3Image = new HtmlImage(PMT8ChCenterOpticsImagePath[2])
            })
        }), HtmlLogUniqueId.LoggingHtml());
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step02Async(CancellationToken cancellationToken)
    {
        calibrationOpticsService.ClinderEXC(OpticsYGhostModeEnum.NI_Zoos, false);
        Point DarkFieldPosition = stageWindowViewModel.DarkFieldPosition;

        {
            var allOpticsPaths = new string[3]; // 创建新数组
            for (int i = 1; i <= 3; i++)
            {
                var CIBInfor = ApplicationCookie.CIBInformations.Single(x => x.PMTId == 8 && x.ChannelId == i);
                using var darkFieldImage = await cibViewModel.GetPMTImageAsync(
                    ApplicationCookie.NILowProductivityInformation,
                    StageCoordinateSystemEnum.Dark,
                    DarkFieldPosition,
                    ImageWidthPixel,
                    CIBInfor,
                    (false, CalChipSiteModelEnum.DswModel),
                    (false, OpticsConfiguration),
                    (false, CIBConfiguration),
                    (false, LaserLightInformation),
                    false,
                    cancellationToken);

                var path1 = Path.Combine(ImageDirectory, $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                PMT8ChPixselAfter[i - 1] = darkFieldImage.Image.GetIntensity().Average;
                darkFieldImage.Image.Save(path1);
                allOpticsPaths[i - 1] = path1;
            }

            PMT8ChAllOpticsImagePath = allOpticsPaths;
        }

        logger.LogHtmlInformation("Get ALL PMT Pictures", HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
        {
            BrightFieldPosition,
            DarkFieldPosition,
            ImageWidthPixel,
            HtmlTab = new HtmlTab(new
            {
                AllPMTCh1Image = new HtmlImage(PMT8ChAllOpticsImagePath[0]),
                AllPMTCh2Image = new HtmlImage(PMT8ChAllOpticsImagePath[1]),
                AllPMTCh3Image = new HtmlImage(PMT8ChAllOpticsImagePath[2])
            })
        }), HtmlLogUniqueId.LoggingHtml());
    }

    [RelayCommand]
    private Task Step03Async()
    {
        Point DarkFieldPosition = stageWindowViewModel.DarkFieldPosition;
        PMT8Ch1PixselCompare = PMT8ChPixselBefore[0] / PMT8ChPixselAfter[0];
        PMT8Ch2PixselCompare = PMT8ChPixselBefore[1] / PMT8ChPixselAfter[1];
        PMT8Ch3PixselCompare = PMT8ChPixselBefore[2] / PMT8ChPixselAfter[2];
        logger.LogHtmlInformation("Compare Pictures Pixsel OF PMT8 And All PMT", HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
        {
            BrightFieldPosition = BrightFieldPosition,
            DarkFieldPosition = DarkFieldPosition,
            ImageWidthPixel = ImageWidthPixel,
            PMT8Ch1PixselCompare = PMT8ChPixselBefore[0] / PMT8ChPixselAfter[0],
            PMT8Ch2PixselCompare = PMT8ChPixselBefore[1] / PMT8ChPixselAfter[1],
            PMT8Ch3PixselCompare = PMT8ChPixselBefore[2] / PMT8ChPixselAfter[2],
            Image = new HtmlImage(PMT8ChCenterOpticsImagePath[2])
        }), HtmlLogUniqueId.LoggingHtml());

        logger.LogHtmlInformation(HtmlLogUniqueId.LoggingPeekHtml($"{DiagnosisHtmlLogFileName}_OK"));
        logger.LogHtmlInformation(HtmlLogUniqueId.LoggingClearHtml());
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void Close()
    {
        try
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save cache");
        }

        CloseView(null);
    }
}