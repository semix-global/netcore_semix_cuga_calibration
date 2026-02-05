using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;

namespace Core.Models.Models.Common.Status;

public sealed partial class OpticsMagTypeEnumCalibrationStatus : ObservableObject
{
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private bool _isCalibrated;
}