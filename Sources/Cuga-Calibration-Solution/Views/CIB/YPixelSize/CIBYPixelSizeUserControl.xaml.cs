using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.CIB.YPixelSize;

[Permission]
[IOCAppService(ServiceType = typeof(CIBYPixelSizeUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBYPixelSizeUserControl
{
    public CIBYPixelSizeUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}