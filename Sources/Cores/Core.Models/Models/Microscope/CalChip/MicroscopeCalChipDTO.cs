using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Utilities;
using Core.Wcf.Models.Microscope;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Helper;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.Collections.Concurrent;
using System.ComponentModel;
using Cuga.Data.DataStruct.Microscope.Enums;

namespace Core.Models.Models.Microscope.CalChip;

public sealed partial class MicroscopeCalChipDTO : CalibrationDtoBase, ICloneable<MicroscopeCalChipDTO>, IAdaptTo<CalibrationMicroscopeCalChip>
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private double _dSWAlignmentDegree;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentItem))]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;

    [ObservableProperty]
    private IReadOnlyList<MicroscopeCalChipDTOItem> _items = [];

    public ConcurrentBag<KeyValuePair<CalChipSiteModelEnum, MicroscopeCalChipDTOItem>> Results { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]

    public MicroscopeCalChipDTOItem CurrentItem => Results.GetOrAdd(CalChipSiteModelEnum, new MicroscopeCalChipDTOItem { CalChipSiteModelEnum = CalChipSiteModelEnum });

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

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079


    public MicroscopeCalChipDTO()
    {
        ScatterPlotControl.Configure(new Columns());

        ScatterPlotControl.SetTitle(0, "Summary(Y: Quality(Score) - X: Focus(ECS))");
    }

    partial void OnItemsChanged(IReadOnlyList<MicroscopeCalChipDTOItem>? oldValue, IReadOnlyList<MicroscopeCalChipDTOItem> newValue)
    {
        foreach (var item in oldValue ?? []) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshPlot();
    }

    private void RefreshPlot()
    {
        try
        {
            ScatterPlotControl.Clear();

            var points = Items.Select(t => new Point(t.EcsValue, t.Quality)).ToList();
            ScatterPlotControl.GetOrAddScatterLine(
                0,
                "Origin",
                [.. points],
                Constants.Category10.GetColor(1));

            if (points.Count > 0)
            {
                var scatterMarkersX = ScatterPlotControl.GetOrAddScatterMarkers(
                    0,
                    "Best Focus",
                    [points.Maxima(t => t.Y).First()],
                    color: Colors.Red,
                    markerShape: MarkerShape.Asterisk);
                scatterMarkersX.MarkerSize = 25;
            }
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    #region Mapper

    public MicroscopeCalChipDTO Clone()
    {
        var cloneItems = new ConcurrentBag<KeyValuePair<CalChipSiteModelEnum, MicroscopeCalChipDTOItem>>();
        foreach (var item in Results)
        {
            cloneItems.GetOrAdd(item.Key, item.Value.Clone());
        }

        return new MicroscopeCalChipDTO
        {
            CalChipSiteModelEnum = CalChipSiteModelEnum,
            MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
            DSWAlignmentDegree = DSWAlignmentDegree,
            DSWBrightFieldMachineAffinePosition = DSWBrightFieldMachineAffinePosition,
            Results = cloneItems,
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredSelfCheck = IsRequiredSelfCheck,
            Id = Id,
            Expiration = Expiration
        };
    }

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

    public MicroscopeCalChipDTOItem Clone() => new()
    {
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        BrightFieldMachinePosition = BrightFieldMachinePosition,
        EcsValue = EcsValue,
        Quality = Quality,
        FilePath = FilePath,
    };
}