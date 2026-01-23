using Core.Utilities.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.CIB.MMD;

[Permission]
[IOCAppService(ServiceType = typeof(CIBMMDUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBMMDUserControl
{
    public CIBMMDUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}