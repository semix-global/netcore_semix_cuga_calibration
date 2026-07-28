using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Fourier.PupilCenterChannelFlexibleAperture
{
    /// <summary>
    /// PupilCenterChannelFlexibleApertureUserControl.xaml 的交互逻辑
    /// </summary>
    [Permission]
    [IOCAppService(ServiceType = typeof(PupilCenterChannelFlexibleApertureUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
    public partial class PupilCenterChannelFlexibleApertureUserControl
    {
        public PupilCenterChannelFlexibleApertureUserControl()
        {
            InitializeComponent();
            InitializePermissionControl();
        }
    }
}