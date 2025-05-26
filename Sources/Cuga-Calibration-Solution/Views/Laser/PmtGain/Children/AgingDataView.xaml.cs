using CugaCalibration.ViewModels.Laser;
using System.ComponentModel;
using System.Windows;

namespace CugaCalibration.Views.Laser.PmtGain.Children;

public sealed partial class AgingDataView
{
    private bool _isLoaded;

    public AgingDataView()
    {
        InitializeComponent();

        Loaded -= OnLoaded;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isLoaded) return;
        _isLoaded = true;

        if (DataContext is not LaserPmtGainCalibrationViewModel viewModel) return;
        viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        viewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (sender is not LaserPmtGainCalibrationViewModel viewModel) return;

        switch (e.PropertyName)
        {
            case nameof(viewModel.SelectLaserPmtGainDto):
                Dispatcher.Invoke(() =>
                {
                    var selectLaserPmtGainDto = viewModel.SelectLaserPmtGainDto;
                    // var pmtName = "PMT" + selectLaserPmtGainDto?.PmtId.ToString() + "_Channel" + selectLaserPmtGainDto?.Channel.ToString();
                    var pmtNameN = "PMT:" + selectLaserPmtGainDto?.PmtId.ToString() + " Channel:" + selectLaserPmtGainDto?.Channel.ToString();
                    groupBox.Header = pmtNameN;
                });
                break;
        }
    }
}