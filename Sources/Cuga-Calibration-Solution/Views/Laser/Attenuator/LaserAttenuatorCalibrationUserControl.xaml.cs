using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.Attenuator;

[IOCAppService(ServiceType = typeof(LaserAttenuatorCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserAttenuatorCalibrationUserControl
{
    [Permission]
    public LaserAttenuatorCalibrationUserControl()
    {
        InitializeComponent();
    }
}