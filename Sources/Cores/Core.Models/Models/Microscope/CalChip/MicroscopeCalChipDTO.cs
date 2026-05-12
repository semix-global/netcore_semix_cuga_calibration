using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Helper;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.Collections.Concurrent;

namespace Core.Models.Models.Microscope.CalChip;

[CacheVersion("1.0.0")]
public sealed partial class MicroscopeCalChipDTO : CalibrationDTOBase<MicroscopeCalChipDTO>, IAdaptTo<CalibrationMicroscopeCalChip>
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private double _dSWAlignmentDegree;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentItem))]
    private CalChipSiteModelEnum _calChipSiteModelEnum;

    [Newtonsoft.Json.JsonConverter(typeof(Net.Utilities.Models.Serializations.DictionaryConverter<CalChipSiteModelEnum, MicroscopeCalChipDTOItem>))]
    public ConcurrentDictionary<CalChipSiteModelEnum, MicroscopeCalChipDTOItem> Results { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public MicroscopeCalChipDTOItem CurrentItem => Results.GetOrAdd(CalChipSiteModelEnum, _ => new MicroscopeCalChipDTOItem { CalChipSiteModelEnum = CalChipSiteModelEnum });

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public MicroscopeCalChipDTOItem DswItem => Results.Get(CalChipSiteModelEnum.DswModel);

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public MicroscopeCalChipDTOItem HazeItem => Results.Get(CalChipSiteModelEnum.HazeModel);

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public MicroscopeCalChipDTOItem ShinyWaferItem => Results.Get(CalChipSiteModelEnum.ShinyWaferModel);

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public MicroscopeCalChipDTOItem UndefineWaferItem => Results.Get(CalChipSiteModelEnum.UndefinedModel);

    [ObservableProperty]
    private Point _dSWBrightFieldMachineAffinePosition;

    #region Mapper

    public override MicroscopeCalChipDTO Clone() => new()
    {
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        DSWAlignmentDegree = DSWAlignmentDegree,
        DSWBrightFieldMachineAffinePosition = DSWBrightFieldMachineAffinePosition,
        Results = new ConcurrentDictionary<CalChipSiteModelEnum, MicroscopeCalChipDTOItem>
        ([
            .. Results.Select(r => new KeyValuePair<CalChipSiteModelEnum, MicroscopeCalChipDTOItem>(r.Key, r.Value.Clone()))
        ]),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };


    public CalibrationMicroscopeCalChip AdaptTo()
    {
        var dswItemTemp = Results.TryGet(CalChipSiteModelEnum.DswModel, out var dswItem) ? dswItem : null;
        var hazeItemTemp = Results.TryGet(CalChipSiteModelEnum.HazeModel, out var hazeItem) ? hazeItem : null;
        var shinyItemTemp = Results.TryGet(CalChipSiteModelEnum.ShinyWaferModel, out var shinyWaferItem) ? shinyWaferItem : null;
        var undefineItemTemp = Results.TryGet(CalChipSiteModelEnum.UndefinedModel, out var undefinedItem) ? undefinedItem : null;

        return new CalibrationMicroscopeCalChip
        {
            CgMicroscopeLens = MicroscopeLensInformation != MicroscopeLensInformation.Default ? MicroscopeLensInformation.AdaptTo().LensCode : CgMicroscopeLens.None,
            DSWAlignmentDegree = DSWAlignmentDegree,
            DswBrightFieldMachinePosition = dswItemTemp is null ? Point.Origin.ToCgPoint() : DSWBrightFieldMachineAffinePosition.ToCgPoint(),
            DswEcsValue = dswItemTemp?.EcsValue ?? 0d,
            UndefinedBrightFieldMachinePosition = undefineItemTemp?.BrightFieldMachinePosition.ToCgPoint() ?? Point.Origin.ToCgPoint(),
            UndefinedEcsValue = undefineItemTemp?.EcsValue ?? 0,
            HazeBrightFieldMachinePosition = hazeItemTemp?.BrightFieldMachinePosition.ToCgPoint() ?? Point.Origin.ToCgPoint(),
            HazeEcsValue = hazeItemTemp?.EcsValue ?? 0d,
            ShinyWaferBrightFieldMachinePosition = shinyItemTemp?.BrightFieldMachinePosition.ToCgPoint() ?? Point.Origin.ToCgPoint(),
            ShinyWaferEcsValue = shinyItemTemp?.EcsValue ?? 0d,
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredCalibrate = IsRequiredSelfCheck
        };
    }

    #endregion Mapper
}

public sealed partial class MicroscopeCalChipDTOItem : ObservableObject, ICloneable<MicroscopeCalChipDTOItem>
{
    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum;

    [ObservableProperty]
    private Point _brightFieldMachinePosition;

    [ObservableProperty]
    private double _ecsValue;

    [ObservableProperty]
    private double _quality;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<double> _ecs = [];

    [ObservableProperty]
    private IReadOnlyList<double> _aFError = [];

    [ObservableProperty]
    private Point[] _ecsAFErrorPoints = [];

    [ObservableProperty]
    private Point[] _ecsAFErrorMaxMins = [];

    partial void OnEcsValueChanged(double value) => RefreshPlot();

    partial void OnAFErrorChanged(IReadOnlyList<double> value) => RefreshPlot();

    partial void OnEcsAFErrorPointsChanged(Point[] value) => RefreshPlot();

    partial void OnEcsAFErrorMaxMinsChanged(Point[] value) => RefreshPlot();

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    public MicroscopeCalChipDTOItem()
    {
        ScatterPlotControl.Configure(new Columns(), 2);

        ScatterPlotControl.SetTitle(0, "Trace Buffers");
        ScatterPlotControl.SetTitle(1, "Ecs AFError Curve And Slope (Y: AF Error - X: ECS)");
    }

    private void RefreshPlot()
    {
        try
        {
            ScatterPlotControl.Clear(0);
            ScatterPlotControl.Clear(1);

            if (Ecs.Count != 0)
                ScatterPlotControl.GetOrAddScatterLine(0,
                    "ECS",
                    [.. Ecs.Select((t, i) => new Point(i, t))],
                    Constants.Category10.GetColor(0));

            if (AFError.Count != 0)
                ScatterPlotControl.GetOrAddScatterLine(0,
                    "AFError",
                    [.. AFError.Select((t, i) => new Point(i, t))],
                    Constants.Category10.GetColor(1));

            if (EcsAFErrorPoints.Length != 0)
                ScatterPlotControl.GetOrAddScatterLine(1,
                    "Ecs AfError Curve",
                    [.. EcsAFErrorPoints],
                    Constants.Category10.GetColor(1));

            if (EcsAFErrorMaxMins.Length != 0)
            {
                ScatterPlotControl.GetOrAddScatterLine(1,
                    "Ecs AfError Slope",
                    [.. EcsAFErrorMaxMins],
                    Constants.Category10.GetColor(2));
                var scatterMarkers = ScatterPlotControl.GetOrAddScatterMarkers(
                    1,
                    "AF ECS",
                    [new Point(EcsValue, 0)],
                    color: Constants.Category10.GetColor(3),
                    MarkerShape.Asterisk);
                scatterMarkers.MarkerSize = 50;
            }
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    public MicroscopeCalChipDTOItem Clone() => new()
    {
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        BrightFieldMachinePosition = BrightFieldMachinePosition,
        EcsValue = EcsValue,
        Quality = Quality,
        FilePath = FilePath,
        Ecs = [.. Ecs],
        AFError = [.. AFError],
        EcsAFErrorPoints = [.. EcsAFErrorPoints],
        EcsAFErrorMaxMins = [.. EcsAFErrorMaxMins]
    };

    public object ToFlatnessHtmlAnonymous() => new
    {
        EcsValue,
        Quality,
        Plot = new HtmlContainer([.. ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()]),
        Image = new HtmlImage(FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(false)])
    };
}