using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Common.StageMap;

public sealed partial class StageMapItemDto : ObservableObject, ICloneable<StageMapItemDto>
{
    [ObservableProperty]
    private int _row;

    [ObservableProperty]
    private int _column;

    [ObservableProperty]
    private Point _point;

    [ObservableProperty]
    private bool _isInWafer;

    [ObservableProperty]
    private string _templateFilePath = string.Empty;

    [ObservableProperty]
    private string _templateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private double _templateScore;

    [ObservableProperty]
    private double _templateAngle;

    [ObservableProperty]
    private bool _isMatchOk;

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
        IsMatchOk = IsMatchOk,
        Id = Id,
        Expiration = Expiration
    };

    #endregion Mapper
}