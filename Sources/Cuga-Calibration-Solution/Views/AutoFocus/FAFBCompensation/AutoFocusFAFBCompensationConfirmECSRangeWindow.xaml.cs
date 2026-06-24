using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.AutoFocus.FAFBCompensation;

[IOCAppService(ServiceType = typeof(AutoFocusFAFBCompensationConfirmECSRangeWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class AutoFocusFAFBCompensationConfirmECSRangeWindow
{
    public AutoFocusFAFBCompensationConfirmECSRangeWindow()
    {
        InitializeComponent();
    }
}