using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Status.Interfaces;
using System.ComponentModel;

namespace Core.Models.Models.Common.Status;

public sealed partial class OpticsIlluminationModeAndProductivityInformationStatus : ObservableObject, IStatus<OpticsIlluminationModeEnum>
{
    [ObservableProperty]
    public partial OpticsIlluminationModeEnum SelectedItem { get; set; }

    [ObservableProperty]
    public partial BindingList<ProductivityInformationStatus> ProductivityInformationStatusList { get; set; } = [];

    public bool IsCalibrated => ProductivityInformationStatusList.All(c => c.IsCalibrated);

    partial void OnProductivityInformationStatusListChanged(BindingList<ProductivityInformationStatus> oldValue, BindingList<ProductivityInformationStatus> newValue)
    {
        oldValue.ListChanged -= OnValueOnListChanged;

        newValue.ListChanged += OnValueOnListChanged;
    }

    private void OnValueOnListChanged(object? o, ListChangedEventArgs listChangedEventArgs)
    {
        OnPropertyChanged(nameof(IsCalibrated));
    }
}