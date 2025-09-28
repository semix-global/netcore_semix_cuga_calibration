using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views;

[IOCAppService(ServiceType = typeof(ApplicationAboutWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class ApplicationAboutWindow
{
    public ApplicationAboutWindow()
    {
        InitializeComponent();
    }
}