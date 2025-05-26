using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.AodDelay;

[IOCAppService(ServiceType = typeof(LaserAodDelayCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserAodDelayCalibrationUserControl
{
    [Permission]
    public LaserAodDelayCalibrationUserControl()
    {
        InitializeComponent();
    }
}