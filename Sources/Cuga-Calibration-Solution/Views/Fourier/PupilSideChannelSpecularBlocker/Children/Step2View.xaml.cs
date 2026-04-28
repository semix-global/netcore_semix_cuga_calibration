using CugaCalibration.ViewModels.Flourier;
using System.Windows.Controls;

namespace CugaCalibration.Views.Fourier.PupilSideChannelSpecularBlocker.Children
{
    /// <summary>
    /// Step2View.xaml 的交互逻辑
    /// </summary>
    public partial class Step2View : UserControl
    {
        public Step2View()
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