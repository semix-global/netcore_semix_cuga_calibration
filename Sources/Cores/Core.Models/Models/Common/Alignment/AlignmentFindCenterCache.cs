using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Common.Alignment;

public partial class AlignmentFindCenterCache : ObservableCacheBase
{
    [ObservableProperty]
    private bool _isOk;

    [ObservableProperty]
    private Point _findWaferCenterOffset1;

    [ObservableProperty]
    private Point _findWaferCenterOffset2;

    [ObservableProperty]
    private Point _findWaferCenterOffset3;

    [ObservableProperty]
    private Point _findWaferCenterOffset4;

    [ObservableProperty]
    private Point _findWaferCenterOffset5;

    [ObservableProperty]
    private Point _findWaferCenterOffset6;

    [ObservableProperty]
    private Point _findWaferCenterOffset7;

    [ObservableProperty]
    private Point _findWaferCenterOffset8;

    [ObservableProperty]
    private double _positionErrorThreshold = 300;

    [ObservableProperty]
    private Point _offsetPosition;

    [property: LiteDB.BsonIgnore]
    [ObservableProperty]
    private byte[] _waferCenterThumb1 = [];

    [property: LiteDB.BsonIgnore]
    [ObservableProperty]
    private byte[] _waferCenterThumb2 = [];

    [property: LiteDB.BsonIgnore]
    [ObservableProperty]
    private byte[] _waferCenterThumb3 = [];

    [property: LiteDB.BsonIgnore]
    [ObservableProperty]
    private byte[] _waferCenterThumb4 = [];

    [property: LiteDB.BsonIgnore]
    [ObservableProperty]
    private byte[] _waferCenterThumb5 = [];

    [property: LiteDB.BsonIgnore]
    [ObservableProperty]
    private byte[] _waferCenterThumb6 = [];

    [property: LiteDB.BsonIgnore]
    [ObservableProperty]
    private byte[] _waferCenterThumb7 = [];

    [property: LiteDB.BsonIgnore]
    [ObservableProperty]
    private byte[] _waferCenterThumb8 = [];
}