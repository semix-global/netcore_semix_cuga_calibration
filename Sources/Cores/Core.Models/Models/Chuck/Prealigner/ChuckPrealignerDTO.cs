using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Chuck;
using Cuga.Data.DataStruct.Microscope.Enums;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Interfaces;
using Net.Utilities.ScottPlot.WPF.Extensions;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.ComponentModel;

namespace Core.Models.Models.Chuck.Prealigner;

[CacheVersion("1.0.0")]
public sealed partial class ChuckPrealignerDTO : CalibrationDTOBase<ChuckPrealignerDTO>, IAdaptTo<CalibrationPrealignerObj>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation LowMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial MicroscopeLensInformation HighMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IReadOnlyList<ChuckPrealignerDTOItem> Items { get; set; } = [];

    partial void OnItemsChanged(IReadOnlyList<ChuckPrealignerDTOItem> oldValue, IReadOnlyList<ChuckPrealignerDTOItem> newValue)
    {
        foreach (var item in oldValue) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshPlot();
    }

    [ObservableProperty]
    public partial ChuckPrealignerDTOItem ResultItemDto { get; set; } = new();

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    public ChuckPrealignerDTO()
    {
        var customGrid = new CustomGrid();
        PlotDataSource.Configure(customGrid, 2,
            plots =>
            {
                customGrid.Set(plots[0], new GridCell(0, 0, 1, 2));
                customGrid.Set(plots[1], new GridCell(0, 1, 1, 2));
            });

        PlotDataSource.SetTitle(0, "Center Offset(Y: Offset - X: Times )");
        PlotDataSource.SetTitle(1, "Angle(Y: Angle - X: Times)");
    }

    private void RefreshPlot()
    {
        PlotDataSource.Clear(0);
        PlotDataSource.Clear(1);

        if (Items.Count == 0) return;

        PlotDataSource.GetOrAddScatterLine(
            0,
            "Center Offset X",
            [
                .. Items.Select((t, i) =>
                    new Point
                    (
                        i,
                        t.OffsetPosition.X
                    )
                )
            ],
            0,
            new ScottPlot.Range(0, Items.Count - 1));

        PlotDataSource.GetOrAddScatterLine(
            0,
            "Center Offset Y",
            [
                .. Items.Select((t, i) =>
                    new Point
                    (
                        i,
                        t.OffsetPosition.Y
                    )
                )
            ],
            1,
            new ScottPlot.Range(0, Items.Count - 1));
        PlotDataSource.GetOrAddScatterLine(
            1,
            "Angle",
            [
                .. Items.Select((t, i) =>
                    new Point
                    (
                        i,
                        t.EfemLoadWaferChuckAbsoluteAngle
                    )
                )
            ],
            2,
            new ScottPlot.Range(0, Items.Count - 1));

        PlotDataSource.AutoScaleRefresh();
    }

    #region Mapper

    public override ChuckPrealignerDTO Clone() => new()
    {
        LowMicroscopeLensInformation = LowMicroscopeLensInformation.Clone(),
        HighMicroscopeLensInformation = HighMicroscopeLensInformation.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        ResultItemDto = ResultItemDto.Clone(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationPrealignerObj AdaptTo() => new()
    {
        CgMicroscopeLens = HighMicroscopeLensInformation != MicroscopeLensInformation.Default ? HighMicroscopeLensInformation.AdaptTo().LensCode : CgMicroscopeLens.None,
        NewEfemLoadWaferStagePosition = ResultItemDto.NewEfemLoadWaferStagePosition.ToCgPoint(),
        EfemLoadWaferChuckAbsoluteAngle = ResultItemDto.EfemLoadWaferChuckAbsoluteAngle,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

public sealed partial class ChuckPrealignerDTOItem : ObservableObject, ICloneable<ChuckPrealignerDTOItem>
{
    [ObservableProperty]
    public partial Point OffsetPosition { get; set; }

    [ObservableProperty]
    public partial Point EfemLoadWaferStagePosition { get; set; }

    [ObservableProperty]
    public partial Point NewEfemLoadWaferStagePosition { get; set; }

    [ObservableProperty]
    public partial double EfemLoadWaferChuckAbsoluteAngle { get; set; }

    public ChuckPrealignerDTOItem Clone() => new()
    {
        OffsetPosition = OffsetPosition,
        EfemLoadWaferStagePosition = EfemLoadWaferStagePosition,
        NewEfemLoadWaferStagePosition = NewEfemLoadWaferStagePosition,
        EfemLoadWaferChuckAbsoluteAngle = EfemLoadWaferChuckAbsoluteAngle
    };
}