using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Optics.GlobalFieldTilt;

[Permission]
[IOCAppService(ServiceType = typeof(OpticsGlobalFieldTiltUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class OpticsGlobalFieldTiltUserControl
{
    public OpticsGlobalFieldTiltUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}