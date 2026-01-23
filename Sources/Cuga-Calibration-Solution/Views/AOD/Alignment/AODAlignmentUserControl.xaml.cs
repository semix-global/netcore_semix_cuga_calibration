using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.AOD.Alignment;

[PermissionControl]
[IOCAppService(ServiceType = typeof(AODAlignmentUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AODAlignmentUserControl
{
    public AODAlignmentUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}