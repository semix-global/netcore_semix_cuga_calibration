using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

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