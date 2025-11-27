using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Local.NoSQL.DB.Providers.Bases;
using System.ComponentModel;

namespace Core.Models.Models.Common.Status;

public sealed partial class OpticsIncidentModeEnumAndProductivityInformationCalibrationStatus : ObservableCacheBase
{
    [ObservableProperty]
    private OpticsIncidentModeEnum _opticsIncidentModeEnum;

    [ObservableProperty]
    private BindingList<ProductivityInformationCalibrationStatus> _productivityInformationCalibrationStatusList = [];

    public bool IsCalibrated => ProductivityInformationCalibrationStatusList.All(c => c.IsCalibrated);

    partial void OnProductivityInformationCalibrationStatusListChanged(BindingList<ProductivityInformationCalibrationStatus>? oldValue, BindingList<ProductivityInformationCalibrationStatus> newValue)
    {
        if (oldValue != null) oldValue.ListChanged -= OnValueOnListChanged;

        newValue.ListChanged += OnValueOnListChanged;
    }

    private void OnValueOnListChanged(object? o, ListChangedEventArgs listChangedEventArgs)
    {
        OnPropertyChanged(nameof(IsCalibrated));
    }
}