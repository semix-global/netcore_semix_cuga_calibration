using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Models.Models.Common.StageMap;

public sealed partial class StageMapItemView : ObservableObject
{
    [ObservableProperty]
    public partial StageMapItemDto Ideal { get; set; } = new();

    [ObservableProperty]
    public partial StageMapItemDto Real { get; set; } = new();

    [ObservableProperty]
    public partial StageMapItemDto Error { get; set; } = new();
}