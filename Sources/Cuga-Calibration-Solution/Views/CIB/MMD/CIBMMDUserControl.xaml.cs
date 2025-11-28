using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.CIB.MMD;

[IOCAppService(ServiceType = typeof(CIBMMDUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBMMDUserControl
{
    [Permission]
    public CIBMMDUserControl()
    {
        InitializeComponent();
    }
}