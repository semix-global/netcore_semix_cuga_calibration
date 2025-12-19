using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status.Interfaces;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Helpers.Helpers.Structs;

namespace Core.Models.Models.Common.Status;

public partial class OpticsIlluminationModeAndProductivityInformationAndApodizationCalibrationStatus : ObservableCacheBase, ICalibrationStatus<OpticsIlluminationModeEnum>
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _selectedItem;

    [ObservableProperty]
    private BindingList<ProductivityInformationAndApodizationCalibrationStatus> _productivityInformationAndApodizationCalibrationStatusList = [];

    public bool IsCalibrated => ProductivityInformationAndApodizationCalibrationStatusList.All(c => c.IsCalibrated);

    partial void OnProductivityInformationAndApodizationCalibrationStatusListChanged(BindingList<ProductivityInformationAndApodizationCalibrationStatus>? oldValue, BindingList<ProductivityInformationAndApodizationCalibrationStatus> newValue)
    {
        if (oldValue != null) oldValue.ListChanged -= OnValueOnListChanged;

        newValue.ListChanged += OnValueOnListChanged;
    }

    private void OnValueOnListChanged(object? o, ListChangedEventArgs listChangedEventArgs)
    {
        OnPropertyChanged(nameof(IsCalibrated));
    }

    public static List<OpticsIlluminationModeAndProductivityInformationAndApodizationCalibrationStatus> CreateList(IReadOnlyList<ProductivityInformation> productivityInformations) =>
        EnumHelper.Enums<OpticsIlluminationModeEnum>()
            .Select(t => new OpticsIlluminationModeAndProductivityInformationAndApodizationCalibrationStatus()
            {
                SelectedItem = t,
                ProductivityInformationAndApodizationCalibrationStatusList = [..ProductivityInformationAndApodizationCalibrationStatus.CreateList(productivityInformations)]
            }).ToList();
}

public partial class ProductivityInformationAndApodizationCalibrationStatus : ObservableCacheBase, ICalibrationStatus<ProductivityInformation>
{
    [ObservableProperty]
    private ProductivityInformation _selectedItem = ProductivityInformation.Default;

    [ObservableProperty]
    private BindingList<OpticsApodizationModeCalibrationStatus> _opticsApodizationModeCalibrationStatusList = [];

    public bool IsCalibrated => OpticsApodizationModeCalibrationStatusList.All(c => c.IsCalibrated);

    partial void OnOpticsApodizationModeCalibrationStatusListChanged(BindingList<OpticsApodizationModeCalibrationStatus>? oldValue, BindingList<OpticsApodizationModeCalibrationStatus> newValue)
    {
        if (oldValue != null) oldValue.ListChanged -= OnValueOnListChanged;

        newValue.ListChanged += OnValueOnListChanged;
    }

    private void OnValueOnListChanged(object? o, ListChangedEventArgs listChangedEventArgs)
    {
        OnPropertyChanged(nameof(IsCalibrated));
    }

    public static List<ProductivityInformationAndApodizationCalibrationStatus> CreateList(IReadOnlyList<ProductivityInformation> productivityInformations) =>
        productivityInformations.Select(t => new ProductivityInformationAndApodizationCalibrationStatus
        {
            SelectedItem = t.Clone(),
            OpticsApodizationModeCalibrationStatusList = [..EnumHelper.Enums<OpticsApodizationModeEnum>().Select(o => new OpticsApodizationModeCalibrationStatus { SelectedItem = o, IsCalibrated = false })]
        }).ToList();
}

public partial class OpticsApodizationModeCalibrationStatus : ObservableCacheBase, ICalibrationStatus<OpticsApodizationModeEnum>
{
    [ObservableProperty]
    private OpticsApodizationModeEnum _selectedItem;

    [ObservableProperty]
    private bool _isCalibrated;
}