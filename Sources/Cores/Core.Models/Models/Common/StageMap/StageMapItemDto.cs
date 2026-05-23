using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Common.StageMap;

public sealed partial class StageMapItemDto : ObservableObject, ICloneable<StageMapItemDto>
{
    [ObservableProperty]
    public partial int Row { get; set; }

    [ObservableProperty]
    public partial int Column { get; set; }

    [ObservableProperty]
    public partial Point Point { get; set; }

    [ObservableProperty]
    public partial bool IsInWafer { get; set; }

    [ObservableProperty]
    public partial string TemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double TemplateScore { get; set; }

    [ObservableProperty]
    public partial double TemplateAngle { get; set; }

    [ObservableProperty]
    public partial bool IsMatchOk { get; set; }

    public void Reset()
    {
        TemplateFilePath = string.Empty;
        TemplateImageFilePath = string.Empty;
        FilePath = string.Empty;
        TemplateScore = 0;
        TemplateAngle = 0;
        IsMatchOk = false;
    }

    public void Clear()
    {
        Row = 0;
        Column = 0;
        Point = Point.Origin;
        IsInWafer = false;
        Reset();
    }

    #region Mapper

    public StageMapItemDto Clone() => new()
    {
        Row = Row,
        Column = Column,
        Point = Point,
        IsInWafer = IsInWafer,
        FilePath = FilePath,
        TemplateFilePath = TemplateFilePath,
        TemplateImageFilePath = TemplateImageFilePath,
        TemplateScore = TemplateScore,
        TemplateAngle = TemplateAngle,
        IsMatchOk = IsMatchOk
    };

    #endregion Mapper
}