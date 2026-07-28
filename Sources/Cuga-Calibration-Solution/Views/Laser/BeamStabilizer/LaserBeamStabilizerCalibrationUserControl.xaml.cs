using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.BeamStabilizer;

[Permission]
[IOCAppService(ServiceType = typeof(LaserBeamStabilizerCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserBeamStabilizerCalibrationUserControl
{
    public LaserBeamStabilizerCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}