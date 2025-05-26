using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Chuck.Center;

[IOCAppService(ServiceType = typeof(ChuckCenterCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckCenterCalibrationUserControl
{
    [Permission]
    public ChuckCenterCalibrationUserControl()
    {
        InitializeComponent();
    }
}