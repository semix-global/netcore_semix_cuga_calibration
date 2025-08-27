using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Local.NoSQL.DB.Providers.Bases;

namespace Core.Models.Models.Common.Status;

public sealed partial class MicroscopeLensInfoCalibrationStatus : ObservableCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = new();

    [ObservableProperty]
    private bool _isCalibrated;
}