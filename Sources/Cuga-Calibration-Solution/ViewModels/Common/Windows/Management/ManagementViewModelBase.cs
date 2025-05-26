using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Net.Utilities.Helper.IOC.Providers;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Management;

public partial class ManagementViewModelBase(
    ILogger<ManagementViewModelBase> logger,
    IDialogWindowProvider dialogWindowProvider,
    ISynchronizationContextProvider synchronizationContextProvider
) : ViewModelBase, IDisposable
{
    protected readonly ILogger<ManagementViewModelBase> Logger = logger;
    protected readonly IDialogWindowProvider DialogWindowProvider = dialogWindowProvider;
    protected readonly ISynchronizationContextProvider SynchronizationContextProvider = synchronizationContextProvider;
    protected CancellationTokenSource? CancellationTokenSource;

    /// <summary>
    /// UI显示的顺序
    /// </summary>
    public virtual int DisplayOrderNum => 0;

    /// <summary>
    /// 显示文本
    /// </summary>
    public virtual string DisplayName => "NaA";

    /// <summary>
    /// 是否选中数据
    /// </summary>
    public virtual bool IsSelectedItem => false;

    /// <summary>
    /// 操作指令（0：无操作，1：edit ,2：add ）
    /// </summary>
    [ObservableProperty]
    private int _operateCommandIndex;

    /// <summary>
    /// 窗口使能（true可用，false禁用-有编辑页面展开时）
    /// </summary>
    [ObservableProperty]
    private bool _isEnableWindow;

    /// <summary>
    /// 插入、更新数据页面显示状态
    /// </summary>
    public virtual bool IsEnableEdit => true;

    [RelayCommand]
    private void Loaded() => _ = LoadedAsync();

    /// <summary>
    /// 加载
    /// </summary>
    public async Task LoadedAsync()
    {
        try
        {
            await Task.Run(async () =>
            {
                CancelToken();
                UpdateEnableStatus(true);
                CancellationTokenSource = new CancellationTokenSource();
                if (await LoadedingAsync().ConfigureAwait(false) == false)
                {
                    Logger.LogWarning("{@Name}: Init Failed", DisplayName);
                    return;
                }

                VariableInitialization();
                Logger.LogInformation("{@Name}: Loading Ok!", DisplayName);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Loading Failed", DisplayName);
        }
    }

    public async Task DeleteAsync()
    {
        try
        {
            if (!IsSelectedItem)
            {
                DialogWindowProvider.ShowDialog("Select data line is empty!", DialogButtonsEnum.OKCancel, DialogIconEnum.Warning);
                return;
            }

            DialogWindowProvider.TryShowDialog("Are you sure to delete this data?"
                , out var dialogButtonsEnum, DialogButtonsEnum.YesNo, DialogIconEnum.Warning);
            if (dialogButtonsEnum == DialogResultEnum.No)
                return;
            UpdateEnableStatus(true);
            await Task.Run(async () =>
            {
                if (await DeletingingAsync().ConfigureAwait(false) == false)
                    CancelToken();
            }).ConfigureAwait(false);
            await SelectAllAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Cancel Failed", DisplayName);
        }
    }

    public async Task SelectAsync()
    {
        try
        {
            UpdateEnableStatus(true);
            await Task.Run(async () =>
            {
                if (await SelectingAsync().ConfigureAwait(false) == false)
                    CancelToken();
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Cancel Failed", DisplayName);
        }
    }

    public async Task SelectAllAsync()
    {
        try
        {
            UpdateEnableStatus(true);
            await Task.Run(async () =>
            {
                if (await SelectingAllAsync().ConfigureAwait(false) == false)
                    CancelToken();
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Cancel Failed", DisplayName);
        }
    }

    public async Task ApplyAsync()
    {
        try
        {
            DialogWindowProvider.TryShowDialog("Are you sure about the application setting?"
                , out var dialogButtonsEnum, DialogButtonsEnum.YesNo, DialogIconEnum.Warning);
            if (dialogButtonsEnum == DialogResultEnum.No)
                return;
            await Task.Run(async () =>
            {
                if (await ApplyingAsync().ConfigureAwait(false) == false)
                    CancelToken();
            }).ConfigureAwait(false);
            UpdateEnableStatus(true);
            VariableInitialization();
            await SelectAllAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DialogWindowProvider.ShowDialog("Insert or update failed! Please check if there is a primary key conflict!", DialogButtonsEnum.OKCancel, DialogIconEnum.Warning);
            Logger.LogError(ex, "{@Name}: Cancel Failed", DisplayName);
        }
    }

    public void Insert()
    {
        try
        {
            UpdateEnableStatus(false, 2);
            VariableInitialization();
            _ = RefreshAssociationTableAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Cancel Failed", DisplayName);
        }
    }

    public void Update()
    {
        try
        {
            if (!IsSelectedItem)
            {
                DialogWindowProvider.ShowDialog("Select data line is empty!", DialogButtonsEnum.OKCancel, DialogIconEnum.Warning);
                return;
            }

            UpdateEnableStatus(false, 1);
            VariableInitialization();
            _ = RefreshAssociationTableAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Cancel Failed", DisplayName);
        }
    }

    public void Reset() => VariableInitialization();

    /// <summary>
    /// 编辑时界面禁用状态
    /// </summary>
    /// <param name="isEnbale">true启用，false禁用</param>
    /// <param name="commandIndex">命令索引</param>
    protected void UpdateEnableStatus(bool isEnbale, int commandIndex = 0)
    {
        IsEnableWindow = isEnbale;
        OperateCommandIndex = commandIndex;
    }

    private void CancelToken()
    {
        CancellationTokenSource?.Cancel();
        CancellationTokenSource?.Dispose();
        CancellationTokenSource = null;
    }

    #region 重载

    protected virtual Task<bool> LoadedingAsync() => Task.FromResult(true);

    protected virtual Task<bool> InsertingAsync() => Task.FromResult(true);

    protected virtual Task<bool> DeletingingAsync() => Task.FromResult(true);

    protected virtual Task<bool> UpdatingAsync() => Task.FromResult(true);

    protected virtual Task<bool> SelectingAsync() => Task.FromResult(true);

    protected virtual Task<bool> SelectingAllAsync() => Task.FromResult(true);

    protected virtual Task<bool> ApplyingAsync() => Task.FromResult(true);

    protected virtual void VariableInitialization()
    {
    }

    protected virtual Task<bool> RefreshAssociationTableAsync() => Task.FromResult(true);

    #endregion 重载

    public virtual void Dispose()
    {
        CancelToken();

        GC.SuppressFinalize(this);
    }
}