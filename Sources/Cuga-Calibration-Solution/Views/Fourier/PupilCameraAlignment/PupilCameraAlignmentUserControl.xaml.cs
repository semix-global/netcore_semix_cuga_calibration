using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Fourier.PupilCameraAlignment;

[Permission]
[IOCAppService(ServiceType = typeof(PupilCameraAlignmentUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class PupilCameraAlignmentUserControl
{
    public PupilCameraAlignmentUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}