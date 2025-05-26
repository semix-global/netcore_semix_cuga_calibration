using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(AlignmentParamWindowDarkField), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class AlignmentParamWindowDarkField
{
    public AlignmentParamWindowDarkField()
    {
        InitializeComponent();
    }
}