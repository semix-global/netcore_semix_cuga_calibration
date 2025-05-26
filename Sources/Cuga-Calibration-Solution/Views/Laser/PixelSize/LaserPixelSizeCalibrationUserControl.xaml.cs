using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.PixelSize;

[IOCAppService(ServiceType = typeof(LaserPixelSizeCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserPixelSizeCalibrationUserControl
{
    [Permission]
    public LaserPixelSizeCalibrationUserControl()
    {
        InitializeComponent();
    }
}