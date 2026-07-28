using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Optics.SC;

[Permission]
[IOCAppService(ServiceType = typeof(OpticsSCUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsSCUserControl
{
    public OpticsSCUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}