using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Chuck.RotateScale;

[IOCAppService(ServiceType = typeof(ChuckRotateScaleCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class ChuckRotateScaleCalibrationUserControl
{
    public ChuckRotateScaleCalibrationUserControl()
    {
        InitializeComponent();
    }
}