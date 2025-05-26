using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.XYAstigmatism;

[IOCAppService(ServiceType = typeof(LaserXYAstigmatismCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserXYAstigmatismCalibrationUserControl
{
    [Permission]
    public LaserXYAstigmatismCalibrationUserControl()
    {
        InitializeComponent();
    }
}