using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.CIB.LineCentricity;

[Permission]
[IOCAppService(ServiceType = typeof(CIBLineCentricityUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBLineCentricityUserControl
{
    public CIBLineCentricityUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}