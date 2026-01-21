using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status.Interfaces;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Helpers.Helpers.Structs;
using System.ComponentModel;

namespace Core.Models.Models.Common.Status;

public partial class OpticsIlluminationModeAndProductivityInformationAndApodizationStatus : ObservableCacheBase, IStatus<OpticsIlluminationModeEnum>
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _selectedItem;

    [ObservableProperty]
    private BindingList<ProductivityInformationAndApodizationStatus> _productivityInformationAndApodizationCalibrationStatusList = [];

    public bool IsCalibrated => ProductivityInformationAndApodizationCalibrationStatusList.All(c => c.IsCalibrated);

    partial void OnProductivityInformationAndApodizationCalibrationStatusListChanged(BindingList<ProductivityInformationAndApodizationStatus>? oldValue, BindingList<ProductivityInformationAndApodizationStatus> newValue)
    {
        if (oldValue != null) oldValue.ListChanged -= OnValueOnListChanged;

        newValue.ListChanged += OnValueOnListChanged;
    }

    private void OnValueOnListChanged(object? o, ListChangedEventArgs listChangedEventArgs)
    {
        OnPropertyChanged(nameof(IsCalibrated));
    }

    public static List<OpticsIlluminationModeAndProductivityInformationAndApodizationStatus> CreateList(IReadOnlyList<ProductivityInformation> productivityInformations) =>
        EnumHelper.Enums<OpticsIlluminationModeEnum>()
            .Select(t => new OpticsIlluminationModeAndProductivityInformationAndApodizationStatus
            {
                SelectedItem = t,
                ProductivityInformationAndApodizationCalibrationStatusList = [.. ProductivityInformationAndApodizationStatus.CreateList(productivityInformations)]
            }).ToList();
}

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
        productivityInformations.Select(t => new ProductivityInformationAndApodizationStatus
        {
            SelectedItem = t.Clone(),
            OpticsApodizationModeCalibrationStatusList = [.. EnumHelper.Enums<OpticsApodizationModeEnum>().Select(o => new OpticsApodizationModeStatus { SelectedItem = o, IsCalibrated = false })]
        }).ToList();
}

public partial class OpticsApodizationModeStatus : ObservableCacheBase, IStatus<OpticsApodizationModeEnum>
{
    [ObservableProperty]
    private OpticsApodizationModeEnum _selectedItem;

    [ObservableProperty]
    private bool _isCalibrated;
}