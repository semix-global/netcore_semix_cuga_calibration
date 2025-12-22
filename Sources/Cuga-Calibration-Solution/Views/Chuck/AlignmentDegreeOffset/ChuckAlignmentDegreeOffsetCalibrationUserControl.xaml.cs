using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Chuck.AlignmentDegreeOffset;

[IOCAppService(ServiceType = typeof(ChuckAlignmentDegreeOffsetCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckAlignmentDegreeOffsetCalibrationUserControl
{
    [Permission]
    public ChuckAlignmentDegreeOffsetCalibrationUserControl()
    {
        InitializeComponent();
    }
}