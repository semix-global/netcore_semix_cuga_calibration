using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Chuck.StageMap;

[Permission]
[IOCAppService(ServiceType = typeof(ChuckStageMapUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckStageMapUserControl
{
    public ChuckStageMapUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}