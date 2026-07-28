using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Microscope.CalChip;

[Permission]
[IOCAppService(ServiceType = typeof(MicroscopeCalChipUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopeCalChipUserControl
{
    public MicroscopeCalChipUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}