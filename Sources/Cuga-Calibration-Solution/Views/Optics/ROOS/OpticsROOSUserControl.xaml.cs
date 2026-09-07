using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Optics.ROOS;

[Permission]
[IOCAppService(ServiceType = typeof(OpticsROOSUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsROOSUserControl
{
    public OpticsROOSUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}