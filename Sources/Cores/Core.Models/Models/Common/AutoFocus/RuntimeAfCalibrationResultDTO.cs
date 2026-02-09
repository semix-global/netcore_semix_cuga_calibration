using CommunityToolkit.Mvvm.ComponentModel;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AutoFocus;

public sealed partial class RuntimeAfCalibrationResultDTO : ObservableCacheBase, ICloneable<RuntimeAfCalibrationResultDTO>
{
    [ObservableProperty]
    private bool _isAFServo = true;

    [ObservableProperty]
    private double _eCSValue;

    [ObservableProperty]
    private double _motorValue;

    [ObservableProperty]
    private double _darkFieldQuality;

    [ObservableProperty]
    private string _rawImageFilePath = string.Empty;

    [ObservableProperty]
    private string _darkFieldFilePath = string.Empty;

    public RuntimeAfCalibrationResultDTO Clone() => new()
    {
        IsAFServo = IsAFServo,
        ECSValue = ECSValue,
        MotorValue = MotorValue,
        DarkFieldQuality = DarkFieldQuality,
        RawImageFilePath = RawImageFilePath,
        DarkFieldFilePath = DarkFieldFilePath,
        Id = Id,
        Expiration = Expiration
    };

    public object ToHtmlAnonymous() => new
    {
        IsAFServo,
        ECSValue,
        MotorValue,
        DarkFieldQuality,
        DarkFieldFilePath,
        RawImageFilePath
    };
}