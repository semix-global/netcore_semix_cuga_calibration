using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Pattern;
using Local.NoSQL.DB.Providers.Bases;

namespace Core.Models.Models.Common.Status;

public sealed partial class MicroscopeMagnificationInfoCalibrationStatus : ObservableCacheBase
{
    [ObservableProperty]
    private MicroscopeMagnificationInfo _microscopeMagnificationInfo = new();

    [ObservableProperty]
    private bool _isCalibrated;
}