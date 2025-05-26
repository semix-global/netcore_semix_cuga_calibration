using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Ads.XGains;

[IOCAppService(ServiceType = typeof(AdsXGainsCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AdsXGainsCalibrationUserControl
{
    [Permission]
    public AdsXGainsCalibrationUserControl()
    {
        InitializeComponent();
    }
}