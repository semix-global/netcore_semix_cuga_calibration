using Core.Utilities.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Fourier.PupilSideChannelFlexibleAperture
{
    /// <summary>
    /// PupilSideChannelFlexibleApertureUserControl.xaml 的交互逻辑
    /// </summary>
    [Permission]
    [IOCAppService(ServiceType = typeof(PupilSideChannelFlexibleApertureUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
    public partial class PupilSideChannelFlexibleApertureUserControl
    {
        public PupilSideChannelFlexibleApertureUserControl()
        {
            InitializeComponent();
            InitializePermissionControl();
        }
    }
}