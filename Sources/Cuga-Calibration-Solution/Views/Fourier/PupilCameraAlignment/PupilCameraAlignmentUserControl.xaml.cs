using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.Views.Fourier.PupilCameraAlignment
{
    /// <summary>
    /// PupilCameraAlignment.xaml 的交互逻辑
    /// </summary>
    [Permission]
    [IOCAppService(ServiceType = typeof(PupilCameraAlignmentUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
    public partial class PupilCameraAlignmentUserControl
    {
        public PupilCameraAlignmentUserControl()
        {
            InitializeComponent();
            InitializePermissionControl();
        }
    }
}