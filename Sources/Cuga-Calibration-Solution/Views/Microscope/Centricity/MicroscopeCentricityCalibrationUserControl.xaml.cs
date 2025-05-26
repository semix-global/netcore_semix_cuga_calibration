using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Microscope.Centricity;

[IOCAppService(ServiceType = typeof(MicroscopeCentricityCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopeCentricityCalibrationUserControl
{
    [Permission]
    public MicroscopeCentricityCalibrationUserControl()
    {
        InitializeComponent();
    }
}