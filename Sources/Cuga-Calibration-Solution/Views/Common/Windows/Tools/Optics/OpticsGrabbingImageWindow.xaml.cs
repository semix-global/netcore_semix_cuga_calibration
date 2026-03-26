using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools.Optics;

[IOCAppService(ServiceType = typeof(OpticsGrabbingImageWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class OpticsGrabbingImageWindow
{
    public OpticsGrabbingImageWindow()
    {
        InitializeComponent();
    }
}