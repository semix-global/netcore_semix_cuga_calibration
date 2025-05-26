using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Chuck.AutoFocus;

[IOCAppService(ServiceType = typeof(ChuckAutoFocusCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckAutoFocusCalibrationUserControl
{
    public ChuckAutoFocusCalibrationUserControl()
    {
        InitializeComponent();
    }
}