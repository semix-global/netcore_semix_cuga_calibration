using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibrationTest.Views;

[IOCAppService(ServiceType = typeof(StageMapWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class StageMapWindow
{
    public StageMapWindow()
    {
        InitializeComponent();
    }
}