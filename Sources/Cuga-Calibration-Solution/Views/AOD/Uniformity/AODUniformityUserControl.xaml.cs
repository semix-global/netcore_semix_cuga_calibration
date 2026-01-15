using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.AOD.Uniformity;

[IOCAppService(ServiceType = typeof(AODUniformityUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AODUniformityUserControl
{
    [Permission]
    public AODUniformityUserControl()
    {
        InitializeComponent();
    }
}