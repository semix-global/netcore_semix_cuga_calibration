using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.AOD.AODAlignment;

[IOCAppService(ServiceType = typeof(AODAlignmentUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AODAlignmentUserControl
{
    [Permission]
    public AODAlignmentUserControl()
    {
        InitializeComponent();
    }
}