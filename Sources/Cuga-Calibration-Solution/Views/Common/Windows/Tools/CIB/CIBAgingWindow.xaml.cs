using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Common.Windows.Tools.CIB;

[IOCAppService(ServiceType = typeof(CIBAgingWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class CIBAgingWindow
{
    public CIBAgingWindow()
    {
        InitializeComponent();
    }
}