using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status.Interfaces;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Helpers.Helpers.Structs;
using System.ComponentModel;

namespace Core.Models.Models.Common.Status;

public partial class ProductivityInformationAndApodizationStatus : ObservableCacheBase, IStatus<ProductivityInformation>
{
    [ObservableProperty]
    private ProductivityInformation _selectedItem = ProductivityInformation.Default;

    [ObservableProperty]
    private BindingList<OpticsApodizationModeStatus> _opticsApodizationModeCalibrationStatusList = [];

    public bool IsCalibrated => OpticsApodizationModeCalibrationStatusList.All(c => c.IsCalibrated);

    partial void OnOpticsApodizationModeCalibrationStatusListChanged(BindingList<OpticsApodizationModeStatus>? oldValue, BindingList<OpticsApodizationModeStatus> newValue)
    {
        if (oldValue != null) oldValue.ListChanged -= OnValueOnListChanged;

        newValue.ListChanged += OnValueOnListChanged;
    }

    private void OnValueOnListChanged(object? o, ListChangedEventArgs listChangedEventArgs)
    {
        OnPropertyChanged(nameof(IsCalibrated));
    }

    public static List<ProductivityInformationAndApodizationStatus> CreateList(IReadOnlyList<ProductivityInformation> productivityInformations) =>
    [
        .. productivityInformations.Select(t => new ProductivityInformationAndApodizationStatus
        {
            SelectedItem = t.Clone(),
            OpticsApodizationModeCalibrationStatusList = [.. EnumHelper.Enums<OpticsApodizationModeEnum>().Select(o => new OpticsApodizationModeStatus { SelectedItem = o, IsCalibrated = false })]
        })
    ];
}

public partial class OpticsApodizationModeStatus : ObservableCacheBase, IStatus<OpticsApodizationModeEnum>
{
    [ObservableProperty]
    private OpticsApodizationModeEnum _selectedItem;

    [ObservableProperty]
    private bool _isCalibrated;
}