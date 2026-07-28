using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Optics.INC;

[Permission]
[IOCAppService(ServiceType = typeof(OpticsINCUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsINCUserControl
{
    public OpticsINCUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}