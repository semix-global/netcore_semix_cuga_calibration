using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Common.Recipe.Wafer.ReticleMask;

public sealed partial class ReticleMarkDto : ObservableObject, ICloneable<ReticleMarkDto>
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
}