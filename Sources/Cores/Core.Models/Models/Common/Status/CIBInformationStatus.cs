using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status.Interfaces;

namespace Core.Models.Models.Common.Status;

public sealed partial class CIBInformationStatus : ObservableObject, IStatus<CIBInformation>
{
    [ObservableProperty]
    private CIBInformation _selectedItem = CIBInformation.Default;

    [ObservableProperty]
    private bool _isCalibrated;
}