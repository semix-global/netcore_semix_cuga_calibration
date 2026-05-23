using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status.Interfaces;
using System.ComponentModel;

namespace Core.Models.Models.Common.Status;

public sealed partial class ProductivityInformationAndLaserLightInformationStatus : ObservableObject, IStatus<ProductivityInformation>
{
    [ObservableProperty]
    public partial ProductivityInformation SelectedItem { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial IReadOnlyList<LaserLightInformationStatus> Items { get; set; } = [];

    public bool IsCalibrated => Items.All(c => c.IsCalibrated);

    partial void OnItemsChanged(IReadOnlyList<LaserLightInformationStatus>? oldValue, IReadOnlyList<LaserLightInformationStatus> newValue)
    {
        foreach (var item in oldValue ?? []) item.PropertyChanged -= ItemOnPropertyChanged;

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

public sealed partial class LaserLightInformationStatus : ObservableObject, IStatus<LaserLightInformation>
{
    [ObservableProperty]
    public partial LaserLightInformation SelectedItem { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial bool IsCalibrated { get; set; }
}