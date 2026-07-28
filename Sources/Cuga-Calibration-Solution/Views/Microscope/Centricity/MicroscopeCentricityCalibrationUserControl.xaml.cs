using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Microscope.Centricity;

[Permission]
[IOCAppService(ServiceType = typeof(MicroscopeCentricityCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopeCentricityCalibrationUserControl
{
    public MicroscopeCentricityCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}