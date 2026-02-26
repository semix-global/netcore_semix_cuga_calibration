using Core.Utilities.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.AutoFocus.CalChipFocusOffset;

[Permission]
[IOCAppService(ServiceType = typeof(AutoFocusCalChipFocusOffsetUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class AutoFocusCalChipFocusOffsetUserControl
{
    public AutoFocusCalChipFocusOffsetUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}