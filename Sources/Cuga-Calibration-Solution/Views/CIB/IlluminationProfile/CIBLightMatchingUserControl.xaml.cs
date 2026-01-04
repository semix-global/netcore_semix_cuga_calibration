using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.CIB.IlluminationProfile;

[IOCAppService(ServiceType = typeof(CIBIlluminationProfileUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBIlluminationProfileUserControl
{
    [Permission]
    public CIBIlluminationProfileUserControl()
    {
        InitializeComponent();
    }
}