using Core.Utilities.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.AOD.BestFocusAndAstigmatism;

[Permission]
[IOCAppService(ServiceType = typeof(BestFocusAndAstigmatismCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class BestFocusAndAstigmatismCalibrationUserControl
{
    public BestFocusAndAstigmatismCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}