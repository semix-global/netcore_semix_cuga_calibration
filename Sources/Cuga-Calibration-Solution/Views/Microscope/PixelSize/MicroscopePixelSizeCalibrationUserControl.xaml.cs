using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Microscope.PixelSize;

[Permission]
[IOCAppService(ServiceType = typeof(MicroscopePixelSizeCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopePixelSizeCalibrationUserControl
{
    public MicroscopePixelSizeCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}