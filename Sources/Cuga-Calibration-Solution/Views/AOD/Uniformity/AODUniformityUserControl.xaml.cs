using Core.Utilities.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.AOD.Uniformity;

[Permission]
[IOCAppService(ServiceType = typeof(AODUniformityUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AODUniformityUserControl
{
    public AODUniformityUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}