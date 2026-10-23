using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Fourier.CenterChannelSpecularBlocker;

[Permission]
[IOCAppService(ServiceType = typeof(FourierCenterChannelSpecularBlockerUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class FourierCenterChannelSpecularBlockerUserControl
{
    public FourierCenterChannelSpecularBlockerUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}