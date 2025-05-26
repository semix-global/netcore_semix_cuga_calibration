using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Models.Models.Common.DarkField;

public sealed partial class GenerateChirpAodWaveParamDto : GenerateAodWaveParamBase
{
    [ObservableProperty]
    private double _soundPackageLength = 3.2;

    [ObservableProperty]
    private string _frequencyCompensationsFilePath = string.Empty;
}