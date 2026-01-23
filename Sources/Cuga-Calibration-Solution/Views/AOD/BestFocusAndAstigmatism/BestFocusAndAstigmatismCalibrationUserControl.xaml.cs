using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.AOD.BestFocusAndAstigmatism;

[PermissionControl]
[IOCAppService(ServiceType = typeof(BestFocusAndAstigmatismCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class BestFocusAndAstigmatismCalibrationUserControl
{
    public BestFocusAndAstigmatismCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}