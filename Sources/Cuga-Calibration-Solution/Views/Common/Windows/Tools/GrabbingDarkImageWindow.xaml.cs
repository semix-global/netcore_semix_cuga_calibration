using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(GrabbingDarkImageWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class GrabbingDarkImageWindow
{
    public GrabbingDarkImageWindow()
    {
        InitializeComponent();
    }
}