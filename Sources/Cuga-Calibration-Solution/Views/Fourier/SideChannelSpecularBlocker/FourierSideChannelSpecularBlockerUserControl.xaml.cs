using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Fourier.SideChannelSpecularBlocker;

[Permission]
[IOCAppService(ServiceType = typeof(FourierSideChannelSpecularBlockerUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class FourierSideChannelSpecularBlockerUserControl
{
    public FourierSideChannelSpecularBlockerUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}