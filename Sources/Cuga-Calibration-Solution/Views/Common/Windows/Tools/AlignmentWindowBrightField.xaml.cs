using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(AlignmentWindowBrightField), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class AlignmentWindowBrightField
{
    public AlignmentWindowBrightField()
    {
        InitializeComponent();
    }
}