using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Exceptions;
using Core.Models.Models;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserAutoFocusCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserAutoFocusCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Find a Position", DefaultIsNextEnable = true },
        new() { StepName = "A Brightness" },
        new() { StepName = "B Brightness" }
    ];

    #region 界面相关

    #region Calibrate

    private LaserAutoFocusDto? _lastAfBrightnessDto;

    [ObservableProperty]
    private ObservableCollection<LaserAutoFocusDto> _laserAutoFocusDtoList = [];

    [ObservableProperty]
    private ObservableCollection<Point> _fList = [];

    [ObservableProperty]
    private ObservableCollection<Point> _nList = [];

    [ObservableProperty]
    private LaserAutoFocusDto? _selectedLaserAutoFocusDto;

    [ObservableProperty]
    private LaserAutoFocusDto? _resultLaserAutoFocusDto;

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private LaserAutoFocusDto? _reviewDto;

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private LaserAutoFocusCache _cache = new();

    [ObservableProperty]
    private LaserAutoFocusDto _calibration = new();

    [ObservableProperty]
    private MicroscopeCalChipDto _microscopeCalChip = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (CalibrationStatusService.GetAdsCalibrationIsOKStatus() == false)
        {
            DialogWindowProvider.ShowDialog("The ADS precondition is Failure", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<MicroscopeFocusItemDto>(out _, out var errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<MicroscopeCalChipDto>(out var microscopeCalChip, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        MicroscopeCalChip = microscopeCalChip;

        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<LaserAutoFocusCache>();
        Calibration = CacheProvider.GetOrDefault<LaserAutoFocusDto>();

        return isHasCache || CacheProvider.Set(Cache, cancellationToken);
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Cache.FindPosition = MicroscopeCalChip.ShinyWaferPosition;
        StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(Cache.FindPosition);
        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewDto = Calibration.Clone();

        return ReviewDto.IsCalibrated;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                _lastAfBrightnessDto = null;
                Cache.IsA = true;
                StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(Cache.FindPosition);
                return true;

            case 1 or 2:
                var isCalibrated = CalibrationStepIndex == 2;

                if (ResultLaserAutoFocusDto is null)
                {
                    DialogWindowProvider.TryShowDialog("Please find current!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    ResultLaserAutoFocusDto.IsCalibrated = isCalibrated;
                    if (Save(ResultLaserAutoFocusDto, cancellationToken) == false)
                    {
                        ResultLaserAutoFocusDto.IsCalibrated = false;
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                        return false;
                    }
                }

                if (CalibrationStepIndex == 1)
                {
                    _lastAfBrightnessDto = ResultLaserAutoFocusDto;
                    Cache.IsA = false;
                }

                IsCalibrated = isCalibrated;

                return true;

            default:
                return false;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand]
    private async Task GetPointAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                var result = StageViewModel.GetDarkFieldStagePosition();

                Cache.FindPosition = result;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get Point Failed", Name);
        }
    }

    [RelayCommand]
    private async Task GotoPointAsync()
    {
        try
        {
            await Task.Run(() => StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(Cache.FindPosition)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.FindPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(() =>
        {
            ClearCalibrationTemp();
            var paramName = Cache.IsA ? "A" : "B";
            Logger.LogHtmlInformation($"Param{paramName}", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                FindPosition = Cache.FindPosition.ToShortString(),
                Cache.ThresholdFMin,
                Cache.ThresholdFMax,
                Cache.ThresholdNMin,
                Cache.ThresholdNMax,
                Cache.ThresholdCurrentMin,
                Cache.ThresholdCurrentMax,
                Cache.FindInterval
            }), HtmlLogUniqueId.LoggingHtml());

            StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(Cache.FindPosition);

            // NSC模式On
            AfViewModel.ToggleDarkFieldEnable(false);
            AfViewModel.GetSensorNscCurveIsOk();
            AfViewModel.ToggleDarkFieldEnable(true);

            var times = 1;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (f, n) = AfViewModel.GetSensorFnValue(Cache.IsA);
                var current = AfViewModel.GetSensorCurrentValue(Cache.IsA);

                var afBrightnessDto = Cache.IsA ? new LaserAutoFocusDto { Fa = f, Na = n, CurrentA = current } : new LaserAutoFocusDto { Fb = f, Nb = n, CurrentB = current };
                SynchronizationContextProvider.Send(() =>
                {
                    LaserAutoFocusDtoList.Add(afBrightnessDto);
                    FList = [.. FList, new Point(Cache.IsA ? afBrightnessDto.CurrentA : afBrightnessDto.CurrentB, Cache.IsA ? afBrightnessDto.Fa : afBrightnessDto.Fb)];
                    NList = [.. NList, new Point(Cache.IsA ? afBrightnessDto.CurrentA : afBrightnessDto.CurrentB, Cache.IsA ? afBrightnessDto.Na : afBrightnessDto.Nb)];
                });

                Logger.LogHtmlInformation($"{Name} time: {times}", HtmlHeaderLevelEnum.Header4, new HtmlQuote(Cache.IsA
                    ? new
                    {
                        FindPosition = Cache.FindPosition.ToShortString(),
                        afBrightnessDto.Fa,
                        afBrightnessDto.Na,
                        afBrightnessDto.CurrentA
                    }
                    : new
                    {
                        FindPosition = Cache.FindPosition.ToShortString(),
                        afBrightnessDto.Fb,
                        afBrightnessDto.Nb,
                        afBrightnessDto.CurrentB
                    }), HtmlLogUniqueId.LoggingHtml());

                if (Cache.ThresholdNMin <= n && n <= Cache.ThresholdNMax && Cache.ThresholdFMin <= f && f <= Cache.ThresholdFMax)
                {
                    SelectedLaserAutoFocusDto = afBrightnessDto;
                    ResultLaserAutoFocusDto = _lastAfBrightnessDto ?? SelectedLaserAutoFocusDto.Clone();
                    if (Cache.IsA)
                    {
                        ResultLaserAutoFocusDto.Fa = SelectedLaserAutoFocusDto.Fa;
                        ResultLaserAutoFocusDto.Na = SelectedLaserAutoFocusDto.Na;
                        ResultLaserAutoFocusDto.CurrentA = SelectedLaserAutoFocusDto.CurrentA;
                    }
                    else
                    {
                        ResultLaserAutoFocusDto.Fb = SelectedLaserAutoFocusDto.Fb;
                        ResultLaserAutoFocusDto.Nb = SelectedLaserAutoFocusDto.Nb;
                        ResultLaserAutoFocusDto.CurrentB = SelectedLaserAutoFocusDto.CurrentB;
                    }

                    break;
                }

                // 判断电流改变方向，f、n>Threshold(f、n)Min时递减 f、n<Threshold(f、n)Min时递增
                var interval = (n > Cache.ThresholdNMax && f >= Cache.ThresholdFMin)
                               || (n >= Cache.ThresholdNMin && f > Cache.ThresholdFMax)
                    ? -Cache.FindInterval
                    : (n < Cache.ThresholdNMin && f <= Cache.ThresholdFMax)
                      || (n <= Cache.ThresholdNMax && f < Cache.ThresholdFMin)
                        ? Cache.FindInterval
                        : throw new CalibrationException("f and n orientation discrepancy");

                current += interval;
                if (current > Cache.ThresholdCurrentMax || current < Cache.ThresholdCurrentMin)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment($"{Name}Error: Current Value({current}) " +
                                                                                             $"Out Of Range({Cache.ThresholdCurrentMin},{Cache.ThresholdCurrentMax})."), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                AfViewModel.SetSensorCurrentValue(current, Cache.IsA);

                Thread.Sleep(100);

                times++;
            }

            var result = ResultLaserAutoFocusDto is not null;
            if (result)
                Logger.LogHtmlInformation(result ? $"{paramName}OK" : $"{paramName}Failed", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    ResultLaserAutoFocusDto!.Fa,
                    ResultLaserAutoFocusDto.Na,
                    ResultLaserAutoFocusDto.CurrentA,
                    ResultLaserAutoFocusDto.Fb,
                    ResultLaserAutoFocusDto.Nb,
                    ResultLaserAutoFocusDto.CurrentB,
                    Cache.ThresholdFMin,
                    Cache.ThresholdFMax,
                    Cache.ThresholdNMin,
                    Cache.ThresholdNMax,
                    Cache.ThresholdCurrentMin,
                    Cache.ThresholdCurrentMax
                }), HtmlLogUniqueId.LoggingHtml());
            return result;
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        if (ReviewDto is null)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(() =>
        {
            ClearCalibrationTemp();

            ReviewDto.IsVerified = false;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                FindPosition = Cache.FindPosition.ToShortString(),
                Cache.ThresholdFMin,
                Cache.ThresholdFMax,
                Cache.ThresholdNMin,
                Cache.ThresholdNMax,
                Cache.ThresholdCurrentMin,
                Cache.ThresholdCurrentMax,
                Cache.FindInterval
            }), HtmlLogUniqueId.LoggingHtml());

            StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(Cache.FindPosition);
            AfViewModel.ToggleDarkFieldEnable(false);
            AfViewModel.GetSensorNscCurveIsOk();
            AfViewModel.ToggleDarkFieldEnable(true);

            // 使用校准后的电流，读当前的fa na fb nb
            var (fa, na) = AfViewModel.GetSensorFnValue(true);
            var (fb, nb) = AfViewModel.GetSensorFnValue(false);

            Cache.VerifyResultFa = fa;
            Cache.VerifyResultFb = fb;
            Cache.VerifyResultNa = na;
            Cache.VerifyResultNb = nb;

            var result = Cache.ThresholdNMin < na && na < Cache.ThresholdNMax && Cache.ThresholdNMin < nb && nb < Cache.ThresholdNMax
                         && Cache.ThresholdFMin < fa && fa < Cache.ThresholdFMax && Cache.ThresholdFMin < fb && fb < Cache.ThresholdFMax;

            Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Fa = fa,
                Na = na,
                Fb = fb,
                Nb = nb,
                Cache.ThresholdFMin,
                Cache.ThresholdFMax,
                Cache.ThresholdNMin,
                Cache.ThresholdNMax
            }), HtmlLogUniqueId.LoggingHtml());

            ReviewDto.IsVerified = result;
            if (Save(ReviewDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name}Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                ReviewDto.IsVerified = false;
                return false;
            }

            DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}, Fa: ({fa:f3}) Na: ({na:f3}) Fb: ({fb:f3}) Nb: ({nb:f3})", DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        }).ConfigureAwait(false);
    }

    private bool Save(LaserAutoFocusDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibration = dto.Clone();

        return CacheProvider.Set(dto, cancellationToken) && CacheProvider.Set(Cache, cancellationToken);
    });

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(() => { LaserAutoFocusDtoList.Clear(); });
        FList = [];
        NList = [];
        SelectedLaserAutoFocusDto = null;
        ResultLaserAutoFocusDto = null;
    }

    #endregion 校准
}