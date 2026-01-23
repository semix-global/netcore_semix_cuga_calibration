using Core.Utilities.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Ads.PressureGains;

[Permission]
[IOCAppService(ServiceType = typeof(AdsPressureGainsCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AdsPressureGainsCalibrationUserControl
{
    public AdsPressureGainsCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}