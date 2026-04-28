using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.File.Save;

[IOCAppService(ServiceType = typeof(SaveFileWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class SaveFileWindow
{
    public SaveFileWindow()
    {
        InitializeComponent();
    }
}