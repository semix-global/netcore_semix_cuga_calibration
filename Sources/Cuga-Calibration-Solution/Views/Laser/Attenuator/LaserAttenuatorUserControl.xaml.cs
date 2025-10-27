using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.Attenuator;

[IOCAppService(ServiceType = typeof(LaserAttenuatorUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserAttenuatorUserControl
{
    [Permission]
    public LaserAttenuatorUserControl()
    {
        InitializeComponent();
    }
}