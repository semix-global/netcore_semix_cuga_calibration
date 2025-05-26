using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Setting;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;

[IOCAppService(ServiceType = typeof(SettingCommonViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class SettingCommonViewModel : SettingWindowViewModelBase
{
    [ObservableProperty]
    private SettingCommonParam _settingCommonParam = new();
}