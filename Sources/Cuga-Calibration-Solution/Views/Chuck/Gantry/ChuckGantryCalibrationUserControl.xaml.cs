using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Chuck.Gantry;

[PermissionControl]
[IOCAppService(ServiceType = typeof(ChuckGantryCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckGantryCalibrationUserControl
{
    public ChuckGantryCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}