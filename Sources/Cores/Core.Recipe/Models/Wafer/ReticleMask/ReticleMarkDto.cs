using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using System.Collections.ObjectModel;

namespace Core.Recipe.Models.Wafer.ReticleMask;

public sealed partial class ReticleMarkDto : ObservableObject, ICloneable<ReticleMarkDto>, IAdaptIn<ReticleMarkDto, ReticleMarkDto>
{
    [ObservableProperty]
    private ObservableCollection<ReticleMarkItemDto> _microsocpeReticleMarkItemList = [];

    [ObservableProperty]
    private ObservableCollection<ReticleMarkItemDto> _chuckReticleMarkItemList = [];

    [ObservableProperty]
    private ObservableCollection<ReticleMarkItemDto> _laserReticleMarkItemList = [];

    public ReticleMarkDto Clone() => new()
    {
        MicrosocpeReticleMarkItemList = [.. MicrosocpeReticleMarkItemList.Select(t => t.Clone())],
        ChuckReticleMarkItemList = [.. ChuckReticleMarkItemList.Select(t => t.Clone())],
        LaserReticleMarkItemList = [.. LaserReticleMarkItemList.Select(t => t.Clone())]
    };

    public ReticleMarkDto AdaptIn(ReticleMarkDto obj)
    {
        MicrosocpeReticleMarkItemList = [.. obj.MicrosocpeReticleMarkItemList.Select(t => new ReticleMarkItemDto().AdaptIn(t))];
        ChuckReticleMarkItemList = [.. obj.ChuckReticleMarkItemList.Select(t => new ReticleMarkItemDto().AdaptIn(t))];
        LaserReticleMarkItemList = [.. obj.LaserReticleMarkItemList.Select(t => new ReticleMarkItemDto().AdaptIn(t))];
        return this;
    }
}