using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Chuck.GlobalScaleError;

[Permission]
[IOCAppService(ServiceType = typeof(ChuckGlobalScaleErrorCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class ChuckGlobalScaleErrorCalibrationUserControl
{
    public ChuckGlobalScaleErrorCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}