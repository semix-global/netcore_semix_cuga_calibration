using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Models.Models.Common.DarkField;

public sealed partial class GenerateAodWaveUniformityItemDto : ObservableObject
{
    [ObservableProperty]
    private double _centerFrequency;

    [ObservableProperty]
    private double _coefficient;
}