using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Fourier.V0CenterChannelFlexibleAperture;

[Permission]
[IOCAppService(ServiceType = typeof(V0FourierCenterChannelFlexibleApertureUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class V0FourierCenterChannelFlexibleApertureUserControl
{
    public V0FourierCenterChannelFlexibleApertureUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}