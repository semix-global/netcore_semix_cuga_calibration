using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.CIB.LightMatching;

[IOCAppService(ServiceType = typeof(CIBLightMatchingUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBLightMatchingUserControl
{
    [Permission]
    public CIBLightMatchingUserControl()
    {
        InitializeComponent();
    }
}