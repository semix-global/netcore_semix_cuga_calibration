using Core.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Ads.XGains;

[PermissionControl]
[IOCAppService(ServiceType = typeof(AdsXGainsCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AdsXGainsCalibrationUserControl
{
    public AdsXGainsCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}