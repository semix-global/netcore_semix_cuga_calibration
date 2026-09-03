using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.AutoFocus;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Interfaces;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Helper;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.Collections.Concurrent;

namespace Core.Models.Models.AutoFocus.CalChipFocusOffset;

[CacheVersion("1.0.0")]
public sealed partial class AutoFocusCalChipFocusOffsetDTO : CalibrationDTOBase<AutoFocusCalChipFocusOffsetDTO>, IAdaptTo<CalibrationAutoFocusCalChipFocusOffset>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentItem))]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; } = CalChipSiteModelEnum.ChuckModel;

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<CalChipSiteModelEnum, AutoFocusCalChipFocusOffsetDTOItem>))]
    public ConcurrentDictionary<CalChipSiteModelEnum, AutoFocusCalChipFocusOffsetDTOItem> Results { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public AutoFocusCalChipFocusOffsetDTOItem CurrentItem => Results.GetOrAdd(CalChipSiteModelEnum, _ => new AutoFocusCalChipFocusOffsetDTOItem { CalChipSiteModelEnum = CalChipSiteModelEnum });

    [Newtonsoft.Json.JsonIgnore]
    public AutoFocusCalChipFocusOffsetDTOItem ChuckItem => Results.Get(CalChipSiteModelEnum.ChuckModel);

    [Newtonsoft.Json.JsonIgnore]
    public AutoFocusCalChipFocusOffsetDTOItem DswItem => Results.Get(CalChipSiteModelEnum.DswModel);

    [Newtonsoft.Json.JsonIgnore]
    public AutoFocusCalChipFocusOffsetDTOItem HazeItem => Results.Get(CalChipSiteModelEnum.HazeModel);

    [Newtonsoft.Json.JsonIgnore]
    public AutoFocusCalChipFocusOffsetDTOItem ShinyWaferItem => Results.Get(CalChipSiteModelEnum.ShinyWaferModel);

    [Newtonsoft.Json.JsonIgnore]
    public AutoFocusCalChipFocusOffsetDTOItem UndefineWaferItem => Results.Get(CalChipSiteModelEnum.UndefinedModel);

    public AutoFocusCalChipFocusOffsetDTO()
    {
        foreach (var calChipSiteModelEnum in EnumHelper.Enums<CalChipSiteModelEnum>())
        {
            Results.GetOrAdd(calChipSiteModelEnum, new AutoFocusCalChipFocusOffsetDTOItem { CalChipSiteModelEnum = calChipSiteModelEnum });
        }
    }

    public override AutoFocusCalChipFocusOffsetDTO Clone() => new()
    {
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        ProductivityInformation = ProductivityInformation.Clone(),
        Results = new ConcurrentDictionary<CalChipSiteModelEnum, AutoFocusCalChipFocusOffsetDTOItem>(Results.Select(r => new KeyValuePair<CalChipSiteModelEnum, AutoFocusCalChipFocusOffsetDTOItem>(r.Key, r.Value.Clone()))),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationAutoFocusCalChipFocusOffset AdaptTo()
    {
        var dswItemTemp = Results.TryGet(CalChipSiteModelEnum.DswModel, out var dswItem) ? dswItem : null;
        var hazeItemTemp = Results.TryGet(CalChipSiteModelEnum.HazeModel, out var hazeItem) ? hazeItem : null;
        var shinyItemTemp = Results.TryGet(CalChipSiteModelEnum.ShinyWaferModel, out var shinyWaferItem) ? shinyWaferItem : null;
        var undefineItemTemp = Results.TryGet(CalChipSiteModelEnum.UndefinedModel, out var undefinedItem) ? undefinedItem : null;
        var chuckItemTemp = Results.TryGet(CalChipSiteModelEnum.ChuckModel, out var chuckItem) ? chuckItem : null;

        return new CalibrationAutoFocusCalChipFocusOffset
        {
            ChuckEcsValue = chuckItemTemp?.ECSValue ?? 0d,
            DswEcsValue = dswItemTemp?.ECSValue ?? 0d,
            UndefineEcsValue = undefineItemTemp?.ECSValue ?? 0d,
            HazeEcsValue = hazeItemTemp?.ECSValue ?? 0d,
            ShinyWaferEcsValue = shinyItemTemp?.ECSValue ?? 0d,
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredCalibrate = IsRequiredSelfCheck
        };
    }
}

public sealed partial class AutoFocusCalChipFocusOffsetDTOItem : ObservableValidator, ICloneable<AutoFocusCalChipFocusOffsetDTOItem>
{
    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; }

    [ObservableProperty]
    public partial double ECSValue { get; set; }

    [ObservableProperty]
    public partial double MotorValue { get; set; }

    [ObservableProperty]
    public partial double NSCRelativeZeroValue { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<double> Ecs { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> Nsc { get; set; } = [];

    [ObservableProperty]
    public partial Point[] EcsNscPoints { get; set; } = [];

    [ObservableProperty]
    public partial Point[] EcsNscMaxMins { get; set; } = [];

    partial void OnNSCRelativeZeroValueChanged(double value) => RefreshPlot();

    partial void OnECSValueChanged(double value) => RefreshPlot();

    partial void OnEcsChanged(IReadOnlyList<double> value) => RefreshPlot();

    partial void OnNscChanged(IReadOnlyList<double> value) => RefreshPlot();

    partial void OnEcsNscPointsChanged(Point[] value) => RefreshPlot();

    partial void OnEcsNscMaxMinsChanged(Point[] value) => RefreshPlot();

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    public AutoFocusCalChipFocusOffsetDTOItem()
    {
        PlotDataSource.Configure(new Rows(), 2);

        PlotDataSource.SetTitle(0, "Trace Buffers");
        PlotDataSource.SetTitle(1, "Ecs Nsc Curve And Slope (Y: NSC - X: ECS)");
    }

    private void RefreshPlot()
    {
        try
        {
            PlotDataSource.Clear(0);
            PlotDataSource.Clear(1);

            if (Ecs.Count != 0)
                PlotDataSource.GetOrAddScatterLine(0,
                    "ECS",
                    [.. Ecs.Select((t, i) => new Point(i, t))],
                    Constants.Category10.GetColor(0));

            if (Nsc.Count != 0)
                PlotDataSource.GetOrAddScatterLine(0,
                    "NSC",
                    [.. Nsc.Select((t, i) => new Point(i, t))],
                    Constants.Category10.GetColor(1));

            if (EcsNscPoints.Length != 0)
                PlotDataSource.GetOrAddScatterLine(1,
                    "Ecs Nsc Curve",
                    [.. EcsNscPoints],
                    Constants.Category10.GetColor(1));

            if (EcsNscMaxMins.Length != 0)
            {
                PlotDataSource.GetOrAddScatterLine(1,
                    "Ecs Nsc Slope",
                    [.. EcsNscMaxMins],
                    Constants.Category10.GetColor(2));
                var scatterMarkers = PlotDataSource.GetOrAddScatterMarkers(
                    1,
                    "AF ECS",
                    [new Point(ECSValue, 0)],
                    color: Constants.Category10.GetColor(3),
                    MarkerShape.Asterisk);
                scatterMarkers.MarkerSize = 50;
            }

            var yLines = PlotDataSource.GetOrAddYLines(0, 1);
            yLines[0].Update("NSC Relative Zero Value", NSCRelativeZeroValue, Colors.OrangeRed);
            yLines[0].LinePattern = LinePattern.Dashed;
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }

    public AutoFocusCalChipFocusOffsetDTOItem Clone() => new()
    {
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        ECSValue = ECSValue,
        MotorValue = MotorValue,
        NSCRelativeZeroValue = NSCRelativeZeroValue,
        Ecs = [.. Ecs],
        Nsc = [.. Nsc],
        EcsNscPoints = [.. EcsNscPoints],
        EcsNscMaxMins = [.. EcsNscMaxMins]
    };

    public object ToFlatnessHtmlAnonymous() => new
    {
        NSCRelativeZeroValue,
        ECSValue,
        MotorValue,
        Plot = new HtmlContainer([.. PlotDataSource.GetAllHtmlPlot2DLinesCharts()])
    };
}