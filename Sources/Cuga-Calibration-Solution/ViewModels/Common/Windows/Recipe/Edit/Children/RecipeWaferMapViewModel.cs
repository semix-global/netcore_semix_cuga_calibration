using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Events;
using Core.Models.Models.Setting;
using Core.Recipe.Models;
using CugaCalibration.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.IOC.Providers;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Recipe.Edit.Children;

/// <summary>
/// 职责：WaferMap 绘制 / Die 选择 / 坐标计算
/// </summary>
[IOCAppService(ServiceType = typeof(RecipeWaferMapViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class RecipeWaferMapViewModel : ViewModelBase, IRecipient<ValueChangedMessage<ToggleRecipeEvent>>
{
    #region 属性

    /// <summary>
    /// 当前编辑中的配方（由主 VM LoadedAsync 赋值）
    /// </summary>
    [ObservableProperty]
    public partial CalibrationRecipeDTO EditingDTO { get; set; } = new();

    [ObservableProperty]
    public required partial RecipeWaferSettingUserControlViewModel RecipeWaferSettingUserControlViewModel { get; set; }

    #region 界面

    private readonly ISynchronizationContextProvider _contextProvider;
    private readonly IMessenger _messenger;

    /// <summary>
    /// 职责：WaferMap 绘制 / Die 选择 / 坐标计算
    /// </summary>
    public RecipeWaferMapViewModel(ISynchronizationContextProvider contextProvider,
        IMessenger messenger)
    {
        _contextProvider = contextProvider;
        _messenger = messenger;

        _messenger.UnregisterAll(this);
        _messenger.RegisterAll(this);
    }

    #endregion

    #endregion

    public void Initialize(CalibrationRecipeDTO dto)
    {
        EditingDTO = dto;

        RecipeWaferSettingUserControlViewModel = new RecipeWaferSettingUserControlViewModel(
            HostApplication.GetRequiredService<ILogger<RecipeWaferSettingUserControlViewModel>>(),
            HostApplication.GetRequiredService<IDialogWindowProvider>(),
            HostApplication.GetRequiredService<ICalibrationRecipeService>(),
            _contextProvider,
            _messenger,
            HostApplication.GetRequiredService<StageViewModel>(),
            HostApplication.GetRequiredService<MicroscopeViewModel>(),
            HostApplication.GetRequiredService<CalibrationSetting>());

        RecipeWaferSettingUserControlViewModel.WaferDTO = EditingDTO.WaferDTO;

        _messenger.Send(ToggleWaferMapEventFactory.UpdateIsRefreshWaferMap(true));
    }


    #region 状态刷新

    public void Receive(ValueChangedMessage<ToggleRecipeEvent> message)
    {
        _contextProvider.Post(() =>
        {
            if (message.Value.IsRecipeAlignment.HasValue)
            {
                RecipeWaferSettingUserControlViewModel.IsAlignmentOk = message.Value.IsRecipeAlignment.Value;
            }
        });
    }

    #endregion

    public void Dispose()
    {
        RecipeWaferSettingUserControlViewModel.Dispose();
    }
}