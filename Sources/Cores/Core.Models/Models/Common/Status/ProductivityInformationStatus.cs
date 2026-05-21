using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status.Interfaces;

namespace Core.Models.Models.Common.Status;

public sealed partial class ProductivityInformationStatus : ObservableObject, IStatus<ProductivityInformation>
{
    [ObservableProperty]
    public partial ProductivityInformation SelectedItem { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial bool IsCalibrated { get; set; }
}