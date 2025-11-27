using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Local.NoSQL.DB.Providers.Bases;

namespace Core.Models.Models.Common.Status;

public sealed partial class OpticsIncidentModeCalibrationStatus : ObservableCacheBase
{
    [ObservableProperty]
    private OpticsIncidentModeEnum _opticsIncidentModeEnum = CalibrationConstantsHelper.MainOpticsIncidentModeEnum;

    [ObservableProperty]
    private bool _isCalibrated;
}