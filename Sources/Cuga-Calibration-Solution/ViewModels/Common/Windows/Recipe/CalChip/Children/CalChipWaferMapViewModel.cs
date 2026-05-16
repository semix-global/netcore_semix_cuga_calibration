using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Events;
using Core.Models.Models.Setting;
using Core.Recipe.Models;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Recipe.Edit.Children;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.IOC.Providers;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Recipe.CalChip.Children;

/// <summary>
/// CalChip WaferMap 绘制 / Die 选择 / 坐标计算
/// </summary>
[IOCAppService(ServiceType = typeof(CalChipWaferMapViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CalChipWaferMapViewModel : ViewModelBase, IRecipient<ValueChangedMessage<ToggleCalChipRecipeEvent>>
{
    private readonly ISynchronizationContextProvider _contextProvider;
    private readonly IMessenger _messenger;
    private bool _isLoaded;

    #region 属性

    [ObservableProperty]
    public required partial RecipeWaferSettingUserControlViewModel RecipeWaferSettingUserControlViewModel { get; set; }

    private CalChipRecipeDTO? _editDTO;

    /// <summary>
    /// 当前编辑中的 CalChip 配方项
    /// </summary>
    public CalChipRecipeDTOItem? EditingItem { get; private set; }

    /// <summary>
    /// CalChip WaferMap 绘制 / Die 选择 / 坐标计算
    /// </summary>
    public CalChipWaferMapViewModel(
        ISynchronizationContextProvider contextProvider,
        IMessenger messenger)
    {
        _contextProvider = contextProvider;
        _messenger = messenger;

        messenger.UnregisterAll(this);
        messenger.RegisterAll(this);
    }

    #endregion


    public void Initialize(CalChipRecipeDTO calChipRecipeDTO)
    {
        _editDTO = calChipRecipeDTO;
        EditingItem = _editDTO.CurrentItem;

        if (_isLoaded == false)
        {
            RecipeWaferSettingUserControlViewModel = new RecipeWaferSettingUserControlViewModel(
                HostApplication.GetRequiredService<ILogger<RecipeWaferSettingUserControlViewModel>>(),
                HostApplication.GetRequiredService<IDialogWindowProvider>(),
                HostApplication.GetRequiredService<ICalibrationRecipeService>(),
                _contextProvider,
                _messenger,
                HostApplication.GetRequiredService<StageViewModel>(),
                HostApplication.GetRequiredService<MicroscopeViewModel>(),
                HostApplication.GetRequiredService<CalibrationSetting>());
        }

        RecipeWaferSettingUserControlViewModel.WaferDTO = EditingItem.CalChipMapDTO;
        RecipeWaferSettingUserControlViewModel.CalChipSiteModelEnum = EditingItem.CalChipSiteModelEnum;
        NotifyAll();

        _isLoaded = true;
    }


    #region 状态刷新

    public void NotifyAll()
    {
        _contextProvider.Post(() =>
        {
            // OnPropertyChanged(nameof(EditingItem));
            // OnPropertyChanged(nameof(RecipeWaferSettingUserControlViewModel));
            _messenger.Send(ToggleWaferMapEventFactory.UpdateIsRefreshWaferMap(true));
        });
    }

    public void Receive(ValueChangedMessage<ToggleCalChipRecipeEvent> message)
    {
        _contextProvider.Post(() =>
        {
            if (message.Value.IsCalChipRecipeAlignment.HasValue)
                RecipeWaferSettingUserControlViewModel.IsAlignmentOk = message.Value.IsCalChipRecipeAlignment.Value;
        });
    }

    #endregion

    public void Dispose()
    {
        RecipeWaferSettingUserControlViewModel.CancelToken();
        _isLoaded = false;
    }
}