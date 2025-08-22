using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;

namespace Core.Models.Models.Common.AODWaveform;

public partial class AODWaveformResult : ObservableObject
{
    [ObservableProperty]
    private OpticsAODElectrodeEnum _opticsAODElectrodeEnum;

    [ObservableProperty]
    private string _filePath = string.Empty;
}