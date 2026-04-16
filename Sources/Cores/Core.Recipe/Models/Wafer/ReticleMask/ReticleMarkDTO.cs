

using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using System.Collections.ObjectModel;

namespace Core.Recipe.Models.Wafer.ReticleMask;

public sealed partial class ReticleMarkDTO : ObservableObject, ICloneable<ReticleMarkDTO>, IAdaptIn<ReticleMarkDTO, ReticleMarkDTO>
{
    [ObservableProperty]
    private ObservableCollection<ReticleMarkDTOItem> _microscopeReticleMarks = [];

    [ObservableProperty]
    private ObservableCollection<ReticleMarkDTOItem> _chuckReticleMarks = [];

    [ObservableProperty]
    private ObservableCollection<ReticleMarkDTOItem> _laserReticleMarks = [];

    public ReticleMarkDTO Clone() => new()
    {
        MicroscopeReticleMarks = [.. MicroscopeReticleMarks.Select(t => t.Clone())],
        ChuckReticleMarks = [.. ChuckReticleMarks.Select(t => t.Clone())],
        LaserReticleMarks = [.. LaserReticleMarks.Select(t => t.Clone())]
    };

    public ReticleMarkDTO AdaptIn(ReticleMarkDTO obj)
    {
        MicroscopeReticleMarks = [.. obj.MicroscopeReticleMarks.Select(t => new ReticleMarkDTOItem().AdaptIn(t))];
        ChuckReticleMarks = [.. obj.ChuckReticleMarks.Select(t => new ReticleMarkDTOItem().AdaptIn(t))];
        LaserReticleMarks = [.. obj.LaserReticleMarks.Select(t => new ReticleMarkDTOItem().AdaptIn(t))];
        return this;
    }
}