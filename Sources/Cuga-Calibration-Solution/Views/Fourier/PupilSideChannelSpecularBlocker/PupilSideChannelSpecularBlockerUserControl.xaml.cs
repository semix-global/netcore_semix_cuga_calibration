using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using System.Windows.Controls;

namespace CugaCalibration.Views.Fourier.PupilSideChannelSpecularBlocker
{
    /// <summary>
    /// PupilSideChannelSpecularBlockerUserControl.xaml 的交互逻辑
    /// </summary>
    [Permission]
    [IOCAppService(ServiceType = typeof(PupilSideChannelSpecularBlockerUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
    public partial class PupilSideChannelSpecularBlockerUserControl : UserControl
    {
        public PupilSideChannelSpecularBlockerUserControl()
        {
            InitializeComponent();
            InitializePermissionControl();
        }
    }
}