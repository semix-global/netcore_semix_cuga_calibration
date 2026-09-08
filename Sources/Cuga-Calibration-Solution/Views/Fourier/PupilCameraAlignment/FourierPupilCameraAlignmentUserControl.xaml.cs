using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Fourier.PupilCameraAlignment;

[Permission]
[IOCAppService(ServiceType = typeof(FourierPupilCameraAlignmentUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class FourierPupilCameraAlignmentUserControl
{
    public FourierPupilCameraAlignmentUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}