using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Ads.PressureGains;

[IOCAppService(ServiceType = typeof(AdsPressureGainsCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AdsPressureGainsCalibrationUserControl
{
    [Permission]
    public AdsPressureGainsCalibrationUserControl()
    {
        InitializeComponent();
    }
}