using Core.Utilities.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Microscope.Focus;

[Permission]
[IOCAppService(ServiceType = typeof(MicroscopeFocusCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopeFocusCalibrationUserControl
{
    public MicroscopeFocusCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}