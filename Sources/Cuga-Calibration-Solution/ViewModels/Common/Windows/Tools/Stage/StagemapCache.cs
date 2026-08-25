using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Extensions;
using Net.Utilities.Models.Geometries;
using Newtonsoft.Json;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

public sealed partial class StagemapCache : ObservableCacheBase
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

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

    [ObservableProperty]
    public partial AlgorithmTemplateTypeEnum AlgorithmTemplateTypeEnum { get; set; } = AlgorithmTemplateTypeEnum.Ncc;

    [ObservableProperty]
    public partial AlgorithmTemplateSizeEnum AlgorithmTemplateSizeEnum { get; set; } = AlgorithmTemplateSizeEnum.Size256;

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial int ImageHeight { get; set; }

    [ObservableProperty]
    public partial double DiePitchSizeWidth { get; set; }

    [ObservableProperty]
    public partial double DiePitchSizeHeight { get; set; }

    [JsonIgnore]
    public Size DiePitchSize => new(DiePitchSizeWidth, DiePitchSizeHeight);

    [ObservableProperty]
    public partial double WaferDiameter { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<StagemapTemplatePoint> TemplatePoints { get; set; } = [];

    [ObservableProperty]
    public partial int RepeatCount { get; set; } = 1;

    [JsonIgnore]
    [ObservableProperty]
    public partial Stagemap Stagemap { get; set; } = new();

    [JsonIgnore]
    [ObservableProperty]
    public partial IReadOnlyList<Stagemap> RepeatedStagemaps { get; set; } = [];

    [JsonIgnore]
    [ObservableProperty]
    public partial Stagemap VerifyStagemap { get; set; } = new();
}

public sealed partial class StagemapTemplatePoint : ObservableObject, ICloneable<StagemapTemplatePoint>
{
    [ObservableProperty]
    public partial Point FindBrightFieldMachinePosition { get; set; }

    [ObservableProperty]
    public partial string BrightTemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BrightTemplateImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DarkTemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DarkTemplateImageFilePath { get; set; } = string.Empty;

    public StagemapTemplatePoint Clone() => new()
    {
        FindBrightFieldMachinePosition = FindBrightFieldMachinePosition,
        BrightTemplateFilePath = BrightTemplateFilePath,
        BrightTemplateImageFilePath = BrightTemplateImageFilePath,
        DarkTemplateFilePath = DarkTemplateFilePath,
        DarkTemplateImageFilePath = DarkTemplateImageFilePath
    };
}

public sealed class Stagemap : ICloneable<Stagemap>
{
    public Point[,] IdealPoint { get; set; } = TwoDimensionalArrayExtensions.EmptyMatrix<Point>();

    public Point[,] RealPoint { get; set; } = TwoDimensionalArrayExtensions.EmptyMatrix<Point>();

    public Point[,] ErrorPoint { get; set; } = TwoDimensionalArrayExtensions.EmptyMatrix<Point>();

    public bool[,] ValidPoint { get; set; } = TwoDimensionalArrayExtensions.EmptyMatrix<bool>();

    public int[] TemplatePointIndexes { get; set; } = [];

    public Point[,] IdealMatrix
    {
        get => IdealPoint;
        set => IdealPoint = value;
    }

    public Point[,] RealMatrix
    {
        get => RealPoint;
        set => RealPoint = value;
    }

    public Point[,] ErrorMatrix
    {
        get => ErrorPoint;
        set => ErrorPoint = value;
    }

    public bool[,] ValidMatrix
    {
        get => ValidPoint;
        set => ValidPoint = value;
    }

    public Stagemap Clone() => new()
    {
        IdealPoint = CopyPointMatrix(IdealPoint),
        RealPoint = CopyPointMatrix(RealPoint),
        ErrorPoint = CopyPointMatrix(ErrorPoint),
        ValidPoint = CopyBoolMatrix(ValidPoint),
        TemplatePointIndexes = [.. TemplatePointIndexes]
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