using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.CIB.LightMatching;

[PermissionControl]
[IOCAppService(ServiceType = typeof(CIBLightMatchingUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBLightMatchingUserControl
{
    public CIBLightMatchingUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}