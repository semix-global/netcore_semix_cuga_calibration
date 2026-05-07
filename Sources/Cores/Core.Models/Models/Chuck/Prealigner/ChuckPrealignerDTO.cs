using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Chuck;
using Cuga.Data.DataStruct.Microscope.Enums;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.ComponentModel;

namespace Core.Models.Models.Chuck.Prealigner;

[CacheVersion("1.0.0")]
public sealed partial class ChuckPrealignerDTO : CalibrationDTOBase, ICloneable<ChuckPrealignerDTO>, IAdaptTo<CalibrationPrealignerObj>
{
    [ObservableProperty]
    private MicroscopeLensInformation _lowMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _highMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IReadOnlyList<ChuckPrealignerDTOItem> _items = [];

    partial void OnItemsChanged(IReadOnlyList<ChuckPrealignerDTOItem>? oldValue, IReadOnlyList<ChuckPrealignerDTOItem> newValue)
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

    [ObservableProperty]
    private ChuckPrealignerDTOItem _resultItemDto = new();

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    public ChuckPrealignerDTO()
    {
        var customGrid = new CustomGrid();
        ScatterPlotControl.Configure(customGrid, 2,
            plots =>
            {
                customGrid.Set(plots[0], new GridCell(0, 0, 1, 2));
                customGrid.Set(plots[1], new GridCell(0, 1, 1, 2));
            });

        ScatterPlotControl.SetTitle(0, "Center Offset(Y: Offset - X: Times )");
        ScatterPlotControl.SetTitle(1, "Angle(Y: Angle - X: Times)");
    }

    private void RefreshPlot()
    {
        ScatterPlotControl.Clear(0);
        ScatterPlotControl.Clear(1);

        if (Items.Count == 0) return;

        ScatterPlotControl.GetOrAddScatterLine(
            0,
            "Center Offset X",
            [
                ..Items.Select((t, i) =>
                    new Point
                    (
                        i,
                        t.OffsetPosition.X
                    )
                )
            ],
            0,
            new ScottPlot.Range(0, Items.Count - 1));

        ScatterPlotControl.GetOrAddScatterLine(
            0,
            "Center Offset Y",
            [
                ..Items.Select((t, i) =>
                    new Point
                    (
                        i,
                        t.OffsetPosition.Y
                    )
                )
            ],
            1,
            new ScottPlot.Range(0, Items.Count - 1));
        ScatterPlotControl.GetOrAddScatterLine(
            1,
            "Angle",
            [
                ..Items.Select((t, i) =>
                    new Point
                    (
                        i,
                        t.EfemLoadWaferChuckAbsoluteAngle
                    )
                )
            ],
            2,
            new ScottPlot.Range(0, Items.Count - 1));

        ScatterPlotControl.AutoScaleRefresh();
    }

    #region Mapper

    public ChuckPrealignerDTO Clone() => new()
    {
        LowMicroscopeLensInformation = LowMicroscopeLensInformation,
        HighMicroscopeLensInformation = HighMicroscopeLensInformation,
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
    private Point _offsetPosition;

    [ObservableProperty]
    private Point _efemLoadWaferStagePosition;

    [ObservableProperty]
    private Point _newEfemLoadWaferStagePosition;

    [ObservableProperty]
    private double _efemLoadWaferChuckAbsoluteAngle;

    public ChuckPrealignerDTOItem Clone() => new()
    {
        OffsetPosition = OffsetPosition,
        EfemLoadWaferStagePosition = EfemLoadWaferStagePosition,
        NewEfemLoadWaferStagePosition = NewEfemLoadWaferStagePosition,
        EfemLoadWaferChuckAbsoluteAngle = EfemLoadWaferChuckAbsoluteAngle
    };
}