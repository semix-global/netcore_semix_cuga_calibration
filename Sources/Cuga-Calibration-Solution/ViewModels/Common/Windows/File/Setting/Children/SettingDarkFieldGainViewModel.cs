using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Setting;
using Local.NoSQL.DB.Providers.Extensions;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Behaviors;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;

[IOCAppService(ServiceType = typeof(SettingDarkFieldGainViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class SettingDarkFieldGainViewModel(
    ILogger<SettingDarkFieldGainViewModel> logger,
    LaserViewModel laserViewModel,
    StageViewModel stageViewModel,
    AfViewModel afViewModel,
    ApplicationCookie applicationCookie,
    ICacheProvider cacheProvider,
    IDialogWindowProvider dialogWindowProvider)
    : SettingWindowViewModelBase
{
    public ApplicationCookie ApplicationCookie => applicationCookie;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private ObservableCollection<SettingDarkFieldGainParam> _settingDarkFieldGainParamList = [];

    [ObservableProperty]
    private SettingDarkFieldGainParam _settingDarkFieldGainParam = new();

    [ObservableProperty]
    private List<WpfPlotModel> _plotList = [];

    [ObservableProperty]
    private List<int> _pmtIdList = [.. CalibrationConstantsHelper.PmtIds];

    [ObservableProperty]
    private List<int> _channelIdList = [.. CalibrationConstantsHelper.ChannelIds];

    [ObservableProperty]
    private int _pmtId = 8;

    [ObservableProperty]
    private int _channelId = 3;

    /// <summary>
    /// 抓取次数
    /// </summary>
    [ObservableProperty]
    private int _catchCount = 10;

    [RelayCommand]
    private void Loaded()
    {
        SettingDarkFieldGainParam = SettingDarkFieldGainParamList.Single(t => t.PmtId == PmtId && t.ChannelId == ChannelId);
    }

    [RelayCommand]
    private void SelectedPmtIdChanged(int pmtId)
    {
        SettingDarkFieldGainParam = SettingDarkFieldGainParamList.Single(t => t.PmtId == pmtId && t.ChannelId == ChannelId);
    }

    [RelayCommand]
    private void SelectedChannelChanged(int channelId)
    {
        SettingDarkFieldGainParam = SettingDarkFieldGainParamList.Single(t => t.PmtId == PmtId && t.ChannelId == channelId);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task AutoPmtGainAsync(double? coefficient, CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            try
            {
                if (coefficient is null) return;

                var microscopeCalChipCache = cacheProvider.GetOrDefault<MicroscopeCalChipCache>();
                var microscopeCalChipDto = cacheProvider.GetOrDefault<MicroscopeCalChipDto>();

                if (microscopeCalChipDto.IsOk(out var errorMessage) == false)
                {
                    dialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                var (isSuccess, gain) = await AutoPmtGainAsync(
                    coefficient.Value,
                    microscopeCalChipCache.HazePosition,
                    CalChipSiteModelEnum.HazeModel,
                    ProductivityInformation,
                    Guid.NewGuid(),
                    cancellationToken,
                    true,
                    PmtId,
                    ChannelId).ConfigureAwait(false);
                if (isSuccess == false) return;

                dialogWindowProvider.ShowDialog($"Auto Pmt Gain Success, Gain: {gain}");
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    dialogWindowProvider.ShowDialog("Canceled!");
                    return;
                }

                logger.LogError(ex, "{@Name}: Auto Pmt Gain Exception", nameof(SettingDarkFieldGainViewModel));
            }
        }, cancellationToken);
    }

    [RelayCommand]
    private void ShowPlot(List<double> list)
    {
        dialogWindowProvider.ShowPlot([.. list]);
    }

    public async Task<(bool IsSuccess, double Gain)> AutoPmtGainAsync(
        double coefficient,
        Point position,
        CalChipSiteModelEnum calChipSiteModelEnum,
        ProductivityInformation productivityInformation,
        Guid htmlLogUniqueId,
        CancellationToken cancellationToken,
        bool isContainsEnd = false,
        int pmtId = 8,
        int channelId = 3,
        OpticsIlluminationModeEnum opticsIlluminationModeEnum = CalibrationConstantsHelper.MainOpticsIlluminationModeEnum)
    {
        var isSuccess = false;
        try
        {
            try
            {
                logger.LogHtmlInformation("Auto PmtGain Start", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

                logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
                {
                    productivityInformation,
                    coefficient,
                    position,
                    calChipSiteModelEnum
                }), htmlLogUniqueId.LoggingHtml());

                var gainCoefficientsParam = SettingDarkFieldGainParam.GainOfCoefficientList.SingleOrDefault(t => t.Coefficient - coefficient == 0);
                if (gainCoefficientsParam is null)
                {
                    gainCoefficientsParam = new GainOfCoefficientParam();
                    SettingDarkFieldGainParam.GainOfCoefficientList.Add(gainCoefficientsParam);
                }

                laserViewModel.SetGain(-10);
                stageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(position, calChipSiteModelEnum);

                afViewModel.ToggleDarkFieldEnable(true);
                laserViewModel.ToggleOpticsMagType(opticsIlluminationModeEnum, productivityInformation);
                laserViewModel.SetPrescanAODWaveProfileByCoefficient(opticsIlluminationModeEnum, productivityInformation, coefficient);
                laserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);
                laserViewModel.ToggleEnableAutoGainControl(false);
                laserViewModel.ToggleEnableL0K(false);

                PlotList = [];
                var targetGain = SettingDarkFieldGainParam.GainMin;
                foreach (var gain in ((double[])
                         [
                             SettingDarkFieldGainParam.GainMin,
                             .. Enumerable.Range(1, (int)Math.Floor((SettingDarkFieldGainParam.GainMax - SettingDarkFieldGainParam.GainMin) / SettingDarkFieldGainParam.GainInterval))
                                 .Select(x => SettingDarkFieldGainParam.GainMin + x * SettingDarkFieldGainParam.GainInterval),
                             SettingDarkFieldGainParam.GainMax
                         ]).Distinct())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    laserViewModel.SetGain(gain);
                    await Task.Delay(300, cancellationToken).ConfigureAwait(false);

                    var pmtDataList = laserViewModel.GetCIBOfPMTDataList(CatchCount, pmtId, channelId);
                    var result = Enumerable.Range(0, pmtDataList.First().Count)
                        .Select(t => pmtDataList.Select(tt => tt[t]).Average())
                        .ToList();
                    PlotList = [.. PlotList, new WpfPlotModel($"Gain: {gain}", [.. result.ToPoints()], (SettingDarkFieldGainParam.GainMin, SettingDarkFieldGainParam.GainMax, gain))];
                    var gainAverage = result.Skip(SettingDarkFieldGainParam.JudgeGainSkipCout).SkipLast(SettingDarkFieldGainParam.JudgeGainSkipCout).Average();

                    logger.LogHtmlInformation($"Gain: {gain}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                    {
                        Gain = gain,
                        Average = gainAverage,
                        Max = result.Max(),
                        Min = result.Min(),
                        Plot = new HtmlPlot2DLinesChart([($"Gain: {gain}", result.ToPoints())], "Gain")
                    }), htmlLogUniqueId.LoggingHtml());

                    if (gainAverage > 4000)
                    {
                        laserViewModel.SetGain(-10);
                        throw new CalibrationException("Pmt Value is too high");
                    }

                    if (gainAverage < SettingDarkFieldGainParam.TargetPmtAverageValue)
                    {
                        targetGain = gain;
                        continue;
                    }

                    if (gainAverage > SettingDarkFieldGainParam.TargetPmtAverageValue)
                    {
                        targetGain = (gain + targetGain) / 2;
                        break;
                    }

                    targetGain = gain;
                    break;
                }

                laserViewModel.SetGain(targetGain);
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);

                gainCoefficientsParam.Gain = targetGain;
                var resultTargetGain = laserViewModel.GetCIBOfPMTDataList(CatchCount, pmtId, channelId).Select(t => t.Average()).ToList();
                //if (isSuccess == false) return (false, 0);
                PlotList = [.. PlotList, new WpfPlotModel($"{coefficient:f3} OK: {targetGain}", [.. resultTargetGain.ToPoints()], (SettingDarkFieldGainParam.GainMin, SettingDarkFieldGainParam.GainMax, targetGain))];

                gainCoefficientsParam.TargetPmtAverageValue = resultTargetGain.Skip(SettingDarkFieldGainParam.JudgeGainSkipCout).SkipLast(SettingDarkFieldGainParam.JudgeGainSkipCout).Average();
                gainCoefficientsParam.TargetPmtValueList = resultTargetGain;

                logger.LogHtmlInformation($"Ok Gain: {gainCoefficientsParam.Gain}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    gainCoefficientsParam.Gain,
                    gainCoefficientsParam.TargetPmtAverageValue,
                    TargePlot = new HtmlPlot2DLinesChart([($"Gain: {gainCoefficientsParam.Gain}", gainCoefficientsParam.TargetPmtValueList.ToPoints())], "Gain"),
                    AllPlots = new HtmlPlot2DLinesChart([.. PlotList.Select(t => (t.Title, PointList: t.Points))], "All Plots")
                }), htmlLogUniqueId.LoggingHtml());

                isSuccess = true;
                return (isSuccess, targetGain);
            }
            finally
            {
                laserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);
                stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(position, calChipSiteModelEnum);
            }
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException) throw;

            logger.LogHtmlCritical(ex, "Critical", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());
            return (false, 0);
        }
        finally
        {
            if (isContainsEnd)
            {
                logger.LogHtmlInformation(htmlLogUniqueId.LoggedEndHtml(
                    $"AutoGain_Coefficient({coefficient})_Mag({productivityInformation})_Position({position})_CalChip({EnumHelper.ToDescriptionString(calChipSiteModelEnum)})_{(isSuccess ? "OK" : "Failed")}"));
            }
        }
    }
}