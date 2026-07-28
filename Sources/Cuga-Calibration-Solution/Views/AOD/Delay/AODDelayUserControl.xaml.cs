using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.AOD.Delay;

[Permission]
[IOCAppService(ServiceType = typeof(AODDelayUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AODDelayUserControl
{
    public AODDelayUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}