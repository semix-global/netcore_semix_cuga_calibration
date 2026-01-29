using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.AutoFocus.GlobalFocusOffset;

[IOCAppService(ServiceType = typeof(GlobalFocusOffsetUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class GlobalFocusOffsetUserControl
{
    public GlobalFocusOffsetUserControl()
    {
        InitializeComponent();
    }
}