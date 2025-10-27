using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.AodDelay;

[IOCAppService(ServiceType = typeof(LaserAODDelayUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserAODDelayUserControl
{
    [Permission]
    public LaserAODDelayUserControl()
    {
        InitializeComponent();
    }
}