using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Chuck.StageMap;

[IOCAppService(ServiceType = typeof(ChuckStageMapCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckStageMapCalibrationUserControl
{
    public ChuckStageMapCalibrationUserControl()
    {
        InitializeComponent();
    }
}