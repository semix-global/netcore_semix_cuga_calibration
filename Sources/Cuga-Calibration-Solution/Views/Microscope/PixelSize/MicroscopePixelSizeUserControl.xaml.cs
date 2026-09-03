using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Microscope.PixelSize;

[Permission]
[IOCAppService(ServiceType = typeof(MicroscopePixelSizeUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopePixelSizeUserControl
{
    public MicroscopePixelSizeUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}