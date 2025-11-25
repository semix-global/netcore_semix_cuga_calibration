using CugaCalibration.Core.Attribute;
using CugaCalibration.Views.AOD.AODAlignment;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.CIB.MMD;

[IOCAppService(ServiceType = typeof(AODAlignmentUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBMMDUserControl
{
    [Permission]
    public CIBMMDUserControl()
    {
        InitializeComponent();
    }
}