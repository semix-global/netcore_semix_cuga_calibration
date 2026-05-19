using CugaCalibration.ViewModels.Common.Windows.Diagnosis;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using System.Windows.Controls;

namespace CugaCalibration.Views.Common.Windows.Diagnosis.AdsMove;

[IOCAppService(ServiceType = typeof(AdsXYMoveDiagnosisWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class AdsXYMoveDiagnosisWindow
{
    public AdsXYMoveDiagnosisWindow()
    {
        InitializeComponent();
    }

    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is AdsXYMoveDiagnosisWindowViewModel viewModel)
        {
            var selectedItem = dataGrid?.SelectedItem as Position;
            if (selectedItem != null)
            {
                viewModel.OnSelectionChangedCommand(selectedItem);
            }
        }
    }
}