using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.BeamStabilizer;

[IOCAppService(ServiceType = typeof(LaserBeamStabilizerCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserBeamStabilizerCalibrationUserControl
{
    [Permission]
    public LaserBeamStabilizerCalibrationUserControl()
    {
        InitializeComponent();
    }
}