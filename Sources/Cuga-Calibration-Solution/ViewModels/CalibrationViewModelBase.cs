using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Local.SQL.DB.Providers.Models.Entities.Base.Interface;
using Microsoft.Extensions.Logging;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels;

public partial class CalibrationViewModelBase : ViewModelBase
{
    public virtual string CalibrateDirectoryName { get; set; } = string.Empty;

    public virtual string CalibrateFileName { get; set; } = string.Empty;

    public virtual string VerifyFileName { get; set; } = string.Empty;

    public Guid HtmlLogUniqueId { get; set; }

    #region 公开

    [RelayCommand]
    public async Task LoadedAsync()
    {
        try
        {
            Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingClearHtml());

            UpdateLoadingStatus();
            RefreshToken();
            await Task.Run(async () =>
            {
                if (await LoadedingAsync(_cancellationTokenSource.Token).ConfigureAwait(false) == false)
                {
                    UpdateFailedStatus();
                    Logger.LogWarning("{@Name}: Loaded Failed", Name);

                    return;
                }

                UpdateWelcomeStatus();
                Logger.LogInformation("{@Name}: Loaded Ok!", Name);
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Loaded Exception", Name);
            UpdateFailedStatus();
        }
    }

    [RelayCommand]
    public async Task CalibrateAsync()
    {
        try
        {
            UpdateLoadingStatus();
            CheckStatus();
            await Task.Run(async () =>
            {
                if (await CalibratingAsync(_cancellationTokenSource.Token).ConfigureAwait(false) == false)
                {
                    UpdateWelcomeStatus();
                    Logger.LogWarning("{@Name}: Calibrate Failed", Name);

                    return;
                }

                Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingClearHtml());
                HtmlLogUniqueId = Guid.NewGuid();

                Logger.LogHtmlInformation($"1. {Name}", HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());

                UpdateCalibrateStatus();
                Logger.LogInformation("{@Name}: Begin Calibrate!", Name);
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Calibrate Exception", Name);
            UpdateFailedStatus();
        }
    }

    [RelayCommand]
    public async Task ReviewAsync()
    {
        try
        {
            UpdateLoadingStatus();
            CheckStatus();
            await Task.Run(async () =>
            {
                if (await ReviewingAsync(_cancellationTokenSource.Token).ConfigureAwait(false) == false)
                {
                    UpdateWelcomeStatus();
                    DialogWindowProvider.ShowDialog("Please calibrate!", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                    return;
                }

                Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingClearHtml());

                UpdateReviewStatus();
                Logger.LogInformation("{@Name}: Begin Review!", Name);
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Review Exception", Name);
            UpdateFailedStatus();
        }
    }

    [RelayCommand]
    public async Task CancelAsync()
    {
        try
        {
            ViewEnum = CalibrationItemViewEnum.Loading;
            UpdateDisableAll();
            CheckStatus();
            await Task.Run(async () =>
            {
                CancelToken();

                if (0 <= CalibrationStepIndex && CalibrationStepIndex <= CalibrationSteps.Count - 1)
                {
                    Logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml("Calibrate" +
                                                                            $"_{FileHelper.RemoveInvalidFileName(ApplicationCookie.DeviceCode)}" +
                                                                            $"_{FileHelper.RemoveInvalidFileName(Name)}" +
                                                                            $"_{FileHelper.RemoveInvalidFileName(CalibrateHtmlLogFileName)}" +
                                                                            $"_Step1-Step{CalibrationStepIndex + 1}" +
                                                                            "_Cancel"));
                }

                if (await CancelingAsync().ConfigureAwait(false) == false)
                {
                    UpdateFailedStatus();
                    Logger.LogError("{@Name}: Cancel Failed", Name);

                    return;
                }

                UpdateCancelStatus();
                Logger.LogInformation("{@Name}: Cancel!", Name);
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Cancel Exception", Name);
            UpdateFailedStatus();
        }
    }

    [RelayCommand]
    public async Task PreviousAsync()
    {
        try
        {
            UpdateLoadingStatus();
            CheckStatus();
            await Task.Run(async () =>
            {
                CalibrationSteps[CalibrationStepIndex].StepIsNextEnable = CalibrationSteps[CalibrationStepIndex].DefaultIsNextEnable; // 恢复默认值

                bool isSuccess;
                try
                {
                    isSuccess = await PreviousingAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
                    if (isSuccess == false) Logger.LogHtmlCritical("Previous Critical", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                }
                catch (Exception ex)
                {
                    isSuccess = false;
                    Logger.LogHtmlCritical(ex, "Previous Critical", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                }

                if (isSuccess == false)
                {
                    UpdatePreviousNextStatus();

                    return;
                }

                CalibrationStepIndex--;
                UpdatePreviousNextStatus();

                Logger.LogInformation("{@Name}: Previous!", Name);
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Previous Exception", Name);
            UpdateFailedStatus();
        }
    }

    [RelayCommand]
    public async Task NextAsync()
    {
        try
        {
            UpdateLoadingStatus();
            CheckStatus();
            await Task.Run(async () =>
            {
                var lastStepIsNextEnable = CalibrationSteps[CalibrationStepIndex].StepIsNextEnable;
                CalibrationSteps[CalibrationStepIndex].StepIsNextEnable = CalibrationSteps[CalibrationStepIndex].DefaultIsNextEnable; // 恢复默认值

                bool isSuccess;
                try
                {
                    isSuccess = await NextingAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
                    if (isSuccess == false) Logger.LogHtmlCritical("Next Critical", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                }
                catch (Exception ex)
                {
                    isSuccess = false;
                    Logger.LogHtmlCritical(ex, "Next Critical", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                }

                if (isSuccess == false)
                {
                    CalibrationSteps[CalibrationStepIndex].StepIsNextEnable = lastStepIsNextEnable; // 失败恢复会原始的值
                    UpdatePreviousNextStatus();

                    return;
                }

                if (CalibrationStepIndex == CalibrationSteps.Count - 1)
                {
                    Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingClearHtml());

                    if (IsCalibrated)
                    {
                        DialogWindowProvider.ShowDialog($"Calibration {Name} All Ok!");
                        UpdateWelcomeStatus();

                        goto End;
                    }

                    if (DialogWindowProvider.TryShowDialog("Do you want to continue with calibration?", out var dialogResultEnum, DialogButtonsEnum.YesNo, DialogIconEnum.Question) != true || dialogResultEnum != DialogResultEnum.Yes)
                    {
                        UpdateWelcomeStatus();

                        goto End;
                    }

                    CalibrationStepIndex = 0;
                }
                else
                    CalibrationStepIndex++;

                UpdatePreviousNextStatus();

                if (CalibrationStepIndex == 0)
                {
                    Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingClearHtml());
                    HtmlLogUniqueId = Guid.NewGuid();

                    Logger.LogHtmlInformation($"1. {Name}", HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
                }

                End:
                Logger.LogInformation("{@Name}: Next!", Name);
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Next Exception", Name);
            UpdateFailedStatus();
        }
    }

    #endregion 公开

    #region 重载

    protected virtual Task<bool> LoadedingAsync(CancellationToken cancellationToken) => Task.FromResult(true);

    protected virtual Task<bool> CalibratingAsync(CancellationToken cancellationToken) => Task.FromResult(true);

    protected virtual Task<bool> ReviewingAsync(CancellationToken cancellationToken) => Task.FromResult(true);

    protected virtual Task<bool> CancelingAsync() => Task.Run(() =>
    {
        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

        return true;
    });

    protected virtual Task<bool> PreviousingAsync(CancellationToken cancellationToken) => Task.FromResult(true);

    protected virtual Task<bool> NextingAsync(CancellationToken cancellationToken) => Task.FromResult(true);

    #endregion 重载

    #region 校准

    protected Task<bool> InvokeCalibrateAsync(Func<bool> func, string comment = "") => InvokeCalibrateAsync(async () => await Task.FromResult(func.Invoke()).ConfigureAwait(false), comment);

    protected Task<bool> InvokeVerifyAsync(Func<bool> func) => InvokeVerifyAsync(async () => await Task.FromResult(func.Invoke()).ConfigureAwait(false));

    protected async Task<bool> InvokeCalibrateAsync(Func<Task<bool>> func, string comment = "")
    {
        Logger.LogHtmlInformation($"{CalibrationStepIndex + 1}. {CalibrationSteps[CalibrationStepIndex].StepName}{(string.IsNullOrWhiteSpace(comment) ? string.Empty : $"[{comment}]")}", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());

        var result = false;
        try
        {
            CheckStatus();
            await Task.Run(async () =>
            {
                UpdateDisableAll();

                result = CalibrationSteps[CalibrationStepIndex].StepIsNextEnable = await func.Invoke().ConfigureAwait(false);
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            result = CalibrationSteps[CalibrationStepIndex].StepIsNextEnable = false;

            if (ex is OperationCanceledException)
            {
                DialogWindowProvider.ShowDialog($"Calibrate {Name} Canceled!");
                Logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                return result;
            }

            Logger.LogHtmlCritical(ex, "Calibrate Critical", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            return result;
        }
        finally
        {
            UpdatePreviousNextStatus();
            if (CalibrationStepIndex == CalibrationSteps.Count - 1 || result == false)
            {
                Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingPeekHtml("Calibrate" +
                                                                          $"_{FileHelper.RemoveInvalidFileName(ApplicationCookie.DeviceCode)}" +
                                                                          $"_{FileHelper.RemoveInvalidFileName(Name)}" +
                                                                          $"_{FileHelper.RemoveInvalidFileName(CalibrateHtmlLogFileName)}" +
                                                                          $"_{(result ? "OK" : "Failed")}"));
            }
        }
    }

    protected async Task<bool> InvokeVerifyAsync(Func<Task<bool>> func)
    {
        HtmlLogUniqueId = Guid.NewGuid();
        Logger.LogHtmlInformation($"1. {Name}", HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
        Logger.LogHtmlInformation("Verify", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());

        var result = false;
        try
        {
            CheckStatus();
            await Task.Run(async () =>
            {
                UpdateDisableAll();

                result = await func.Invoke().ConfigureAwait(false);
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            result = false;
            if (ex is OperationCanceledException)
            {
                DialogWindowProvider.ShowDialog($"Calibrate {Name} Canceled!");
                Logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                return result;
            }

            Logger.LogHtmlCritical(ex, "Verify Critical", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            return result;
        }
        finally
        {
            UpdateReviewStatus();
            Logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml("Verify" +
                                                                    $"_{FileHelper.RemoveInvalidFileName(ApplicationCookie.DeviceCode)}" +
                                                                    $"_{FileHelper.RemoveInvalidFileName(Name)}" +
                                                                    $"_{FileHelper.RemoveInvalidFileName(VerifyHtmlFileLogName)}" +
                                                                    $"_{(result ? "OK" : "Failed")}"));
        }
    }

    protected bool InvokeSave(Action<Action<ICacheItem>> action)
    {
        while (true)
        {
            try
            {
                action(UpdateIsInsert);

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{@Name}: Save Exception", Name);

                DialogWindowProvider.TryShowDialog("Save Failed!", out var dialogResultEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                if (dialogResultEnum == DialogResultEnum.Retry) continue;

                return false;
            }
        }

        void UpdateIsInsert(ICacheItem cacheItem)
        {
            cacheItem.Id = 0;
            cacheItem.CreatedTime = DateTime.Now;
            cacheItem.IsDeleted = false;

            if (cacheItem is not IEntityAdd entity) return;

            entity.CreatedUserId = ApplicationCookie.SysUser.Id;
            entity.CreatedUserName = ApplicationCookie.SysUser.UserName;
        }
    }

    #endregion 校准
}