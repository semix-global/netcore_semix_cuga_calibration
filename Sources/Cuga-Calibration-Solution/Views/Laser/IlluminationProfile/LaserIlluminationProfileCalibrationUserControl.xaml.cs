using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.IlluminationProfile;

[IOCAppService(ServiceType = typeof(LaserIlluminationProfileCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserIlluminationProfileCalibrationUserControl
{
    public LaserIlluminationProfileCalibrationUserControl()
    {
        InitializeComponent();
    }
}