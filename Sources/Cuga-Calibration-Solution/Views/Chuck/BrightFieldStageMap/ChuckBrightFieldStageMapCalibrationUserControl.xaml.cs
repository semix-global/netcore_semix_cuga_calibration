using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Chuck.BrightFieldStageMap;

[IOCAppService(ServiceType = typeof(ChuckBrightFieldStageMapCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckBrightFieldStageMapCalibrationUserControl
{
    [Permission]
    public ChuckBrightFieldStageMapCalibrationUserControl()
    {
        InitializeComponent();
    }
}