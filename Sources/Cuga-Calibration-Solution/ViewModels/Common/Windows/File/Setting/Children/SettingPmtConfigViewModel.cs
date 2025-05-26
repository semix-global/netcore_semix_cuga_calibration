using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Setting;
using MoreLinq;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;

[IOCAppService(ServiceType = typeof(SettingPmtConfigViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class SettingPmtConfigViewModel : SettingWindowViewModelBase
{
    [ObservableProperty]
    private SettingPmtConfigParam _settingPmtConfigParam = new();

    [RelayCommand]
    private void ChangeAllSelection(object isSelectAll)
    {
        try
        {
            var isEnabled = Convert.ToBoolean(isSelectAll);
            SettingPmtConfigParam.PmtConfigList.ForEach(t => t.Enabled = isEnabled);
        }
        catch
        {
            throw new ArgumentException("Command Parameter Convert to Boolean Invalid!");
        }
    }
}