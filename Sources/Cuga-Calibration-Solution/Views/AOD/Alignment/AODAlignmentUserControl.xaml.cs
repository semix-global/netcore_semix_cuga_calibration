using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.AOD.Alignment;

[Permission]
[IOCAppService(ServiceType = typeof(AODAlignmentUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AODAlignmentUserControl
{
    public AODAlignmentUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}