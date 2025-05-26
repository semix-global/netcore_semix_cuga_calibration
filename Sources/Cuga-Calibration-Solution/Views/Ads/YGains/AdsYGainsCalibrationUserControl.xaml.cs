using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Ads.YGains;

[IOCAppService(ServiceType = typeof(AdsYGainsCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AdsYGainsCalibrationUserControl
{
    [Permission]
    public AdsYGainsCalibrationUserControl()
    {
        InitializeComponent();
    }
}