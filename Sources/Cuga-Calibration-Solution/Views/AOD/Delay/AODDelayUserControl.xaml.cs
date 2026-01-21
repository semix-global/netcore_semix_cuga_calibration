using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.AOD.Delay;

[IOCAppService(ServiceType = typeof(AODDelayUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AODDelayUserControl
{
    [Permission]
    public AODDelayUserControl()
    {
        InitializeComponent();
    }
}