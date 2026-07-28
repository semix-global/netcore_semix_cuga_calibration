using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.AOD.BestFocusAndAstigmatism;

[Permission]
[IOCAppService(ServiceType = typeof(AODBestFocusAndAstigmatismUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AODBestFocusAndAstigmatismUserControl
{
    public AODBestFocusAndAstigmatismUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}