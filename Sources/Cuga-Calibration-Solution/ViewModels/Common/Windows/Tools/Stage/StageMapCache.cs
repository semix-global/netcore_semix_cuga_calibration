using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Graphics;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Extensions;
using Net.Utilities.Models.Geometries;
using Newtonsoft.Json;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

public sealed partial class StageMapCache : ObservableCacheBase
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
    
    [ObservableProperty]
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

    #endregion

    #region Step1

    [ObservableProperty]
    public partial AlgorithmTemplateTypeEnum AlgorithmTemplateTypeEnum { get; set; } = AlgorithmTemplateTypeEnum.Ncc;

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [JsonIgnore]
    [ObservableProperty]
    public partial CanvasDocument CanvasDocument { get; set; } = new();

    [ObservableProperty]
    public partial double DiePitchWidth { get; set; }

    [ObservableProperty]
    public partial double DiePitchHeight { get; set; }

    [ObservableProperty]
    public partial double WaferRadius { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<StageMapTemplatePoint> TemplatePoints { get; set; } = [];

    #endregion

    public object ToHtmlAnonymous() => new
    {
        ProductivityInformation,
        MicroscopeLensInformation,
        LaserLightInformation,
        CIBInformation,
        OpticsConfiguration,
        CIBConfiguration,
        AlgorithmTemplateTypeEnum,
        ImageWidth,
        DiePitchWidth,
        DiePitchHeight,
        WaferRadius
    };
}

public sealed partial class StageMapTemplatePoint : ObservableObject
{
    [ObservableProperty]
    public partial Point FindBrightFieldMachinePosition { get; set; }

    [ObservableProperty]
    public partial string DarkTemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DarkTemplateImageFilePath { get; set; } = string.Empty;
}

public sealed class StageMap : ICloneable<StageMap>
{
    public Point[,] IdealMatrix { get; set; } = TwoDimensionalArrayExtensions.EmptyMatrix<Point>();

    public Point[,] RealMatrix { get; set; } = TwoDimensionalArrayExtensions.EmptyMatrix<Point>();

    public Point[,] ErrorMatrix { get; set; } = TwoDimensionalArrayExtensions.EmptyMatrix<Point>();

    public bool[,] ValidMatrix { get; set; } = TwoDimensionalArrayExtensions.EmptyMatrix<bool>();

    public StageMap Clone() => new()
    {
        IdealMatrix = CopyPointMatrix(IdealMatrix),
        RealMatrix = CopyPointMatrix(RealMatrix),
        ErrorMatrix = CopyPointMatrix(ErrorMatrix),
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