using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.AutoFocus;

[PermissionControl]
[IOCAppService(ServiceType = typeof(LaserAutoFocusCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserAutoFocusCalibrationUserControl
{
    public LaserAutoFocusCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}