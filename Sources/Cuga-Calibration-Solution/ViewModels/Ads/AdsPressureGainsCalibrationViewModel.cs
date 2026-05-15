using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models;
using Core.Models.Models.Ads.PressureGains;
using Core.Utilities.SourceGenerators.Attributes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;

namespace CugaCalibration.ViewModels.Ads;

[IOCAppService(ServiceType = typeof(AdsPressureGainsCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AdsPressureGainsCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override IReadOnlyList<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select a location", DefaultIsNextEnable = true },
        new() { StepName = "Pressure Gains" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private AdsPressureGainsDto _resultAdsPressureGainsDto = new();

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private AdsPressureGainsDto? _reviewDto;

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private AdsPressureGainsCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private AdsPressureGainsDto _calibration = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<AdsPressureGainsCache>();
        Calibration = CacheProvider.GetOrDefault<AdsPressureGainsDto>();
        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.FindPosition);
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
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.FindPosition);
                return true;

            case 1:
                ResultAdsPressureGainsDto.IsCalibrated = true;
                if (Save(ResultAdsPressureGainsDto, cancellationToken) == false)
                {
                    ResultAdsPressureGainsDto.IsCalibrated = false;
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                IsCalibrated = true;

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
                var result = StageViewModel.GetMachineStagePosition();

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
            await Task.Run(() => StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.FindPosition)).ConfigureAwait(false);
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
    private async Task<bool> Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = false;
        await InvokeCalibrateAsync(() =>
        {
            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.FindPosition);

            Thread.Sleep(1000);
            Logger.LogHtmlInformation($"{Name} Start", HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Get Sensor All Pressure Trans Buffer Value Start! "), HtmlLogUniqueId.LoggingHtml());
            var transBuffer = AdsViewModel.GetSensorAllPressureTraceBufferList(TimeSpan.FromSeconds(3));

            var pressureValue1 = transBuffer.Average(x => x.PressureValue1);
            var pressureValue2 = transBuffer.Average(x => x.PressureValue2);
            var pressureValue3 = transBuffer.Average(x => x.PressureValue3);

            ResultAdsPressureGainsDto = new AdsPressureGainsDto
            {
                FindPosition = Cache.FindPosition,
                PressureValue1 = pressureValue1,
                PressureValue2 = pressureValue2,
                PressureValue3 = pressureValue3
            };
            AdsViewModel.SetSensorFeedForwardPressureValue(ResultAdsPressureGainsDto.PressureValue1, ResultAdsPressureGainsDto.PressureValue2, ResultAdsPressureGainsDto.PressureValue3);

            Logger.LogHtmlInformation("Ok", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                ResultAdsPressureGainsDto.FindPosition,
                ResultAdsPressureGainsDto.PressureValue1,
                ResultAdsPressureGainsDto.PressureValue2,
                ResultAdsPressureGainsDto.PressureValue3,
                transBuffer = new HtmlPlot2DLinesChart([
                    ("Pressure 1", transBuffer.Select(t => t.PressureValue1).ToPoints()),
                    ("Pressure 2", transBuffer.Select(t => t.PressureValue2).ToPoints()),
                    ("Pressure 3", transBuffer.Select(t => t.PressureValue3).ToPoints())
                ], "AdsPressure")
            }), HtmlLogUniqueId.LoggingHtml());
            result = true;
            return result;
        }).ConfigureAwait(false);
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> VerifyActionAsync(CancellationToken cancellationToken)
    {
        var result = false;
        await InvokeVerifyAsync(async () =>
        {
            if (ReviewDto is null)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Please select a review item!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            result = await VerifyCalibrationAsync(ReviewDto, cancellationToken);
            return result;
        }).ConfigureAwait(false);
        return result;
    }

    private async Task<bool> VerifyCalibrationAsync(AdsPressureGainsDto selectAdsPressureGainsDto, CancellationToken cancellationToken)
    {
        var result = false;
        await Task.Run(() =>
        {
            if (selectAdsPressureGainsDto is null)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Please select a review item!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            Cache.FindPosition = selectAdsPressureGainsDto.FindPosition;

            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.FindPosition);

            Thread.Sleep(1000);
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.FindPosition
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlInformation($"{Name} Start", HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Get Sensor Height, Roll, Pitch Trans Buffer Value Start! "), HtmlLogUniqueId.LoggingHtml());
            var transBuffer = AdsViewModel.GetSensorHeightRollPitchTraceBufferList(TimeSpan.FromSeconds(HostEnvironment.IsDevelopment() ? 1 : 5));

            var heightMax = transBuffer.Select(t => t.Height).Max(Math.Abs);
            var rollMax = transBuffer.Select(t => t.Roll).Max(Math.Abs);
            var pitchMax = transBuffer.Select(t => t.Pitch).Max(Math.Abs);

            var heightResult = heightMax < Cache.Threshold;
            var rollResult = rollMax < Cache.Threshold;
            var pitchResult = pitchMax < Cache.Threshold;
            result = heightResult && rollResult && pitchResult;

            Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                HeightMax = heightMax,
                RollMax = rollMax,
                PitchMax = pitchMax,
                transBuffer = new HtmlPlot2DLinesChart([
                    ("Height", transBuffer.Select(t => t.Height).ToPoints()),
                    ("Roll", transBuffer.Select(t => t.Roll).ToPoints()),
                    ("Pitch", transBuffer.Select(t => t.Pitch).ToPoints())
                ], "TransBuffer ")
            }), HtmlLogUniqueId.LoggingHtml());

            selectAdsPressureGainsDto.IsVerified = result;
            if (Save(selectAdsPressureGainsDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                selectAdsPressureGainsDto.IsVerified = false;
                return false;
            }

            DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}, HeightMax: ({heightMax:f3}) RollMax: ({rollMax:f3}) PitchMax: ({pitchMax:f3})", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        }, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private bool Save(AdsPressureGainsDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibration = dto.Clone();
        CacheProvider.Set(dto, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}