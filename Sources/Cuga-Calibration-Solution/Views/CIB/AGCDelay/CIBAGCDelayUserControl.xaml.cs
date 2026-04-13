using Core.Utilities.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.CIB.AGCDelay;

[Permission]
[IOCAppService(ServiceType = typeof(CIBAGCDelayUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBAGCDelayUserControl
{
    public CIBAGCDelayUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}