using Core.Utilities.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Microscope.CalChip;

[Permission]
[IOCAppService(ServiceType = typeof(MicroscopeCalChipCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopeCalChipCalibrationUserControl
{
    public MicroscopeCalChipCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}