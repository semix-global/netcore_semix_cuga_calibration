using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(GrabbingDarkImagePointToPointWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class GrabbingDarkImagePointToPointWindow
{
    public GrabbingDarkImagePointToPointWindow()
    {
        InitializeComponent();
    }
}