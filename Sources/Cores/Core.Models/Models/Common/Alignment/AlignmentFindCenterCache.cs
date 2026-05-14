using CommunityToolkit.Mvvm.ComponentModel;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Common.Alignment;

public partial class AlignmentFindCenterCache : ObservableCacheBase
{
    [ObservableProperty]
    public partial bool IsOk { get; set; }

    [ObservableProperty]
    public partial Point FindWaferCenterOffset1 { get; set; }

    [ObservableProperty]
    public partial Point FindWaferCenterOffset2 { get; set; }

    [ObservableProperty]
    public partial Point FindWaferCenterOffset3 { get; set; }

    [ObservableProperty]
    public partial Point FindWaferCenterOffset4 { get; set; }

    [ObservableProperty]
    public partial Point FindWaferCenterOffset5 { get; set; }

    [ObservableProperty]
    public partial Point FindWaferCenterOffset6 { get; set; }

    [ObservableProperty]
    public partial Point FindWaferCenterOffset7 { get; set; }

    [ObservableProperty]
    public partial Point FindWaferCenterOffset8 { get; set; }

    [ObservableProperty]
    public partial double PositionErrorThreshold { get; set; } = 300;

    [ObservableProperty]
    public partial Point OffsetPosition { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial byte[] WaferCenterThumb1 { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial byte[] WaferCenterThumb2 { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial byte[] WaferCenterThumb3 { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial byte[] WaferCenterThumb4 { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial byte[] WaferCenterThumb5 { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial byte[] WaferCenterThumb6 { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial byte[] WaferCenterThumb7 { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial byte[] WaferCenterThumb8 { get; set; } = [];
}