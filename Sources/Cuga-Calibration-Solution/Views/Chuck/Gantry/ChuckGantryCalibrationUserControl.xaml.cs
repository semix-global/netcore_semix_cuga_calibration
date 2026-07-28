using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Chuck.Gantry;

[Permission]
[IOCAppService(ServiceType = typeof(ChuckGantryCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckGantryCalibrationUserControl
{
    public ChuckGantryCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}