using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Ads.YGains;

[Permission]
[IOCAppService(ServiceType = typeof(AdsYGainsCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AdsYGainsCalibrationUserControl
{
    public AdsYGainsCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}