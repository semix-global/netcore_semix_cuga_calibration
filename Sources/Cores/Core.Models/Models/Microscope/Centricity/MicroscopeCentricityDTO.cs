using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Interfaces;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Helper;
using System.ComponentModel;

namespace Core.Models.Models.Microscope.Centricity;

[CacheVersion("1.0.1")]
public sealed partial class MicroscopeCentricityDTO : CalibrationDTOBase<MicroscopeCentricityDTO>, IAdaptTo<CalibrationMicroscopeCentricityItem>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial MicroscopeCentricityDTOItem Result { get; set; } = new();

    partial void OnResultChanged(MicroscopeCentricityDTOItem oldValue, MicroscopeCentricityDTOItem newValue) => RefreshPlot();

    [ObservableProperty]
    public partial IReadOnlyList<MicroscopeCentricityDTOItem> Items { get; set; } = [];

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    partial void OnItemsChanged(IReadOnlyList<MicroscopeCentricityDTOItem>? oldValue, IReadOnlyList<MicroscopeCentricityDTOItem> newValue)
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

    public MicroscopeCentricityDTO()
    {
        PlotDataSource.SetTitle(0, "Centricity(Y: um - X: Index)");
    }

    private void RefreshPlot()
    {
        try
        {
            PlotDataSource.Clear();

            PlotDataSource.GetOrAddScatterLine(
                0,
                "X",
                [.. Items.Select((t, i) => new Point(i, t.CentricityPosition.X))],
                Constants.Category10.GetColor(0));

            PlotDataSource.GetOrAddScatterLine(
                0,
                "Y",
                [.. Items.Select((t, i) => new Point(i, t.CentricityPosition.Y))],
                Constants.Category10.GetColor(1));
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }

    public override MicroscopeCentricityDTO Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        Result = Result.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationMicroscopeCentricityItem AdaptTo() => new()
    {
        CgMicroscopeLens = MicroscopeLensInformation != MicroscopeLensInformation.Default ? MicroscopeLensInformation.AdaptTo().LensCode : CgMicroscopeLens.None,
        Offset = Result.Offset.ToCgPoint(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };
}

public sealed partial class MicroscopeCentricityDTOItem : ObservableValidator, ICloneable<MicroscopeCentricityDTOItem>
{
    [ObservableProperty]
    public partial int Index { get; set; }

    [ObservableProperty]
    public partial Point CentricityPosition { get; set; }

    [ObservableProperty]
    public partial Point Offset { get; set; }

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    public MicroscopeCentricityDTOItem Clone() => new()
    {
        Index = Index,
        CentricityPosition = CentricityPosition,
        Offset = Offset,
        FilePath = FilePath
    };
}