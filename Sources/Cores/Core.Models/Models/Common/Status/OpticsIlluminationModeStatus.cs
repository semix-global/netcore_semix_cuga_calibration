using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Status.Interfaces;

namespace Core.Models.Models.Common.Status;

public sealed partial class OpticsIlluminationModeStatus : ObservableObject, IStatus<OpticsIlluminationModeEnum>
{
    [ObservableProperty]
    public partial OpticsIlluminationModeEnum SelectedItem { get; set; }

    [ObservableProperty]
    public partial bool IsCalibrated { get; set; }
}