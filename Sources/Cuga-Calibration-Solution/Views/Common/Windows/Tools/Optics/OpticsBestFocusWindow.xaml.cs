using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools.Optics;

[IOCAppService(ServiceType = typeof(OpticsBestFocusWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class OpticsBestFocusWindow
{
    public OpticsBestFocusWindow()
    {
        InitializeComponent();
    }
}