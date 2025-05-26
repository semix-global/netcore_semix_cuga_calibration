using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(AlignmentParamWindowBrightField), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class AlignmentParamWindowBrightField
{
    public AlignmentParamWindowBrightField()
    {
        InitializeComponent();
    }
}