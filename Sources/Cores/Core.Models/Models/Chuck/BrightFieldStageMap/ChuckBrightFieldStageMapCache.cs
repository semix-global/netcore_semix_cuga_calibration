using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using System.ComponentModel.DataAnnotations;

namespace Core.Models.Models.Chuck.BrightFieldStageMap;

public sealed partial class ChuckBrightFieldStageMapCache : CalibrationCacheBase
{
    private int _rowNumber = 17;
    private int _columnNumber = 21;
    private double _waferDiameter = 300_000;
    private double _columnCellWidth = 15300;
    private double _rowCellHeight = 16600;
    private int _calculateContainRowMinCount = 8;
    private int _calculateContainColumnMinCount = 8;

    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = new();

    [Comparison(1, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Row Number: ")]
    public int RowNumber
    {
        get => _rowNumber;
        set => SetProperty(ref _rowNumber, value, true);
    }

    [Comparison(1, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Column Number: ")]
    public int ColumnNumber
    {
        get => _columnNumber;
        set => SetProperty(ref _columnNumber, value, true);
    }

    [Comparison(0.1d, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Column Cell Width must be greater than 0.1.")]
    public double ColumnCellWidth
    {
        get => _columnCellWidth;
        set => SetProperty(ref _columnCellWidth, value, true);
    }

    [Comparison(0.1d, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Row Cell Height must be greater than 0.1.")]
    public double RowCellHeight
    {
        get => _rowCellHeight;
        set => SetProperty(ref _rowCellHeight, value, true);
    }

    [Comparison(1000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Chuck Diameter: ")]
    public double WaferDiameter
    {
        get => _waferDiameter;
        set => SetProperty(ref _waferDiameter, value, true);
    }

    [CustomValidation(typeof(ChuckBrightFieldStageMapCache), nameof(ValidateIsOutOfRowNumberRange))]
    [Comparison(1, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Calculate Contain Row MinCout: ")]
    public int CalculateContainRowMinCount
    {
        get => _calculateContainRowMinCount;
        set => SetProperty(ref _calculateContainRowMinCount, value, true);
    }

    [CustomValidation(typeof(ChuckBrightFieldStageMapCache), nameof(ValidateIsOutOfColumnNumberRange))]
    [Comparison(1, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Calculate Contain Column MinCout: ")]
    public int CalculateContainColumnMinCount
    {
        get => _calculateContainColumnMinCount;
        set => SetProperty(ref _calculateContainColumnMinCount, value, true);
    }

    [ObservableProperty]
    private double _p5Angle;

    [ObservableProperty]
    private Point _firstStageMapPosition;

    [ObservableProperty]
    private string _templateFilePath = string.Empty;

    [ObservableProperty]
    private string _templateImageFilePath = string.Empty;

    [ObservableProperty]
    private double _calibrationAlignmentThreshold = 1.466d;

    [ObservableProperty]
    private double _calibrationGantryThreshold = 5d;

    [ObservableProperty]
    private double _calibrationScaleThreshold = 5d;

    [ObservableProperty]
    private double _verifyAlignmentThreshold = 1.466d;

    [ObservableProperty]
    private double _verifyGantryThreshold = 1.466d;

    [ObservableProperty]
    private double _verifyScaleThreshold = 1.466d;

    [ObservableProperty]
    private Point _threshold;

    #region Validators

    // 校验逻辑：校验是否超出rowNumber范围
    public static ValidationResult? ValidateIsOutOfRowNumberRange(int value, ValidationContext context)
    {
        var vm = (ChuckBrightFieldStageMapCache)context.ObjectInstance;

        return value > vm.RowNumber
            ? new ValidationResult("Out of row number range!")
            : ValidationResult.Success;
    }

    // 校验逻辑：校验是否超出columnNumber范围
    public static ValidationResult? ValidateIsOutOfColumnNumberRange(int value, ValidationContext context)
    {
        var vm = (ChuckBrightFieldStageMapCache)context.ObjectInstance;

        return value > vm.ColumnNumber
            ? new ValidationResult("Out of column number range!")
            : ValidationResult.Success;
    }

    #endregion Validators

    #region Verify

    public (bool IsSuccess, string ErrorMessage) Step1Verify()
    {
        ClearErrors();
        ValidateProperty(RowNumber, nameof(RowNumber));
        ValidateProperty(ColumnNumber, nameof(ColumnNumber));
        ValidateProperty(WaferDiameter, nameof(WaferDiameter));
        ValidateProperty(RowCellHeight, nameof(RowCellHeight));
        ValidateProperty(ColumnCellWidth, nameof(ColumnCellWidth));

        return HasErrors ? (false, string.Join(Environment.NewLine, GetErrors())) : (true, string.Empty);
    }

    public (bool IsSuccess, string ErrorMessage) Step3Verify()
    {
        ClearErrors();
        ValidateProperty(CalculateContainRowMinCount, nameof(CalculateContainRowMinCount));
        ValidateProperty(CalculateContainColumnMinCount, nameof(CalculateContainColumnMinCount));

        return HasErrors ? (false, string.Join(Environment.NewLine, GetErrors())) : (true, string.Empty);
    }

    #endregion Verify
}