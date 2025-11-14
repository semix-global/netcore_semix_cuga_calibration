using CugaCalibration.ViewModels.Common.Windows.Diagnosis;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using System.Windows.Controls;

namespace CugaCalibration.Views.Common.Windows.Diagnosis.AfGetNsc;

[IOCAppService(ServiceType = typeof(AfGetAnyNscDiagnosisWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]

/// <summary>
/// AfGetAnyNscDiagnosisWindow.xaml 的交互逻辑
/// </summary>
public partial class AfGetAnyNscDiagnosisWindow
{
    public AfGetAnyNscDiagnosisWindow()
    {
        InitializeComponent();
    }

    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is AfGetAnyNscDiagnosisWindowViewModel viewModel)
        {
            var selectedItem = dataGrid.SelectedItem as AfGetAnyNscDiagnosisWindowViewModel.Position;
            if (selectedItem != null)
            {
                // 调用 ViewModel 中的方法
                viewModel.OnSelectionChangedCommand(selectedItem);
            }
        }
    }
}
