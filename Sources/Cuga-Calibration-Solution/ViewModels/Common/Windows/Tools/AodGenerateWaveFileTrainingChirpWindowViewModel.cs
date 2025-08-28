using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Setting;
using Core.Utilities;
using MathNet.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;
using Core.Models.Models.Common.Cookies;
using AodWaveGenerator = Net.Utilities.Algorithms.Modules.AodWaveGenerator;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(AodGenerateWaveFileTrainingChirpWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AodGenerateWaveFileTrainingChirpWindowViewModel(
    IDialogWindowProvider dialogWindowProvider,
    IWindowManagerService windowManagerService,
    CreateRoiWindowViewModel createRoiWindowViewModel,
    StageViewModel stageViewModel,
    LaserViewModel laserViewModel,
    AfViewModel afViewModel,
    CalibrationSetting calibrationSetting,
    IOptions<ApplicationSetting> options,
    ApplicationCookie applicationCookie,
    ILogger<AodGenerateWaveFileTrainingChirpWindowViewModel> logger) : ViewModelBase
{
    public IReadOnlyList<LaserLightInformation> LaserLightInformationList => applicationCookie.LaserLightInformationList;
    
    public string ImageDirectory => Path.Combine(options.Value.AppHomeDirectory, "Images", DirectoryHelper.RemoveInvalidDirectoryName(nameof(AodGenerateWaveFileTrainingChirpWindowViewModel)), DateTime.Now.ToString(Constants.MiddleFileDateTimeFormat));

    public string AodWaveDirectory => Path.Combine(options.Value.AppHomeDirectory, "Chirp", DirectoryHelper.RemoveInvalidDirectoryName(nameof(AodGenerateWaveFileTrainingChirpWindowViewModel)), DateTime.Now.ToString(Constants.MiddleFileDateTimeFormat));

    #region 0. 确认生成波形参数

    [ObservableProperty]
    private GenerateChirpAodWaveParamDto _generateChirpAodWaveParamDto = new()
    {
        HeaderFrequency = 275,
        FooterFrequency = 155
    };

    #endregion 0. 确认生成波形参数

    #region 1. 确认ROI范围用来寻找最大灰阶值

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private int _xWidthPixel = 800;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum = OpticsMagTypeEnum.High;

    [ObservableProperty]
    private StageSpeedEnum _stageSpeedEnum = StageSpeedEnum.Low;

    [ObservableProperty]
    private Rect _roiRect = new(0, 0, 256, 256);

    #endregion 1. 确认ROI范围用来寻找最大灰阶值

    #region 2. RTFC

    [ObservableProperty]
    private double _ecsMin = 5000;

    [ObservableProperty]
    private double _ecsMax = 5100;

    [ObservableProperty]
    private double _ecsStep = 10;

    #endregion 2. RTFC

    #region 3. 补偿训练

    [ObservableProperty]
    private LaserLightInformation _prescanLaserLightInformation = calibrationSetting.SettingCommonParam.MainLaserLightInformation;

    #region 散光

    [ObservableProperty]
    private bool _astigmatismCompensationCoefficientIsEnable = true;

    [ObservableProperty]
    private double _astigmatismCompensationCoefficientMin = 0.0000000001;

    [ObservableProperty]
    private double _astigmatismCompensationCoefficientMax = 0.0000000002;

    [ObservableProperty]
    private double _astigmatismCompensationCoefficientStep = 0.0000000001;

    #endregion 散光

    #region 球差

    [ObservableProperty]
    private bool _sphericalAberrationCompensationCoefficientIsEnable;

    [ObservableProperty]
    private double _sphericalAberrationCompensationCoefficientMin = 0.0000000001;

    [ObservableProperty]
    private double _sphericalAberrationCompensationCoefficientMax = 0.0000000002;

    [ObservableProperty]
    private double _sphericalAberrationCompensationCoefficientStep = 0.0000000001;

    #endregion 球差

    #region 二阶散光

    [ObservableProperty]
    private bool _secondaryAstigmatismCompensationCoefficientIsEnable;

    [ObservableProperty]
    private double _secondaryAstigmatismCompensationCoefficientMin = 0.0000000001;

    [ObservableProperty]
    private double _secondaryAstigmatismCompensationCoefficientMax = 0.0000000002;

    [ObservableProperty]
    private double _secondaryAstigmatismCompensationCoefficientStep = 0.0000000001;

    #endregion 二阶散光

    #region Coma

    [ObservableProperty]
    private bool _comaCompensationCoefficientIsEnable;

    [ObservableProperty]
    private double _comaCompensationCoefficientMin = 1;

    [ObservableProperty]
    private double _comaCompensationCoefficientMax = 2;

    [ObservableProperty]
    private double _comaCompensationCoefficientStep = 1;

    #endregion Coma

    #region Trefoil

    [ObservableProperty]
    private bool _trefoilCompensationCoefficientIsEnable;

    [ObservableProperty]
    private double _trefoilCompensationCoefficientMin = 1;

    [ObservableProperty]
    private double _trefoilCompensationCoefficientMax = 2;

    [ObservableProperty]
    private double _trefoilCompensationCoefficientStep = 1;

    #endregion Trefoil

    #region Quadrafoil

    [ObservableProperty]
    private bool _quadrafoilCompensationCoefficientIsEnable;

    [ObservableProperty]
    private double _quadrafoilCompensationCoefficientMin = 1;

    [ObservableProperty]
    private double _quadrafoilCompensationCoefficientMax = 2;

    [ObservableProperty]
    private double _quadrafoilCompensationCoefficientStep = 1;

    #endregion Quadrafoil

    #endregion 3. 补偿训练

    [ObservableProperty]
    private AodGenerateWaveFileTrainingChirp? _selectItem;

    [ObservableProperty]
    private AodGenerateWaveFileTrainingChirp[] _items = [];

    [ObservableProperty]
    private Point[] _itemsPoints = [];

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [RelayCommand]
    private async Task Step1Async()
    {
        var htmlGuid = Guid.NewGuid();
        var isSuccess = false;
        try
        {
            await Task.Run(() =>
            {
                FindPosition = stageViewModel.GetBrightFieldStagePosition();

                var darkFieldImageDto = laserViewModel.GetDarkFieldLineScanImage(
                    CalChipSiteModelEnum.ChuckModel,
                    FindPosition,
                    (false, calibrationSetting.SettingCommonParam.MainLaserLightInformation),
                    false,
                    CIBConfiguration,
                    XWidthPixel,
                    OpticsMagTypeEnum,
                    StageSpeedEnum,
                    stageCoordinateSystemEnum: StageCoordinateSystemEnum.Bright);
                var detectImageDirectory = ImageDirectory;
                using var _ = darkFieldImageDto;

                var filePath = $"{detectImageDirectory}\\{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg";
                HalconHelper.Save(darkFieldImageDto.Image, filePath);
                createRoiWindowViewModel.ImageFilePath = filePath;

                var showDialog = windowManagerService.ShowDialog(createRoiWindowViewModel);

                if (showDialog == false)
                {
                    dialogWindowProvider.ShowDialog("Generate ROI Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                RoiRect = createRoiWindowViewModel.Rect;

                logger.LogHtmlInformation("Create ROI", HtmlHeaderLevelEnum.Header1, new HtmlBullet(new
                {
                    calibrationSetting.SettingCommonParam.MainLaserLightInformation,
                    FindPosition,
                    XWidthPixel,
                    OpticsMagTypeEnum,
                    StageSpeedEnum,
                    RoiRect,
                    Image = new HtmlImage(filePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(RoiRect)])
                }), htmlGuid.LoggingHtml());

                isSuccess = true;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                logger.LogWarning("Canceled");
                return;
            }

            logger.LogError(ex, "Step1");
        }
        finally
        {
            logger.LogHtmlInformation(htmlGuid.LoggedEndHtml($"Step1CreateROI{(isSuccess ? "OK" : "Error")}"));
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step3Async(CancellationToken cancellationToken)
    {
        var htmlGuid = Guid.NewGuid();
        var isSuccess = false;
        try
        {
            var imageDirectory = ImageDirectory;
            var aodWaveDirectory = AodWaveDirectory;

            logger.LogHtmlInformation("Training", HtmlHeaderLevelEnum.Header1, htmlGuid.LoggingHtml());

            if (AstigmatismCompensationCoefficientIsEnable)
            {
                Items =
                [
                    .. Generate.LinearRange(AstigmatismCompensationCoefficientMin, AstigmatismCompensationCoefficientStep, AstigmatismCompensationCoefficientMax).Select(t => new AodGenerateWaveFileTrainingChirp
                    {
                        AstigmatismCompensationCoefficient = t,
                        SphericalAberrationCompensationCoefficient = 0,
                        SecondaryAstigmatismCompensationCoefficient = 0,
                        ComaCompensationCoefficient = 0,
                        TrefoilCompensationCoefficient = 0,
                        QuadrafoilCompensationCoefficient = 0
                    })
                ];
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                logger.LogHtmlInformation("Astigmatism", HtmlHeaderLevelEnum.Header2, htmlGuid.LoggingHtml());

                ItemsPoints = [];
                foreach (var item in Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SelectItem = item;
                    if (await CatchImagesAsync(item, imageDirectory, aodWaveDirectory, htmlGuid, cancellationToken).ConfigureAwait(false) == false) continue;

                    item.IsOk = true;
                    ItemsPoints = [.. ItemsPoints, new Point(item.AstigmatismCompensationCoefficient, item.MaxItem?.ImageMaxGrayValue ?? 0d)];
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                }

                var maxItem = Items.OrderByDescending(t => t.MaxItem?.ImageMaxGrayValue ?? 0).First();

                logger.LogHtmlInformation("OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    MaxItemAstigmatismCompensationCoefficient = maxItem.AstigmatismCompensationCoefficient.ToString("0.###############"),
                    MaxItemAstigmatismCompensationEcs = maxItem.MaxItem?.Ecs,
                    MaxItemAstigmatismCompensationImageMaxGrayValue = maxItem.MaxItem?.ImageMaxGrayValue,
                    Table = new HtmlTable([
                        ..Items.Select(t => new
                        {
                            AstigmatismCompensationCoefficient = t.AstigmatismCompensationCoefficient.ToString("0.###############"),
                            t.ChirpAodWaveFilePath,
                            Signals = new HtmlPlot2DLinesChart([(string.Empty, t.AodWaveSignals)], string.Empty),
                            Fouriers = new HtmlPlot2DLinesChart([(string.Empty, t.AodWaveSignalsFourier)], string.Empty)
                        })
                    ]),
                    Plot = new HtmlPlot2DLinesChart([(string.Empty, ItemsPoints)], string.Empty)
                }), htmlGuid.LoggingHtml());
            }

            if (SphericalAberrationCompensationCoefficientIsEnable)
            {
                Items =
                [
                    .. Generate.LinearRange(SphericalAberrationCompensationCoefficientMin, SphericalAberrationCompensationCoefficientStep, SphericalAberrationCompensationCoefficientMax).Select(t => new AodGenerateWaveFileTrainingChirp
                    {
                        AstigmatismCompensationCoefficient = 0,
                        SphericalAberrationCompensationCoefficient = t,
                        SecondaryAstigmatismCompensationCoefficient = 0,
                        ComaCompensationCoefficient = 0,
                        TrefoilCompensationCoefficient = 0,
                        QuadrafoilCompensationCoefficient = 0
                    })
                ];
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                logger.LogHtmlInformation("SphericalAberration", HtmlHeaderLevelEnum.Header2, htmlGuid.LoggingHtml());

                ItemsPoints = [];
                foreach (var item in Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SelectItem = item;
                    if (await CatchImagesAsync(item, imageDirectory, aodWaveDirectory, htmlGuid, cancellationToken).ConfigureAwait(false) == false) continue;

                    item.IsOk = true;
                    ItemsPoints = [.. ItemsPoints, new Point(item.SphericalAberrationCompensationCoefficient, item.MaxItem?.ImageMaxGrayValue ?? 0d)];

                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                }

                var maxItem = Items.OrderByDescending(t => t.MaxItem?.ImageMaxGrayValue ?? 0).First();

                logger.LogHtmlInformation("OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    MaxItemSphericalAberrationCompensationCoefficient = maxItem.SphericalAberrationCompensationCoefficient.ToString("0.###############"),
                    MaxItemSphericalAberrationCompensationEcs = maxItem.MaxItem?.Ecs,
                    MaxItemSphericalAberrationCompensationImageMaxGrayValue = maxItem.MaxItem?.ImageMaxGrayValue,
                    Table = new HtmlTable([
                        ..Items.Select(t => new
                        {
                            SphericalAberrationCompensationCoefficient = t.SphericalAberrationCompensationCoefficient.ToString("0.###############"),
                            t.ChirpAodWaveFilePath,
                            Signals = new HtmlPlot2DLinesChart([(string.Empty, t.AodWaveSignals)], string.Empty),
                            Fouriers = new HtmlPlot2DLinesChart([(string.Empty, t.AodWaveSignalsFourier)], string.Empty)
                        })
                    ]),
                    Plot = new HtmlPlot2DLinesChart([(string.Empty, ItemsPoints)], string.Empty)
                }), htmlGuid.LoggingHtml());
            }

            if (SecondaryAstigmatismCompensationCoefficientIsEnable)
            {
                Items =
                [
                    .. Generate.LinearRange(SecondaryAstigmatismCompensationCoefficientMin, SecondaryAstigmatismCompensationCoefficientStep, SecondaryAstigmatismCompensationCoefficientMax).Select(t => new AodGenerateWaveFileTrainingChirp
                    {
                        AstigmatismCompensationCoefficient = 0,
                        SphericalAberrationCompensationCoefficient = 0,
                        SecondaryAstigmatismCompensationCoefficient = t,
                        ComaCompensationCoefficient = 0,
                        TrefoilCompensationCoefficient = 0,
                        QuadrafoilCompensationCoefficient = 0
                    })
                ];
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                logger.LogHtmlInformation("SecondaryAstigmatism", HtmlHeaderLevelEnum.Header2, htmlGuid.LoggingHtml());

                ItemsPoints = [];
                foreach (var item in Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SelectItem = item;
                    if (await CatchImagesAsync(item, imageDirectory, aodWaveDirectory, htmlGuid, cancellationToken).ConfigureAwait(false) == false) continue;

                    item.IsOk = true;
                    ItemsPoints = [.. ItemsPoints, new Point(item.SecondaryAstigmatismCompensationCoefficient, item.MaxItem?.ImageMaxGrayValue ?? 0d)];
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                }

                var maxItem = Items.OrderByDescending(t => t.MaxItem?.ImageMaxGrayValue ?? 0).First();

                logger.LogHtmlInformation("OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    MaxItemSecondaryAstigmatismCompensationCoefficient = maxItem.SecondaryAstigmatismCompensationCoefficient.ToString("0.###############"),
                    MaxItemSecondaryAstigmatismCompensationEcs = maxItem.MaxItem?.Ecs,
                    MaxItemSecondaryAstigmatismCompensationImageMaxGrayValue = maxItem.MaxItem?.ImageMaxGrayValue,
                    Table = new HtmlTable([
                        ..Items.Select(t => new
                        {
                            SecondaryAstigmatismCompensationCoefficient = t.SecondaryAstigmatismCompensationCoefficient.ToString("0.###############"),
                            t.ChirpAodWaveFilePath,
                            Signals = new HtmlPlot2DLinesChart([(string.Empty, t.AodWaveSignals)], string.Empty),
                            Fouriers = new HtmlPlot2DLinesChart([(string.Empty, t.AodWaveSignalsFourier)], string.Empty)
                        })
                    ]),
                    Plot = new HtmlPlot2DLinesChart([(string.Empty, ItemsPoints)], string.Empty)
                }), htmlGuid.LoggingHtml());
            }

            if (ComaCompensationCoefficientIsEnable)
            {
                Items =
                [
                    .. Generate.LinearRange(ComaCompensationCoefficientMin, ComaCompensationCoefficientStep, ComaCompensationCoefficientMax).Select(t => new AodGenerateWaveFileTrainingChirp
                    {
                        AstigmatismCompensationCoefficient = 0,
                        SphericalAberrationCompensationCoefficient = 0,
                        SecondaryAstigmatismCompensationCoefficient = 0,
                        ComaCompensationCoefficient = t,
                        TrefoilCompensationCoefficient = 0,
                        QuadrafoilCompensationCoefficient = 0
                    })
                ];
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                logger.LogHtmlInformation("Coma", HtmlHeaderLevelEnum.Header2, htmlGuid.LoggingHtml());

                ItemsPoints = [];
                foreach (var item in Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SelectItem = item;
                    if (await CatchImagesAsync(item, imageDirectory, aodWaveDirectory, htmlGuid, cancellationToken).ConfigureAwait(false) == false) continue;

                    item.IsOk = true;
                    ItemsPoints = [.. ItemsPoints, new Point(item.ComaCompensationCoefficient, item.MaxItem?.ImageMaxGrayValue ?? 0d)];
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                }

                var maxItem = Items.OrderByDescending(t => t.MaxItem?.ImageMaxGrayValue ?? 0).First();

                logger.LogHtmlInformation("OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    MaxItemComaCompensationCoefficient = maxItem.ComaCompensationCoefficient.ToString("0.###############"),
                    MaxItemComaCompensationEcs = maxItem.MaxItem?.Ecs,
                    MaxItemComaCompensationImageMaxGrayValue = maxItem.MaxItem?.ImageMaxGrayValue,
                    Table = new HtmlTable([
                        ..Items.Select(t => new
                        {
                            ComaCompensationCoefficient = t.ComaCompensationCoefficient.ToString("0.###############"),
                            t.ChirpAodWaveFilePath,
                            Signals = new HtmlPlot2DLinesChart([(string.Empty, t.AodWaveSignals)], string.Empty),
                            Fouriers = new HtmlPlot2DLinesChart([(string.Empty, t.AodWaveSignalsFourier)], string.Empty)
                        })
                    ]),
                    Plot = new HtmlPlot2DLinesChart([(string.Empty, ItemsPoints)], string.Empty)
                }), htmlGuid.LoggingHtml());
            }

            if (TrefoilCompensationCoefficientIsEnable)
            {
                Items =
                [
                    .. Generate.LinearRange(TrefoilCompensationCoefficientMin, TrefoilCompensationCoefficientStep, TrefoilCompensationCoefficientMax).Select(t => new AodGenerateWaveFileTrainingChirp
                    {
                        AstigmatismCompensationCoefficient = 0,
                        SphericalAberrationCompensationCoefficient = 0,
                        SecondaryAstigmatismCompensationCoefficient = 0,
                        ComaCompensationCoefficient = 0,
                        TrefoilCompensationCoefficient = t,
                        QuadrafoilCompensationCoefficient = 0
                    })
                ];
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                logger.LogHtmlInformation("Trefoil", HtmlHeaderLevelEnum.Header2, htmlGuid.LoggingHtml());

                ItemsPoints = [];
                foreach (var item in Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SelectItem = item;
                    if (await CatchImagesAsync(item, imageDirectory, aodWaveDirectory, htmlGuid, cancellationToken).ConfigureAwait(false) == false) continue;

                    item.IsOk = true;
                    ItemsPoints = [.. ItemsPoints, new Point(item.TrefoilCompensationCoefficient, item.MaxItem?.ImageMaxGrayValue ?? 0d)];
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                }

                var maxItem = Items.OrderByDescending(t => t.MaxItem?.ImageMaxGrayValue ?? 0).First();

                logger.LogHtmlInformation("OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    MaxItemTrefoilCompensationCoefficient = maxItem.TrefoilCompensationCoefficient.ToString("0.###############"),
                    MaxItemTrefoilCompensationEcs = maxItem.MaxItem?.Ecs,
                    MaxItemTrefoilCompensationImageMaxGrayValue = maxItem.MaxItem?.ImageMaxGrayValue,
                    Table = new HtmlTable([
                        ..Items.Select(t => new
                        {
                            TrefoilCompensationCoefficient = t.TrefoilCompensationCoefficient.ToString("0.###############"),
                            t.ChirpAodWaveFilePath,
                            Signals = new HtmlPlot2DLinesChart([(string.Empty, t.AodWaveSignals)], string.Empty),
                            Fouriers = new HtmlPlot2DLinesChart([(string.Empty, t.AodWaveSignalsFourier)], string.Empty)
                        })
                    ]),
                    Plot = new HtmlPlot2DLinesChart([(string.Empty, ItemsPoints)], string.Empty)
                }), htmlGuid.LoggingHtml());
            }

            if (QuadrafoilCompensationCoefficientIsEnable)
            {
                Items =
                [
                    .. Generate.LinearRange(QuadrafoilCompensationCoefficientMin, QuadrafoilCompensationCoefficientStep, QuadrafoilCompensationCoefficientMax).Select(t => new AodGenerateWaveFileTrainingChirp
                    {
                        AstigmatismCompensationCoefficient = 0,
                        SphericalAberrationCompensationCoefficient = 0,
                        SecondaryAstigmatismCompensationCoefficient = 0,
                        ComaCompensationCoefficient = 0,
                        TrefoilCompensationCoefficient = 0,
                        QuadrafoilCompensationCoefficient = t
                    })
                ];
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                logger.LogHtmlInformation("Quadrafoil", HtmlHeaderLevelEnum.Header2, htmlGuid.LoggingHtml());

                ItemsPoints = [];
                foreach (var item in Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SelectItem = item;
                    if (await CatchImagesAsync(item, imageDirectory, aodWaveDirectory, htmlGuid, cancellationToken).ConfigureAwait(false) == false) continue;

                    item.IsOk = true;
                    ItemsPoints = [.. ItemsPoints, new Point(item.QuadrafoilCompensationCoefficient, item.MaxItem?.ImageMaxGrayValue ?? 0d)];
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                }

                var maxItem = Items.OrderByDescending(t => t.MaxItem?.ImageMaxGrayValue ?? 0).First();

                logger.LogHtmlInformation("OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    MaxItemQuadrafoilCompensationCoefficient = maxItem.QuadrafoilCompensationCoefficient.ToString("0.###############"),
                    MaxItemQuadrafoilCompensationEcs = maxItem.MaxItem?.Ecs,
                    MaxItemQuadrafoilCompensationImageMaxGrayValue = maxItem.MaxItem?.ImageMaxGrayValue,
                    Table = new HtmlTable([
                        ..Items.Select(t => new
                        {
                            QuadrafoilCompensationCoefficient = t.QuadrafoilCompensationCoefficient.ToString("0.###############"),
                            t.ChirpAodWaveFilePath,
                            Signals = new HtmlPlot2DLinesChart([(string.Empty, t.AodWaveSignals)], string.Empty),
                            Fouriers = new HtmlPlot2DLinesChart([(string.Empty, t.AodWaveSignalsFourier)], string.Empty)
                        })
                    ]),
                    Plot = new HtmlPlot2DLinesChart([(string.Empty, ItemsPoints)], string.Empty)
                }), htmlGuid.LoggingHtml());
            }

            isSuccess = true;
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                logger.LogWarning("Canceled");
                return;
            }

            logger.LogError(ex, "Step3");
        }
        finally
        {
            logger.LogHtmlInformation(htmlGuid.LoggedEndHtml($"Step3Training{(isSuccess ? "OK" : "Error")}"));
        }
    }

    private async Task<bool> CatchImagesAsync(AodGenerateWaveFileTrainingChirp item, string imageDirectory, string aodWaveDirectory, Guid htmlGuid, CancellationToken cancellationToken)
    {
        try
        {
            return await Task.Run(async () =>
            {
                if (GenerateChirpAodWaveFile() == false) return false;

                logger.LogHtmlInformation($"""
                                           {item.AstigmatismCompensationCoefficient:0.###############}astigmatism
                                           {item.SphericalAberrationCompensationCoefficient:0.###############}sphericalAberration
                                           {item.SecondaryAstigmatismCompensationCoefficient:0.###############}secondaryAstigmatism
                                           {item.ComaCompensationCoefficient:0.###############}coma
                                           {item.TrefoilCompensationCoefficient:0.###############}trefoil
                                           {item.QuadrafoilCompensationCoefficient:0.###############}quadrafoil
                                           """, HtmlHeaderLevelEnum.Header3, htmlGuid.LoggingHtml());
                logger.LogHtmlInformation("Aod Wave", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    item.ChirpAodWaveFilePath,
                    GenerateChirpAodWaveParamDto.BandWidth,
                    GenerateChirpAodWaveParamDto.CenterFrequency,
                    GenerateChirpAodWaveParamDto.SoundPackageLength,
                    MonotonicTypeEnum = GenerateChirpAodWaveParamDto.FunctionMonotonicTypeEnum,
                    GenerateChirpAodWaveParamDto.SampleRate,
                    GenerateChirpAodWaveParamDto.Amplitude,
                    GenerateChirpAodWaveParamDto.AodWaveDirectory,
                    GenerateChirpAodWaveParamDto.ZeroSampleCount,
                    GenerateChirpAodWaveParamDto.EndpointSampleCount,
                    item.AstigmatismCompensationCoefficient,
                    item.SphericalAberrationCompensationCoefficient,
                    item.SecondaryAstigmatismCompensationCoefficient,
                    Signals = new HtmlTab(new
                    {
                        AodWaveSignals = new HtmlPlot2DLinesChart([(string.Empty, item.AodWaveSignals)], string.Empty),
                        AodWaveSignalsFourier = new HtmlPlot2DLinesChart([(string.Empty, item.AodWaveSignalsFourier)], string.Empty),
                        AodWaveFlatnessLinearFrequencySignals = new HtmlPlot2DLinesChart([(string.Empty, item.AodWaveFlatnessLinearFrequencySignals)], string.Empty),
                        AodWaveFlatnessTotalFrequencySignals = new HtmlPlot2DLinesChart([(string.Empty, item.AodWaveFlatnessTotalFrequencySignals)], string.Empty),
                        AodWaveFlatnessAstigmatismCompensationSignals = new HtmlPlot2DLinesChart([(string.Empty, item.AodWaveFlatnessAstigmatismCompensationSignals)], string.Empty),
                        AodWaveFlatnessSphericalAberrationCompensationSignals = new HtmlPlot2DLinesChart([(string.Empty, item.AodWaveFlatnessSphericalAberrationCompensationSignals)], string.Empty),
                        AodWaveFlatnessSecondaryAstigmatismCompensationSignals = new HtmlPlot2DLinesChart([(string.Empty, item.AodWaveFlatnessSecondaryAstigmatismCompensationSignals)], string.Empty),
                        AodWaveFlatnessComaCompensationSignals = new HtmlPlot2DLinesChart([(string.Empty, item.AodWaveFlatnessComaCompensationSignals)], string.Empty),
                        AodWaveFlatnessTrefoilCompensationSignals = new HtmlPlot2DLinesChart([(string.Empty, item.AodWaveFlatnessTrefoilCompensationSignals)], string.Empty),
                        AodWaveFlatnessQuadrafoilCompensationSignals = new HtmlPlot2DLinesChart([(string.Empty, item.AodWaveFlatnessQuadrafoilCompensationSignals)], string.Empty),
                    })
                }), htmlGuid.LoggingHtml());
                logger.LogHtmlInformation($"ECS: [{EcsMin}, {EcsMax}] STEP: {EcsStep}", HtmlHeaderLevelEnum.Header4, htmlGuid.LoggingHtml());

                foreach (var (index, ecs) in Generate.LinearRange(EcsMin, EcsStep, EcsMax).Select((t, i) => (Index: i, Ecs: t)))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    laserViewModel.SetPrescanAODWaveProfileByCoefficient(OpticsMagTypeEnum, PrescanLaserLightInformation);
                    laserViewModel.SetChirpAODWaveProfileList([AODWaveformProfileFactory.CreateChirp(OpticsAODElectrodeEnum.Electrode1, item.ChirpAodWaveFilePath)]);

                    afViewModel.ToggleBrightFieldEnable(false);
                    afViewModel.SetSensorEcsValue(ecs);
                    if (index == 0) await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

                    using var darkFieldImageDto = laserViewModel.GetDarkFieldLineScanImage(
                        CalChipSiteModelEnum.ChuckModel,
                        FindPosition,
                        (true, null),
                        true,
                        CIBConfiguration,
                        XWidthPixel,
                        OpticsMagTypeEnum,
                        StageSpeedEnum,
                        stageCoordinateSystemEnum: StageCoordinateSystemEnum.Bright);

                    var filePath = $"{imageDirectory}\\{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}" +
                                   $"_{item.AstigmatismCompensationCoefficient:0.###############}astigmatism" +
                                   $"_{item.SphericalAberrationCompensationCoefficient:0.###############}sphericalAberration" +
                                   $"_{item.SecondaryAstigmatismCompensationCoefficient:0.###############}secondaryAstigmatism" +
                                   $"_{item.ComaCompensationCoefficient:0.###############}coma" +
                                   $"_{item.TrefoilCompensationCoefficient:0.###############}trefoil" +
                                   $"_{item.QuadrafoilCompensationCoefficient:0.###############}quadrafoil" +
                                   $".jpg";
                    filePath = FileHelper.GetEnsureLongPathSupport(filePath);

                    HalconHelper.Save(darkFieldImageDto.Image, filePath);

                    var (maxGrayValue, maxGrayPoints, _, _) = HalconHelper.GetMaxMinGrayValue(darkFieldImageDto.Image, RoiRect);
                    item.Items =
                    [
                        .. item.Items,
                        new AodGenerateWaveFileTrainingChirpItem
                        {
                            Ecs = ecs,
                            FilePath = filePath,
                            ImageMaxGrayValue = maxGrayValue
                        }
                    ];
                    logger.LogHtmlInformation($"{ecs}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                    {
                        OpticsMagTypeEnum,
                        PrescanCoefficient = PrescanLaserLightInformation,
                        item.ChirpAodWaveFilePath,
                        FindPosition,
                        XWidthPixel,
                        StageSpeedEnum,
                        ecs,
                        Image = new HtmlImage(filePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(RoiRect), .. maxGrayPoints.Select(t => new HtmlImageCrossOverlay(t))]),
                        maxGrayValue
                    }), htmlGuid.LoggingHtml());
                }

                logger.LogHtmlInformation("OK", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    ECSOfGray = new HtmlPlot2DLinesChart([(string.Empty, item.EcsPoints)], string.Empty),
                    MaxItemEcs = item.MaxItem?.Ecs,
                    MaxItemImageMaxGrayValue = item.MaxItem?.ImageMaxGrayValue
                }), htmlGuid.LoggingHtml());

                return true;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException) throw;

            logger.LogError(ex, "Catch Images");

            return false;
        }

        bool GenerateChirpAodWaveFile()
        {
            try
            {
                item.Items = [];

                var (aodWaveFilePath,
                    aodWaveFlatnessLinearFrequencySignals,
                    aodWaveFlatnessTotalFrequencySignals,
                    aodWaveFlatnessAstigmatismCompensationSignals,
                    aodWaveFlatnessSphericalAberrationCompensationSignals,
                    aodWaveFlatnessSecondaryAstigmatismCompensationSignals,
                    aodWaveFlatnessComaCompensationSignals,
                    aodWaveFlatnessTrefoilCompensationSignals,
                    aodWaveFlatnessQuadrafoilCompensationSignals,
                    _,
                    aodWaveSignals,
                    aodWaveSignalsFourier,
                    _) = AodWaveGenerator.GenerateChirpAodWaveFile(
                    GenerateChirpAodWaveParamDto.BandWidth,
                    GenerateChirpAodWaveParamDto.CenterFrequency,
                    GenerateChirpAodWaveParamDto.SoundPackageLength,
                    GenerateChirpAodWaveParamDto.FunctionMonotonicTypeEnum,
                    GenerateChirpAodWaveParamDto.SampleRate,
                    GenerateChirpAodWaveParamDto.Amplitude,
                    aodWaveDirectory,
                    zeroSampleCount: GenerateChirpAodWaveParamDto.ZeroSampleCount,
                    endpointSampleCount: GenerateChirpAodWaveParamDto.EndpointSampleCount,
                    astigmatismCompensationCoefficient: item.AstigmatismCompensationCoefficient,
                    sphericalAberrationCompensationCoefficient: item.SphericalAberrationCompensationCoefficient,
                    secondaryAstigmatismCompensationCoefficient: item.SecondaryAstigmatismCompensationCoefficient,
                    comaCompensationCoefficient: item.ComaCompensationCoefficient,
                    trefoilCompensationCoefficient: item.TrefoilCompensationCoefficient,
                    quadrafoilCompensationCoefficient: item.QuadrafoilCompensationCoefficient,
                    generateRetryTimes: 1000);

                item.ChirpAodWaveFilePath = aodWaveFilePath;
                item.AodWaveFlatnessLinearFrequencySignals = aodWaveFlatnessLinearFrequencySignals;
                item.AodWaveFlatnessTotalFrequencySignals = aodWaveFlatnessTotalFrequencySignals;
                item.AodWaveFlatnessAstigmatismCompensationSignals = aodWaveFlatnessAstigmatismCompensationSignals;
                item.AodWaveFlatnessSphericalAberrationCompensationSignals = aodWaveFlatnessSphericalAberrationCompensationSignals;
                item.AodWaveFlatnessSecondaryAstigmatismCompensationSignals = aodWaveFlatnessSecondaryAstigmatismCompensationSignals;
                item.AodWaveFlatnessComaCompensationSignals = aodWaveFlatnessComaCompensationSignals;
                item.AodWaveFlatnessTrefoilCompensationSignals = aodWaveFlatnessTrefoilCompensationSignals;
                item.AodWaveFlatnessQuadrafoilCompensationSignals = aodWaveFlatnessQuadrafoilCompensationSignals;
                item.AodWaveSignals = aodWaveSignals;
                item.AodWaveSignalsFourier = aodWaveSignalsFourier;

                return true;
            }
            catch (Exception ex)
            {
                logger.LogHtmlError(ex, "Generate Aod Wave Error", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    item.AstigmatismCompensationCoefficient,
                    item.SphericalAberrationCompensationCoefficient,
                    item.SecondaryAstigmatismCompensationCoefficient,
                    item.ComaCompensationCoefficient,
                    item.TrefoilCompensationCoefficient,
                    item.QuadrafoilCompensationCoefficient
                }), htmlGuid.LoggingHtml());

                return false;
            }
        }
    }

    [RelayCommand]
    private async Task SavePictureAsync(AodGenerateWaveFileTrainingChirpItem aodGenerateWaveFileTrainingChirpItem)
    {
        try
        {
            await Task.Run(() =>
            {
                var tryShowSaveFilePathDialog = dialogWindowProvider.TryShowSaveFilePathDialog(".jpg", out var saveFilePath);
                if (tryShowSaveFilePathDialog == false) return;

                System.IO.File.Copy(aodGenerateWaveFileTrainingChirpItem.FilePath, saveFilePath, true);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name}: Grabbing Image Failed", nameof(GrabbingDarkImagePointToPointWindowViewModel));
        }
    }

    [RelayCommand]
    private void Close() => CloseView(true);
}

public sealed partial class AodGenerateWaveFileTrainingChirp : ObservableObject
{
    [ObservableProperty]
    private double _astigmatismCompensationCoefficient;

    [ObservableProperty]
    private double _sphericalAberrationCompensationCoefficient;

    [ObservableProperty]
    private double _secondaryAstigmatismCompensationCoefficient;

    [ObservableProperty]
    private double _comaCompensationCoefficient;

    [ObservableProperty]
    private double _trefoilCompensationCoefficient;

    [ObservableProperty]
    private double _quadrafoilCompensationCoefficient;

    [ObservableProperty]
    private string _chirpAodWaveFilePath = string.Empty;

    [ObservableProperty]
    private Point[] _aodWaveFlatnessLinearFrequencySignals = [];

    [ObservableProperty]
    private Point[] _aodWaveFlatnessTotalFrequencySignals = [];

    [ObservableProperty]
    private Point[] _aodWaveFlatnessAstigmatismCompensationSignals = [];

    [ObservableProperty]
    private Point[] _aodWaveFlatnessSphericalAberrationCompensationSignals = [];

    [ObservableProperty]
    private Point[] _aodWaveFlatnessSecondaryAstigmatismCompensationSignals = [];

    [ObservableProperty]
    private Point[] _aodWaveFlatnessComaCompensationSignals = [];

    [ObservableProperty]
    private Point[] _aodWaveFlatnessTrefoilCompensationSignals = [];

    [ObservableProperty]
    private Point[] _aodWaveFlatnessQuadrafoilCompensationSignals = [];

    [ObservableProperty]
    private Point[] _aodWaveSignals = [];

    [ObservableProperty]
    private Point[] _aodWaveSignalsFourier = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaxItem))]
    [NotifyPropertyChangedFor(nameof(EcsPoints))]
    private AodGenerateWaveFileTrainingChirpItem[] _items = [];

    [ObservableProperty]
    private bool _isOk;

    public AodGenerateWaveFileTrainingChirpItem? MaxItem => Items.Length > 0 ? Items.OrderByDescending(t => t.ImageMaxGrayValue).First() : null;

    public Point[] EcsPoints => [.. Items.Select(t => new Point(t.Ecs, t.ImageMaxGrayValue))];
}

public sealed partial class AodGenerateWaveFileTrainingChirpItem : ObservableObject
{
    [ObservableProperty]
    private double _ecs;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private double _imageMaxGrayValue;
}