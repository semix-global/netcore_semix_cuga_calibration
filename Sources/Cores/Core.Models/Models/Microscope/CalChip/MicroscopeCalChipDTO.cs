using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Helper;
using Net.Utilities.ScottPlot.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.Collections.Concurrent;

namespace Core.Models.Models.Microscope.CalChip;

[CacheVersion("1.0.0")]
public sealed partial class MicroscopeCalChipDTO : CalibrationDTOBase<MicroscopeCalChipDTO>, IAdaptTo<CalibrationMicroscopeCalChip>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial double DSWAlignmentDegree { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentItem))]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; }

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<CalChipSiteModelEnum, MicroscopeCalChipDTOItem>))]
    public ConcurrentDictionary<CalChipSiteModelEnum, MicroscopeCalChipDTOItem> Results { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public MicroscopeCalChipDTOItem CurrentItem => Results.GetOrAdd(CalChipSiteModelEnum, _ => new MicroscopeCalChipDTOItem { CalChipSiteModelEnum = CalChipSiteModelEnum });

    [Newtonsoft.Json.JsonIgnore]
    public MicroscopeCalChipDTOItem DswItem => Results.Get(CalChipSiteModelEnum.DswModel);

    [Newtonsoft.Json.JsonIgnore]
    public MicroscopeCalChipDTOItem HazeItem => Results.Get(CalChipSiteModelEnum.HazeModel);

    [Newtonsoft.Json.JsonIgnore]
    public MicroscopeCalChipDTOItem ShinyWaferItem => Results.Get(CalChipSiteModelEnum.ShinyWaferModel);

    [Newtonsoft.Json.JsonIgnore]
    public MicroscopeCalChipDTOItem UndefineWaferItem => Results.Get(CalChipSiteModelEnum.UndefinedModel);

    [ObservableProperty]
    public partial Point DSWBrightFieldMachineAffinePosition { get; set; }

    public MicroscopeCalChipDTO()
    {
        foreach (var calChipSiteModelEnum in EnumHelper.Enums<CalChipSiteModelEnum>().Where(t => t != CalChipSiteModelEnum.ChuckModel))
        {
            Results.GetOrAdd(calChipSiteModelEnum, new MicroscopeCalChipDTOItem { CalChipSiteModelEnum = calChipSiteModelEnum });
        }
    }

    public Point GetBFMachinePosition(CalChipSiteModelEnum calChipSiteModelEnum)
    {
        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();
        return calChipSiteModelEnum switch
        {
            CalChipSiteModelEnum.ChuckModel => applicationCookie.BFCenterMachinePosition,
            CalChipSiteModelEnum.DswModel => DswItem.BrightFieldMachinePosition,
            CalChipSiteModelEnum.UndefinedModel => UndefineWaferItem.BrightFieldMachinePosition,
            CalChipSiteModelEnum.HazeModel => HazeItem.BrightFieldMachinePosition,
            CalChipSiteModelEnum.ShinyWaferModel => ShinyWaferItem.BrightFieldMachinePosition,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(calChipSiteModelEnum))
        };
    }

    #region Mapper

    public override MicroscopeCalChipDTO Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        DSWAlignmentDegree = DSWAlignmentDegree,
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        DSWBrightFieldMachineAffinePosition = DSWBrightFieldMachineAffinePosition,
        Results = new ConcurrentDictionary<CalChipSiteModelEnum, MicroscopeCalChipDTOItem>(Results.Select(r => new KeyValuePair<CalChipSiteModelEnum, MicroscopeCalChipDTOItem>(r.Key, r.Value.Clone()))),
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
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; }

    [ObservableProperty]
    public partial Point BrightFieldMachinePosition { get; set; }

    [ObservableProperty]
    public partial double EcsValue { get; set; }

    [ObservableProperty]
    public partial double Quality { get; set; }

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<double> Ecs { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> AFError { get; set; } = [];

    [ObservableProperty]
    public partial Point[] EcsAFErrorPoints { get; set; } = [];

    [ObservableProperty]
    public partial Point[] EcsAFErrorMaxMins { get; set; } = [];

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnEcsValueChanged(double value) => RefreshPlot();

    partial void OnAFErrorChanged(IReadOnlyList<double> value) => RefreshPlot();

    partial void OnEcsAFErrorPointsChanged(Point[] value) => RefreshPlot();

    partial void OnEcsAFErrorMaxMinsChanged(Point[] value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    public MicroscopeCalChipDTOItem()
    {
        PlotDataSource.Configure(new Columns(), 2);

        PlotDataSource.SetTitle(0, "Trace Buffers");
        PlotDataSource.SetTitle(1, "Ecs AFError Curve And Slope (Y: AF Error - X: ECS)");
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

            if (AFError.Count != 0)
                PlotDataSource.GetOrAddScatterLine(0,
                    "AFError",
                    [.. AFError.Select((t, i) => new Point(i, t))],
                    Constants.Category10.GetColor(1));

            if (EcsAFErrorPoints.Length != 0)
                PlotDataSource.GetOrAddScatterLine(1,
                    "Ecs AfError Curve",
                    [.. EcsAFErrorPoints],
                    Constants.Category10.GetColor(1));

            if (EcsAFErrorMaxMins.Length != 0)
            {
                PlotDataSource.GetOrAddScatterLine(1,
                    "Ecs AfError Slope",
                    [.. EcsAFErrorMaxMins],
                    Constants.Category10.GetColor(2));
                var scatterMarkers = PlotDataSource.GetOrAddScatterMarkers(
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
            PlotDataSource.AutoScaleRefresh();
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
        Plot = new HtmlContainer([.. PlotDataSource.GetAllHtmlPlot2DLinesCharts()]),
        Image = new HtmlImage(FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(false)])
    };
}