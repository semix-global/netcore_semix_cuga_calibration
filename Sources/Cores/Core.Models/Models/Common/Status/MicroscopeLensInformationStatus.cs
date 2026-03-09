using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status.Interfaces;

namespace Core.Models.Models.Common.Status;

public sealed partial class MicroscopeLensInformationStatus : ObservableObject, IStatus<MicroscopeLensInformation>
{
    [ObservableProperty]
    private MicroscopeLensInformation _selectedItem = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private bool _isCalibrated;
}