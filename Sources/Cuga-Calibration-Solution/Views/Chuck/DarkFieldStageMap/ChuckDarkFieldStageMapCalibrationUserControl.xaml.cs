using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Chuck.DarkFieldStageMap;

[IOCAppService(ServiceType = typeof(ChuckDarkFieldStageMapCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckDarkFieldStageMapCalibrationUserControl
{
    public ChuckDarkFieldStageMapCalibrationUserControl()
    {
        InitializeComponent();
    }
}