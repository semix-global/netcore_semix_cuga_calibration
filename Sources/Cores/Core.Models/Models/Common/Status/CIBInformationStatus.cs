using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status.Interfaces;

namespace Core.Models.Models.Common.Status;

public sealed partial class CIBInformationStatus : ObservableObject, IStatus<CIBInformation>
{
    [ObservableProperty]
    public partial CIBInformation SelectedItem { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial bool IsCalibrated { get; set; }
}