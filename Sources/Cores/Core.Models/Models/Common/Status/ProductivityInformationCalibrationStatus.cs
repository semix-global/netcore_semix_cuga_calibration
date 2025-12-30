using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status.Interfaces;
using Local.NoSQL.DB.Providers.Bases;

namespace Core.Models.Models.Common.Status;

public partial class ProductivityInformationCalibrationStatus : ObservableCacheBase, ICalibrationStatus<ProductivityInformation>
{
    [ObservableProperty]
    private ProductivityInformation _selectedItem = ProductivityInformation.Default;

    [ObservableProperty]
    private bool _isCalibrated;

    public static List<ProductivityInformationCalibrationStatus> CreateList(IReadOnlyList<ProductivityInformation> productivityInformations) =>
        productivityInformations.Select(t => new ProductivityInformationCalibrationStatus { SelectedItem = t, IsCalibrated = false }).ToList();
}