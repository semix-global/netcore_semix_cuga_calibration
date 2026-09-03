using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Helper;
using Net.Utilities.ScottPlot.Interfaces;
using ScottPlot;
using ScottPlot.MultiplotLayouts;

namespace Core.Models.Models.CIB.YPixelSize;

[CacheVersion("1.0.0")]
public sealed partial class CIBYPixelSizeDTO : CalibrationDTOBase<CIBYPixelSizeDTO>, IAdaptTo<CalibrationLaserPixelSizeItem>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial int PmtId { get; set; }

    [ObservableProperty]
    public partial Point FindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial double YPixelSize { get; set; }

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DrawImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RawFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IReadOnlyList<Point> YProjects { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IReadOnlyList<int> AlgorithmIndexes { get; set; } = [];

    partial void OnYProjectsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnAlgorithmIndexesChanged(IReadOnlyList<int> value) => RefreshPlot();

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    public CIBYPixelSizeDTO()
    {
        PlotDataSource.Configure(new Columns());
    }

    private void RefreshPlot()
    {
        try
        {
            PlotDataSource.SetTitle(0, $"PMT {PmtId}: Y Projects (Y: Gray - X: Y Pixel)");

            PlotDataSource.Clear(0);

            if (YProjects.Count != 0)
                PlotDataSource.GetOrAddScatterLine(0,
                    "Y Projects",
                    [.. YProjects],
                    Constants.Turbo.GetColor(0));

            if (AlgorithmIndexes.Count > 0)
            {
                var xLines = PlotDataSource.GetOrAddXLines(AlgorithmIndexes.Count);
                for (var i = 0; i < xLines.Count; i++)
                {
                    xLines[i].Update($"Line No.{i + 1}", AlgorithmIndexes[i], Constants.Category10.GetColor(i));
                    xLines[i].LinePattern = LinePattern.Dashed;
                }
            }
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }

    #region Mapper

    public override CIBYPixelSizeDTO Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        ProductivityInformation = ProductivityInformation.Clone(),
        PmtId = PmtId,
        FindBFMachinePosition = FindBFMachinePosition,
        YPixelSize = YPixelSize,
        YProjects = [.. YProjects],
        AlgorithmIndexes = [.. AlgorithmIndexes],
        FilePath = FilePath,
        DrawImageFilePath = DrawImageFilePath,
        RawFilePath = RawFilePath,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserPixelSizeItem AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        PmtId = PmtId,
        YPixelSize = YPixelSize,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}