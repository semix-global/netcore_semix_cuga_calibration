using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.AOD.BestFocusAndAstigmatism;

[IOCAppService(ServiceType = typeof(BestFocusAndAstigmatismCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class BestFocusAndAstigmatismCalibrationUserControl
{
    [Permission]
    public BestFocusAndAstigmatismCalibrationUserControl()
    {
        InitializeComponent();
    }
}