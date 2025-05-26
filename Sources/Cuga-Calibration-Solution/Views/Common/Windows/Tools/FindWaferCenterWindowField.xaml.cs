using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(FindWaferCenterWindowField), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class FindWaferCenterWindowField
{
    public FindWaferCenterWindowField()
    {
        InitializeComponent();
    }
}