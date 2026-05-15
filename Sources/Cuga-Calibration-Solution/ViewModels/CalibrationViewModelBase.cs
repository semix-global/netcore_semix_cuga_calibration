using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Enums;
using Core.Models.Models;
using Core.Models.Models.Common.Pattern;
using Core.Utilities;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Local.SQL.DB.Providers.Models.Entities.Base.Interface;
using Microsoft.Extensions.Logging;
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
            RefreshToken();
            await Task.Run(async () =>
            {
                ViewEnum = CalibrationItemViewEnum.Loading;
                if (await LoadedingAsync(_cancellationTokenSource.Token).ConfigureAwait(false) == false)
                {
                    UpdateFailedStatus();
                    Logger.LogWarning("{@Name}: Loading Failed", Name);
                    return;
                }

                UpdateWelcomeStatus();
                Logger.LogInformation("{@Name}: Loading Ok!", Name);
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Loading Exception", Name);
            UpdateFailedStatus();
        }
    }

    [RelayCommand]
    public async Task CalibrateAsync()
    {
        try
        {
            CheckStatus();
            await Task.Run(async () =>
            {
                ViewEnum = CalibrationItemViewEnum.Loading;

                if (await CalibratingAsync(_cancellationTokenSource.Token).ConfigureAwait(false) == false)
                {
                    ViewEnum = CalibrationItemViewEnum.Welcome;
                    Logger.LogWarning("{@Name}: Calibrate Failed", Name);
                    return;
                }

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
            CheckStatus();
            await Task.Run(async () =>
            {
                ViewEnum = CalibrationItemViewEnum.Loading;
                if (await ReviewingAsync(_cancellationTokenSource.Token).ConfigureAwait(false) == false)
                {
                    ViewEnum = CalibrationItemViewEnum.Welcome;

                    DialogWindowProvider.ShowDialog("Please calibrate!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

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
            UpdateDisableAll();

            await Task.Run(async () =>
            {
                CancelToken();

                if (ViewEnum == CalibrationItemViewEnum.Calibration) Logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{ApplicationCookie.DeviceCode}_{CalibrateHtmlLogFileName}_Step1-Step{CalibrationStepIndex + 1}_Failed"));

                ViewEnum = CalibrationItemViewEnum.Loading;
                if (await CancelingAsync().ConfigureAwait(false) == false)
                {
                    Logger.LogError("{@Name}: Cancel Failed", Name);
                    UpdateFailedStatus();
                    return;
                }

                UpdateCancelStatus();
                Logger.LogInformation("{@Name}: Cancel!", Name);
            }).ConfigureAwait(false);
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
            CheckStatus();

            await Task.Run(async () =>
            {
                CalibrationStepList[CalibrationStepIndex].StepIsNextEnable = CalibrationStepList[CalibrationStepIndex].DefaultIsNextEnable; // 恢复默认值

                ViewEnum = CalibrationItemViewEnum.Loading;
                if (await PreviousingAsync(_cancellationTokenSource.Token).ConfigureAwait(false) == false)
                {
                    UpdatePreviousNextStatus();
                    Logger.LogWarning("{@Name}: Previous Failed", Name);
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
            CheckStatus();
            await Task.Run(async () =>
            {
                CalibrationStepList[CalibrationStepIndex].StepIsNextEnable = CalibrationStepList[CalibrationStepIndex].DefaultIsNextEnable; // 恢复默认值

                ViewEnum = CalibrationItemViewEnum.Loading;
                if (await NextingAsync(_cancellationTokenSource.Token).ConfigureAwait(false) == false)
                {
                    UpdatePreviousNextStatus();
                    Logger.LogWarning("{@Name}: Next Failed", Name);
                    return;
                }

                CalibrationStepIndex++;
                UpdatePreviousNextStatus();

                if (CalibrationStepIndex == 0)
                {
                    Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingClearHtml());
                    HtmlLogUniqueId = Guid.NewGuid();
                    Logger.LogHtmlInformation($"1. {Name}", HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
                }

                Logger.LogInformation("{@Name}: Next!", Name);
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Next Exception", Name);
            UpdateFailedStatus();
        }
    }

    [RelayCommand]
    private void MagnificationSelected(object obj)
    {
        if (obj is MicroscopeLensInformation lensInformation && ApplicationCookie.MicroscopeLensInformations.Contains(lensInformation) == false)
        {
            Logger.LogError("{@Name}: Select magnification is illegal!", Name);
        }
    }

    #region Event

    public void Receive(PropertyChangedMessage<bool> message)
    {
        if (message is not { Sender: CalibrationItemStep, PropertyName: nameof(CalibrationItemStep.StepIsNextEnable) }) return;

        UpdateNextStatus();
        OnPropertyChanged(nameof(CalibrationProgress));
    }

    #endregion Event

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

    protected Task<bool> InvokeCalibrateAsync(Func<Task<bool>> func, string comment = "") => InvokeCalibrateAsync(() => func.Invoke().GetAwaiter().GetResult(), comment);

    protected Task<bool> InvokeVerifyAsync(Func<Task<bool>> func) => InvokeVerifyAsync(() => func.Invoke().GetAwaiter().GetResult());

    protected async Task<bool> InvokeCalibrateAsync(Func<bool> func, string comment = "")
    {
        Logger.LogHtmlInformation($"{CalibrationStepIndex + 1}. {CalibrationStepList[CalibrationStepIndex].StepName}{(string.IsNullOrWhiteSpace(comment) ? string.Empty : $"[{comment}]")}", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());

        var calibrateName = Name.Trim().Replace(" ", "");
        var result = false;
        try
        {
            CheckStatus();
            await Task.Run(() =>
            {
                UpdateDisableAll();

                result = CalibrationStepList[CalibrationStepIndex].StepIsNextEnable = func.Invoke();
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            result = CalibrationStepList[CalibrationStepIndex].StepIsNextEnable = false;

            if (ex is OperationCanceledException)
            {
                DialogWindowProvider.ShowDialog($"Calibrate {calibrateName} Canceled!");
                Logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                return result;
            }

            Logger.LogHtmlCritical(ex, "Critical", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            return result;
        }
        finally
        {
            UpdatePreviousNextStatus();
            if (CalibrationStepIndex == CalibrationStepList.Count - 1 || !result)
                Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingPeekHtml($"{nameof(CalibrationTypeEnum.HandleCalibration)}_{ApplicationCookie.DeviceCode}_{calibrateName}_{CalibrateHtmlLogFileName}_{(result ? "OK" : "Failed")}"));
        }
    }

    protected async Task<bool> InvokeVerifyAsync(Func<bool> func)
    {
        HtmlLogUniqueId = Guid.NewGuid();
        var calibrateName = Name.Trim().Replace(" ", "");
        Logger.LogHtmlInformation($"1. {calibrateName}", HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
        Logger.LogHtmlInformation("Verify", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
        var result = false;
        try
        {
            CheckStatus();

            await Task.Run(() =>
            {
                UpdateDisableAll();

                result = func.Invoke();
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            result = false;
            if (ex is OperationCanceledException)
            {
                DialogWindowProvider.ShowDialog($"Calibrate {calibrateName} Canceled!");
                Logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                return result;
            }

            Logger.LogHtmlCritical(ex, "Critical", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            return result;
        }
        finally
        {
            UpdateReviewStatus();

            Logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{nameof(CalibrationTypeEnum.HandleVerify)}_{ApplicationCookie.DeviceCode}_{calibrateName}_{VerifyHtmlFileLogName}_{(result ? "OK" : "Failed")}"));
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