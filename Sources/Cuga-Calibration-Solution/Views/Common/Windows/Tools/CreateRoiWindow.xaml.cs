using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(CreateRoiWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class CreateRoiWindow
{
    public CreateRoiWindow()
    {
        InitializeComponent();
    }
}