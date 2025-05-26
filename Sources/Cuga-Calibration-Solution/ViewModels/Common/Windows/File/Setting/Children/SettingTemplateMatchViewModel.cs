using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Setting;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;

[IOCAppService(ServiceType = typeof(SettingTemplateMatchViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class SettingTemplateMatchViewModel : SettingWindowViewModelBase
{
    [ObservableProperty]
    private SettingTemplateMatchParam _settingTemplateMatchParam = new();
}