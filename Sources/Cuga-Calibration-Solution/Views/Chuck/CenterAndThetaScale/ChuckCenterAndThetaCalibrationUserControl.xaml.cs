using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Chuck.CenterAndThetaScale;

[IOCAppService(ServiceType = typeof(ChuckCenterAndThetaCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckCenterAndThetaCalibrationUserControl
{
    [Permission]
    public ChuckCenterAndThetaCalibrationUserControl()
    {
        InitializeComponent();
    }
}