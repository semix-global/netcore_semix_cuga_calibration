using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;

namespace Core.Models.Models.Common.Status;

public sealed partial class MicroscopeLensInfoCalibrationStatus : ObservableObject
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private bool _isCalibrated;
}