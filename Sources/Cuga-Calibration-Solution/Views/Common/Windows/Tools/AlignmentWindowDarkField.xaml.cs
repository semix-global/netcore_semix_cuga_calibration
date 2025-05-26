using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(AlignmentWindowDarkField), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class AlignmentWindowDarkField
{
    public AlignmentWindowDarkField()
    {
        InitializeComponent();
    }
}