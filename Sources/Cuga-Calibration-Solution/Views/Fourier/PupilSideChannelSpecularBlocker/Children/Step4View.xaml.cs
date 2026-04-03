using CugaCalibration.ViewModels.Flourier;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace CugaCalibration.Views.Fourier.PupilSideChannelSpecularBlocker.Children
{
    /// <summary>
    /// Step4View.xaml 的交互逻辑
    /// </summary>
    public partial class Step4View : UserControl
    {
        public Step4View()
        {
            InitializeComponent();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is PupilSideChannelSpecularBlockerViewModel viewModel) // 替换 YourViewModel 为你的实际 ViewModel 类名
            {
                var tabControl = (TabControl)sender;
                switch (tabControl.SelectedIndex)
                {
                    case 0: // 第一个 Tab：“Initial Image File”
                        //viewModel.Cache.IsToggleSelectRectROIDrawableInitialCh2 = false;
                        //viewModel.Cache.IsToggleSelectRectROIDrawableProcessCh2 = false;
                        break;

                    case 1: // 第二个 Tab：“Process Image File”
                        //viewModel.Cache.IsToggleSelectRectROIDrawableInitialCh2 = false;
                        //viewModel.Cache.IsToggleSelectRectROIDrawableProcessCh2 = true;
                        break;
                }
            }
        }
    }
}
