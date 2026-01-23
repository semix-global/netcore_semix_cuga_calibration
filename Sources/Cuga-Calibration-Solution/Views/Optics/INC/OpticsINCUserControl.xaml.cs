using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Optics.INC;

[PermissionControl]
[IOCAppService(ServiceType = typeof(OpticsINCUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsINCUserControl
{
    public OpticsINCUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}