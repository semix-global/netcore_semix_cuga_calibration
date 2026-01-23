using Core.Utilities.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.CIB.XPixelSize;

[Permission]
[IOCAppService(ServiceType = typeof(CIBXPixelSizeUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBXPixelSizeUserControl
{
    public CIBXPixelSizeUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}