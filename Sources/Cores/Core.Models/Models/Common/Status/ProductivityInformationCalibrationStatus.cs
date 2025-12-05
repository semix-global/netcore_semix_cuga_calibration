using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Local.NoSQL.DB.Providers.Bases;

namespace Core.Models.Models.Common.Status;

public sealed partial class ProductivityInformationCalibrationStatus : ObservableCacheBase
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private bool _isCalibrated;

    public static List<ProductivityInformationCalibrationStatus> CreateList(IReadOnlyList<ProductivityInformation> productivityInformations) =>
        productivityInformations.Select(t => new ProductivityInformationCalibrationStatus { ProductivityInformation = t, IsCalibrated = false }).ToList();
}