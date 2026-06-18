using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.File.Setting;

[IOCAppService(ServiceType = typeof(SettingWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class SettingWindow
{
    public SettingWindow()
    {
        InitializeComponent();
    }
}