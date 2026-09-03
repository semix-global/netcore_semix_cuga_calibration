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

namespace Core.Models.Models.Microscope.PixelSize;

[CacheVersion("1.0.1")]
public sealed partial class MicroscopePixelSizeDTO : CalibrationDTOBase<MicroscopePixelSizeDTO>, IAdaptTo<CalibrationMicroscopePixelSizeItem>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial MicroscopePixelSizeDTOItem Result { get; set; } = new();

    partial void OnResultChanged(MicroscopePixelSizeDTOItem oldValue, MicroscopePixelSizeDTOItem newValue) => RefreshPlot();

    [ObservableProperty]
    public partial IReadOnlyList<MicroscopePixelSizeDTOItem> Items { get; set; } = [];

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    partial void OnItemsChanged(IReadOnlyList<MicroscopePixelSizeDTOItem>? oldValue, IReadOnlyList<MicroscopePixelSizeDTOItem> newValue)
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

    public MicroscopePixelSizeDTO()
    {
        PlotDataSource.SetTitle(0, "PixelSize(Y: um - X: Index)");
    }

    private void RefreshPlot()
    {
        try
        {
            PlotDataSource.Clear();

            PlotDataSource.GetOrAddScatterLine(
                0,
                "Width",
                [.. Items.Select((t, i) => new Point(i, t.PixelSize.Width))],
                Constants.Category10.GetColor(0));

            PlotDataSource.GetOrAddScatterLine(
                0,
                "Height",
                [.. Items.Select((t, i) => new Point(i, t.PixelSize.Height))],
                Constants.Category10.GetColor(1));
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }

    #region Mapper

    public override MicroscopePixelSizeDTO Clone() => new()
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

    public CalibrationMicroscopePixelSizeItem AdaptTo() => new()
    {
        CgMicroscopeLens = MicroscopeLensInformation != MicroscopeLensInformation.Default ? MicroscopeLensInformation.AdaptTo().LensCode : CgMicroscopeLens.None,
        PixelSize = Result.PixelSize.ToCgSize(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

public sealed partial class MicroscopePixelSizeDTOItem : ObservableValidator, ICloneable<MicroscopePixelSizeDTOItem>
{
    [ObservableProperty]
    public partial int Index { get; set; }

    [ObservableProperty]
    public partial Size PixelSize { get; set; }

    [ObservableProperty]
    public partial string OriginFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    public MicroscopePixelSizeDTOItem Clone() => new()
    {
        Index = Index,
        PixelSize = PixelSize,
        OriginFilePath = OriginFilePath,
        FilePath = FilePath
    };
}