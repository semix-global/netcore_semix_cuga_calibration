using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.AOD.Uniformity;

[IOCAppService(ServiceType = typeof(CIBXTCUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBXTCUserControl
{
    [Permission]
    public CIBXTCUserControl()
    {
        InitializeComponent();
    }
}