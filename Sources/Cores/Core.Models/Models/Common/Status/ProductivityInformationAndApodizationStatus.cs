using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status.Interfaces;
using System.ComponentModel;

namespace Core.Models.Models.Common.Status;

public partial class ProductivityInformationAndApodizationStatus : ObservableObject, IStatus<ProductivityInformation>
{
    [ObservableProperty]
    public partial ProductivityInformation SelectedItem { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial IReadOnlyList<OpticsApodizationModeStatus> Items { get; set; } = [];

    public bool IsCalibrated => Items.All(c => c.IsCalibrated);

    partial void OnItemsChanged(IReadOnlyList<OpticsApodizationModeStatus> oldValue, IReadOnlyList<OpticsApodizationModeStatus> newValue)
    {
        foreach (var item in oldValue) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        OnPropertyChanged(nameof(IsCalibrated));

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(IsCalibrated));
    }
}

public partial class OpticsApodizationModeStatus : ObservableObject, IStatus<OpticsApodizationModeEnum>
{
    [ObservableProperty]
    public partial OpticsApodizationModeEnum SelectedItem { get; set; }

    [ObservableProperty]
    public partial bool IsCalibrated { get; set; }
}