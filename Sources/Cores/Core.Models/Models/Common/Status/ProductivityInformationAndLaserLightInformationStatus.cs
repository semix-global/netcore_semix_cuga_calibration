using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status.Interfaces;
using Local.NoSQL.DB.Providers.Bases;
using System.ComponentModel;

namespace Core.Models.Models.Common.Status;

public sealed partial class ProductivityInformationAndLaserLightInformationStatus : ObservableObject, IStatus<ProductivityInformation>
{
    [ObservableProperty]
    private ProductivityInformation _selectedItem = ProductivityInformation.Default;

    [ObservableProperty]
    private IReadOnlyList<LaserLightInformationStatus> _items = [];

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
    private LaserLightInformation _selectedItem = LaserLightInformation.Default;

    [ObservableProperty]
    private bool _isCalibrated;
}