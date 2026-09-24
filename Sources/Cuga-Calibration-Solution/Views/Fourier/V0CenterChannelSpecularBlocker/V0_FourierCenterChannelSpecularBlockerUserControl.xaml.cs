using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Fourier.V0CenterChannelSpecularBlocker;

[Permission]
[IOCAppService(ServiceType = typeof(V0FourierCenterChannelSpecularBlockerUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class V0FourierCenterChannelSpecularBlockerUserControl
{
    public V0FourierCenterChannelSpecularBlockerUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}