using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Status.Interfaces;
using Local.NoSQL.DB.Providers.Bases;
using System.ComponentModel;

namespace Core.Models.Models.Common.Status;

public sealed partial class OpticsIlluminationModeAndProductivityInformationStatus : ObservableCacheBase, IStatus<OpticsIlluminationModeEnum>
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _selectedItem;

    [ObservableProperty]
    private BindingList<ProductivityInformationStatus> _productivityInformationCalibrationStatusList = [];

    public bool IsCalibrated => ProductivityInformationCalibrationStatusList.All(c => c.IsCalibrated);

    partial void OnProductivityInformationCalibrationStatusListChanged(BindingList<ProductivityInformationStatus>? oldValue, BindingList<ProductivityInformationStatus> newValue)
    {
        if (oldValue != null) oldValue.ListChanged -= OnValueOnListChanged;

        newValue.ListChanged += OnValueOnListChanged;
    }

    private void OnValueOnListChanged(object? o, ListChangedEventArgs listChangedEventArgs)
    {
        OnPropertyChanged(nameof(IsCalibrated));
    }
}