using Core.Utilities.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.CIB.XTC;

[Permission]
[IOCAppService(ServiceType = typeof(CIBXTCUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBXTCUserControl
{
    public CIBXTCUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}