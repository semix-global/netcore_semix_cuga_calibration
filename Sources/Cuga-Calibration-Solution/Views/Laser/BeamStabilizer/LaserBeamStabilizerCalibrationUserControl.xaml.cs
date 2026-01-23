using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.BeamStabilizer;

[PermissionControl]
[IOCAppService(ServiceType = typeof(LaserBeamStabilizerCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserBeamStabilizerCalibrationUserControl
{
    public LaserBeamStabilizerCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}