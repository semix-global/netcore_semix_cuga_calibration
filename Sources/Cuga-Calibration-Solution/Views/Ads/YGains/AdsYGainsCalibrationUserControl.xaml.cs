using Core.Utilities.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

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