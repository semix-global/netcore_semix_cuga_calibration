using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status.Interfaces;
using Local.NoSQL.DB.Providers.Bases;
using System.ComponentModel;

namespace Core.Models.Models.Common.Status;

public sealed partial class OpticsIlluminationModeAndProductivityInformationCalibrationStatus : ObservableCacheBase, ICalibrationStatus<OpticsIlluminationModeEnum>
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _selectedItem;

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

public partial class ProductivityInformationCalibrationStatus : ObservableCacheBase, ICalibrationStatus<ProductivityInformation>
{
    [ObservableProperty]
    private ProductivityInformation _selectedItem = ProductivityInformation.Default;

    [ObservableProperty]
    private bool _isCalibrated;

    public static List<ProductivityInformationCalibrationStatus> CreateList(IReadOnlyList<ProductivityInformation> productivityInformations) =>
        productivityInformations.Select(t => new ProductivityInformationCalibrationStatus { SelectedItem = t, IsCalibrated = false }).ToList();
}