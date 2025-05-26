using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.XPixelSize;

[IOCAppService(ServiceType = typeof(LaserXPixelSizeCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserXPixelSizeCalibrationUserControl
{
    [Permission]
    public LaserXPixelSizeCalibrationUserControl()
    {
        InitializeComponent();
    }
}