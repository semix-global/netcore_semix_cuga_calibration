using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Local.NoSQL.DB.Providers.Bases;

namespace Core.Models.Models.Common.Status;

public sealed partial class MicroscopeMagnificationEnumCalibrationStatus : ObservableCacheBase
{
    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum;

    [ObservableProperty]
    private bool _isCalibrated;
}