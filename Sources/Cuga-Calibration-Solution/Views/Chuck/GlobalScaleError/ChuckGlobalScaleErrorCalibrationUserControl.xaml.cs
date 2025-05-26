using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Chuck.GlobalScaleError;

[IOCAppService(ServiceType = typeof(ChuckGlobalScaleErrorCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class ChuckGlobalScaleErrorCalibrationUserControl
{
    [Permission]
    public ChuckGlobalScaleErrorCalibrationUserControl()
    {
        InitializeComponent();
    }
}