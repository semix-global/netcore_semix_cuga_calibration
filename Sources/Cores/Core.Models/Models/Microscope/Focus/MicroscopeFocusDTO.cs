using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Helper;
using Net.Utilities.ScottPlot.Interfaces;
using ScottPlot;
using System.ComponentModel;

namespace Core.Models.Models.Microscope.Focus;

[CacheVersion("1.0.1")]
public sealed partial class MicroscopeFocusDTO : CalibrationDTOBase<MicroscopeFocusDTO>, IAdaptTo<CalibrationMicroscopeFocusItem>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation LensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial MicroscopeFocusDTOItem Result { get; set; } = new();

    partial void OnResultChanged(MicroscopeFocusDTOItem oldValue, MicroscopeFocusDTOItem newValue) => RefreshPlot();

    [ObservableProperty]
    public partial IReadOnlyList<MicroscopeFocusDTOItem> Items { get; set; } = [];

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    partial void OnItemsChanged(IReadOnlyList<MicroscopeFocusDTOItem>? oldValue, IReadOnlyList<MicroscopeFocusDTOItem> newValue)
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

    public MicroscopeFocusDTO()
    {
        PlotDataSource.SetTitle(0, "Focus(Y: Quality - X: ECS)");
    }

    private void RefreshPlot()
    {
        try
        {
            PlotDataSource.Clear();

            PlotDataSource.GetOrAddScatterLine(
                0,
                $"Focus",
                [.. Items.Select(t => new Point(t.EcsValue, t.Quality))],
                Constants.Category10.GetColor(0));

            if (Result.EcsValue != 0 && Result.Quality != 0)
            {
                var scatterMarkers = PlotDataSource.GetOrAddScatterMarkers(
                    0,
                    "Result",
                    [new Point(Result.EcsValue, Result.Quality)],
                    color: Constants.Category10.GetColor(1),
                    MarkerShape.Asterisk);
                scatterMarkers.MarkerSize = 50;
            }
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }

    #region Mapper

    public override MicroscopeFocusDTO Clone() => new()
    {
        LensInformation = LensInformation.Clone(),
        Result = Result.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationMicroscopeFocusItem AdaptTo() => new()
    {
        CgMicroscopeLens = LensInformation != MicroscopeLensInformation.Default ? LensInformation.AdaptTo().LensCode : CgMicroscopeLens.None,
        EcsValue = Result.EcsValue,
        MicroscopeVoltage = Result.MicroscopeVoltage,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

public sealed partial class MicroscopeFocusDTOItem : ObservableValidator, ICloneable<MicroscopeFocusDTOItem>
{
    [ObservableProperty]
    public partial int Index { get; set; }

    [ObservableProperty]
    public partial double EcsValue { get; set; }

    [ObservableProperty]
    public partial double Quality { get; set; }

    [ObservableProperty]
    public partial double TransBufferAfErrorValue { get; set; }

    [ObservableProperty]
    public partial double MicroscopeVoltage { get; set; }

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    #region Mapper

    public MicroscopeFocusDTOItem Clone() => new()
    {
        Index = Index,
        EcsValue = EcsValue,
        Quality = Quality,
        TransBufferAfErrorValue = TransBufferAfErrorValue,
        MicroscopeVoltage = MicroscopeVoltage,
        FilePath = FilePath,
    };

    #endregion Mapper
}