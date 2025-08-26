using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.Center;

public sealed partial class ChuckCenterCacheItem : ObservableCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _lensInformation = new();

    [ObservableProperty]
    private Point _topPosition = new(150, 0);

    [ObservableProperty]
    private Point _leftPosition = new(0, 150);

    [ObservableProperty]
    private Point _bottomPosition = new(-150, 0);

    [ObservableProperty]
    private Point _rightPosition = new(0, -150);

    [ObservableProperty]
    private string _topTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _topTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _bottomTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _bottomTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _leftTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _leftTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _rightTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _rightTemplateImageFilePath = string.Empty;
}