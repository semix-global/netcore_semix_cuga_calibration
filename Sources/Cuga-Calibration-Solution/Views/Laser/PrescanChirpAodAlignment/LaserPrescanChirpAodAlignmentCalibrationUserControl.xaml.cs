using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.PrescanChirpAodAlignment;

[IOCAppService(ServiceType = typeof(LaserPrescanChirpAodAlignmentCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserPrescanChirpAodAlignmentCalibrationUserControl
{
    [Permission]
    public LaserPrescanChirpAodAlignmentCalibrationUserControl()
    {
        InitializeComponent();
    }
}