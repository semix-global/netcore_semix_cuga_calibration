using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.FocusShift;

[IOCAppService(ServiceType = typeof(LaserFocusShiftCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserFocusShiftCalibrationUserControl
{
    [Permission]
    public LaserFocusShiftCalibrationUserControl()
    {
        InitializeComponent();
    }
}