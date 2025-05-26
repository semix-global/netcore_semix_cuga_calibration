using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Microscope.Focus;

[IOCAppService(ServiceType = typeof(MicroscopeFocusCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopeFocusCalibrationUserControl
{
    [Permission]
    public MicroscopeFocusCalibrationUserControl()
    {
        InitializeComponent();
    }
}