using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Fourier.PupilCenterChannelSpecularBlocker
{
    /// <summary>
    /// PupilCenterChannelSpecularBlockerUserControl.xaml 的交互逻辑
    /// </summary>
    [Permission]
    [IOCAppService(ServiceType = typeof(PupilCenterChannelSpecularBlockerUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
    public partial class PupilCenterChannelSpecularBlockerUserControl
    {
        public PupilCenterChannelSpecularBlockerUserControl()
        {
            InitializeComponent();
            InitializePermissionControl();
        }
    }
}