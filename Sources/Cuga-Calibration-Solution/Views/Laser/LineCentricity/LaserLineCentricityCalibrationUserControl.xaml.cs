using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.LineCentricity;

[PermissionControl]
[IOCAppService(ServiceType = typeof(LaserLineCentricityCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserLineCentricityCalibrationUserControl
{
    public LaserLineCentricityCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}