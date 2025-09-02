using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Local.NoSQL.DB.Providers.Bases;

namespace Core.Models.Models.Common.Status;

public sealed partial class MicroscopeLensInfoCalibrationStatus : ObservableCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private bool _isCalibrated;
}