using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.CIB.IlluminationProfile;

[PermissionControl]
[IOCAppService(ServiceType = typeof(CIBIlluminationProfileUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBIlluminationProfileUserControl
{
    public CIBIlluminationProfileUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}