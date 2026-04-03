using Core.Utilities.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

