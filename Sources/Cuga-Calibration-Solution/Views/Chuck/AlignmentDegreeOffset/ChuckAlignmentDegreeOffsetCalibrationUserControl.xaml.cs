using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Chuck.AlignmentDegreeOffset;

[Permission]
[IOCAppService(ServiceType = typeof(ChuckAlignmentDegreeOffsetCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckAlignmentDegreeOffsetCalibrationUserControl
{
    public ChuckAlignmentDegreeOffsetCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}