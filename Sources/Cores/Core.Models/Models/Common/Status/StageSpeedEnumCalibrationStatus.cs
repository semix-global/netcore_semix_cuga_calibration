using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Local.NoSQL.DB.Providers.Bases;

namespace Core.Models.Models.Common.Status;

public sealed partial class StageSpeedEnumCalibrationStatus : ObservableObject
{
    [ObservableProperty]
    private StageSpeedEnum _stageSpeedEnum;

    [ObservableProperty]
    private bool _isCalibrated;
}