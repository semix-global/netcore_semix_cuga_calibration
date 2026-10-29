using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Fourier.CenterChannelFlexibleAperture;

[Permission]
[IOCAppService(ServiceType = typeof(FourierCenterChannelFlexibleApertureUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class FourierCenterChannelFlexibleApertureUserControl
{
    public FourierCenterChannelFlexibleApertureUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}