using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.Rtfc;

[IOCAppService(ServiceType = typeof(LaserRtfcCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserRtfcCalibrationUserControl
{
    [Permission]
    public LaserRtfcCalibrationUserControl()
    {
        InitializeComponent();
    }
}