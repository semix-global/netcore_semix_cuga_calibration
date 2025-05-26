using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Models.Models.Common.DarkField;

public sealed partial class GeneratePrescanAodWaveParamDto : GenerateAodWaveParamBase
{
    [ObservableProperty]
    private double _flatnessTime = 4300;
}