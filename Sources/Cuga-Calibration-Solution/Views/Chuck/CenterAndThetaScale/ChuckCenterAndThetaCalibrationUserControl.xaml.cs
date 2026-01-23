using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Chuck.CenterAndThetaScale;

[PermissionControl]
[IOCAppService(ServiceType = typeof(ChuckCenterAndThetaCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckCenterAndThetaCalibrationUserControl
{
    public ChuckCenterAndThetaCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}