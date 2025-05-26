using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Microscope.CalChip;

[IOCAppService(ServiceType = typeof(MicroscopeCalChipCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopeCalChipCalibrationUserControl
{
    [Permission]
    public MicroscopeCalChipCalibrationUserControl()
    {
        InitializeComponent();
    }
}