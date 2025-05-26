using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.LineCentricity;

[IOCAppService(ServiceType = typeof(LaserLineCentricityCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserLineCentricityCalibrationUserControl
{
    [Permission]
    public LaserLineCentricityCalibrationUserControl()
    {
        InitializeComponent();
    }
}