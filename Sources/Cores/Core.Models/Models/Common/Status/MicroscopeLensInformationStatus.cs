using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status.Interfaces;

namespace Core.Models.Models.Common.Status;

public sealed partial class MicroscopeLensInformationStatus : ObservableObject, IStatus<MicroscopeLensInformation>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation SelectedItem { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial bool IsCalibrated { get; set; }
}