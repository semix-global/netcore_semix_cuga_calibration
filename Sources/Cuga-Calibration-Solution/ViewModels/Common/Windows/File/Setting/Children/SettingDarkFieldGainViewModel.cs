using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Setting;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Extensions;
using Net.Utilities.Helper.Enum;
using Net.Utilities.Models;
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
    ICacheProvider cacheProvider,
    IDialogWindowProvider dialogWindowProvider)
    : SettingWindowViewModelBase
{
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

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

    [RelayCommand]
    private void Loaded()
    {
        SettingDarkFieldGainParam = SettingDarkFieldGainParamList.SingleOrDefault(t => t.PmtId == PmtId && t.ChannelId == ChannelId);
    }

    [RelayCommand]
    private void SelectedPmtIdChanged(int pmtId)
    {
        SettingDarkFieldGainParam = SettingDarkFieldGainParamList.SingleOrDefault(t => t.PmtId == pmtId && t.ChannelId == ChannelId);
    }

    [RelayCommand]
    private void SelectedChannelChanged(int channelId)
    {
        SettingDarkFieldGainParam = SettingDarkFieldGainParamList.SingleOrDefault(t => t.PmtId == PmtId && t.ChannelId == channelId);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task AutoPmtGainAsync(double? coefficient, CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            try
            {
                if (coefficient is null) return;

                var microscopeCalChipDto = cacheProvider.GetOrDefault<MicroscopeCalChipDto>();

                if (microscopeCalChipDto.IsOk(out var errorMessage) == false)
                {
                    dialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                var (isSuccess, gain) = await AutoPmtGainAsync(coefficient.Value, microscopeCalChipDto.HazePosition, CalChipSiteModelEnum.HazeModel, Guid.NewGuid(), cancellationToken, true, PmtId, ChannelId).ConfigureAwait(false);
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
        Guid htmlLogUniqueId,
        CancellationToken cancellationToken,
        bool isContainsEnd = false,
        int pmtId = 8,
        int channelId = 3,
        OpticsMagTypeEnum opticsMagTypeEnum = OpticsMagTypeEnum.High)
    {
        var isSuccess = false;
        try
        {
            try
            {
                logger.LogHtmlInformation("Auto PmtGain Start", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

                logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
                {
                    OpticsMagTypeEnum = opticsMagTypeEnum,
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

                // 自动聚焦
                var isAutoFocus = afViewModel.SetDarkFieldAutoFocus(null, opticsMagTypeEnum, calChipSiteModelEnum);
                if (isAutoFocus)
                    afViewModel.ToggleDarkFieldEnable(true);
                laserViewModel.SendOpticsMagType(opticsMagTypeEnum);
                laserViewModel.SendPrescanByCoefficient(opticsMagTypeEnum, coefficient);
                laserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Through);
                laserViewModel.ToggleEnableAutoGain(false);
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
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);

                    var result = laserViewModel.GetPmtDataList(pmtId, channelId);
                    if (isSuccess == false) return (false, 0);
                    PlotList = [.. PlotList, new WpfPlotModel($"Gain: {gain}", result.ToPoints(), (SettingDarkFieldGainParam.GainMin, SettingDarkFieldGainParam.GainMax, gain))];
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
                var resultTargetGain = laserViewModel.GetPmtDataList(pmtId, channelId);
                if (isSuccess == false) return (false, 0);
                PlotList = [.. PlotList, new WpfPlotModel($"{coefficient:f3} OK: {targetGain}", resultTargetGain.ToPoints(), (SettingDarkFieldGainParam.GainMin, SettingDarkFieldGainParam.GainMax, targetGain))];

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
                laserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Scan);
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
                    $"AutoGain_Coefficient({coefficient})_Mag({EnumHelper.ToDescriptionString(opticsMagTypeEnum)})_Position({position.ToShortString()})_CalChip({EnumHelper.ToDescriptionString(calChipSiteModelEnum)})_{(isSuccess ? "OK" : "Failed")}"));
            }
        }
    }
}