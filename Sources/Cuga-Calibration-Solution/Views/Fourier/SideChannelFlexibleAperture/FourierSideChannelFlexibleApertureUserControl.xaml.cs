using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Fourier.SideChannelFlexibleAperture;

[Permission]
[IOCAppService(ServiceType = typeof(FourierSideChannelFlexibleApertureUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class FourierSideChannelFlexibleApertureUserControl
{
    public FourierSideChannelFlexibleApertureUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}
