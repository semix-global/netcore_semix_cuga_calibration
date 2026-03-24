using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(BestFocusWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class BestFocusWindow
{
    public BestFocusWindow()
    {
        InitializeComponent();
    }
}