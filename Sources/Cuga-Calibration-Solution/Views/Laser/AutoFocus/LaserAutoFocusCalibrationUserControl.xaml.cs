using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.AutoFocus;

[IOCAppService(ServiceType = typeof(LaserAutoFocusCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserAutoFocusCalibrationUserControl
{
    [Permission]
    public LaserAutoFocusCalibrationUserControl()
    {
        InitializeComponent();
    }
}