using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Chuck.AlignmentDegreeOffset;

[PermissionControl]
[IOCAppService(ServiceType = typeof(ChuckAlignmentDegreeOffsetCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckAlignmentDegreeOffsetCalibrationUserControl
{
    public ChuckAlignmentDegreeOffsetCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}