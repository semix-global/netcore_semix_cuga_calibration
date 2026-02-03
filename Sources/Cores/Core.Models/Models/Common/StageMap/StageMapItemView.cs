using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;

namespace Core.Models.Models.Common.StageMap;

public sealed partial class StageMapItemView : ObservableObject
{
    [ObservableProperty]
    private StageMapItemDto _ideal = new();

    [ObservableProperty]
    private StageMapItemDto _real = new();

    [ObservableProperty]
    private StageMapItemDto _error = new();
}