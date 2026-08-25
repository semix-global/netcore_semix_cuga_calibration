using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Models;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Graphics;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Interfaces;
using Newtonsoft.Json;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

public sealed partial class StageMapCache : CalibrationCacheBase
{
    #region Common

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    #endregion

    #region Step0

    [ObservableProperty]
    public partial bool IsDarkFieldAlignment { get; set; }

    #endregion

    #region Step1

    [ObservableProperty]
    public partial AlgorithmTemplateTypeEnum AlgorithmTemplateTypeEnum { get; set; } = AlgorithmTemplateTypeEnum.Ncc;

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial double DiePitchWidth { get; set; }

    [ObservableProperty]
    public partial double DiePitchHeight { get; set; }

    [ObservableProperty]
    public partial double WaferRadius { get; set; }

    [ObservableProperty]
    public partial StageMapTemplatePoint[] StageMapTemplatePoints { get; set; } = [];

    #endregion

    #region Step4

    [ObservableProperty]
    public partial int StageMapRetryCount { get; set; } = 20;

    #endregion

    #region Result

    [ObservableProperty]
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

    [JsonIgnore]
    [ObservableProperty]
    public partial CanvasDocument CanvasDocument { get; set; } = new();

    [JsonIgnore]
    [ObservableProperty]
    public partial StageMap StageMap { get; set; } = new();

    [JsonIgnore]
    [ObservableProperty]
    public partial IReadOnlyList<StageMap> RepeatStageMaps { get; set; } = [];

    [JsonIgnore]
    [ObservableProperty]
    public partial StageMap VerifyStageMap { get; set; } = new();

    #endregion

    public object ToHtmlAnonymous() => new
    {
        ProductivityInformation,
        MicroscopeLensInformation,
        LaserLightInformation,
        CIBInformation,
        OpticsConfiguration,
        CIBConfiguration,
        IsDarkFieldAlignment,
        AlgorithmTemplateTypeEnum,
        ImageWidth,
        DiePitchWidth,
        DiePitchHeight,
        WaferRadius,
        StageMapRetryCount
    };
}

public sealed partial class StageMapTemplatePoint : ObservableObject
{
    [ObservableProperty]
    public partial Point DFPosition { get; set; }

    [ObservableProperty]
    public partial string TemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateImageFilePath { get; set; } = string.Empty;
}

public sealed partial class StageMap : ObservableObject, ICloneable<StageMap>
{
    public Point[,] IdealMatrix { get; set; } = TwoDimensionalArrayExtensions.EmptyMatrix<Point>();

    public Vector[,] ErrorMatrix { get; set; } = TwoDimensionalArrayExtensions.EmptyMatrix<Vector>();

    public bool[,] ValidMatrix { get; set; } = TwoDimensionalArrayExtensions.EmptyMatrix<bool>();

    [JsonIgnore]
    [ObservableProperty]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

    public void Refresh()
    {
        var vectorFields = PlotDataSource.GetOrAddVectorFields(1);
        var heatmaps = PlotDataSource.GetOrAddHeatmaps(1);

        var vectorFieldList = new List<(Point Point, Vector Vector)>();
        var heatmapList = new List<Point3D>();

        var (rowCount, columnCount) = IdealMatrix.GetRowCountColCount();

        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                if (ValidMatrix[row, column])
                {
                    vectorFieldList.Add((IdealMatrix[row, column], ErrorMatrix[row, column]));
                    heatmapList.Add(new Point3D(IdealMatrix[row, column].X, IdealMatrix[row, column].Y, ErrorMatrix[row, column].Length));
                }
            }
        }

        heatmaps[0].Update(string.Empty, heatmapList);
        vectorFields[0].Update(string.Empty, vectorFieldList);
    }

    public StageMap Clone() => new()
    {
        IdealMatrix = CopyPointMatrix(IdealMatrix),
        ErrorMatrix = new Vector[ErrorMatrix.GetLength(0), ErrorMatrix.GetLength(1)],
        ValidMatrix = CopyBoolMatrix(ValidMatrix)
    };

    private static Point[,] CopyPointMatrix(Point[,] source)
    {
        if (source.Length == 0) return TwoDimensionalArrayExtensions.EmptyMatrix<Point>();

        var rowCount = source.GetLength(0);
        var columnCount = source.GetLength(1);
        var result = new Point[rowCount, columnCount];

        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                var point = source[row, column];
                result[row, column] = new Point(point.X, point.Y);
            }
        }

        return result;
    }

    private static bool[,] CopyBoolMatrix(bool[,] source)
    {
        if (source.Length == 0) return TwoDimensionalArrayExtensions.EmptyMatrix<bool>();

        var rowCount = source.GetLength(0);
        var columnCount = source.GetLength(1);
        var result = new bool[rowCount, columnCount];

        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                result[row, column] = source[row, column];
            }
        }

        return result;
    }
}