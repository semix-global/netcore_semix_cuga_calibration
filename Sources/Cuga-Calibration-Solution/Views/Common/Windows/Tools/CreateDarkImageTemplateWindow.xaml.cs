using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(CreateDarkImageTemplateWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class CreateDarkImageTemplateWindow
{
    public CreateDarkImageTemplateWindow()
    {
        InitializeComponent();
    }
}