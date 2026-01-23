using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.CIB.MMD;

[PermissionControl]
[IOCAppService(ServiceType = typeof(CIBMMDUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBMMDUserControl
{
    public CIBMMDUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}