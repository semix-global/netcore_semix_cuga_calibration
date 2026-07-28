using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Microscope.Focus;

[Permission]
[IOCAppService(ServiceType = typeof(MicroscopeFocusCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopeFocusCalibrationUserControl
{
    public MicroscopeFocusCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}