using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using System.ComponentModel.DataAnnotations;

namespace Core.Models.Models.Chuck.StageMap;

public sealed partial class ChuckStageMapCache : CalibrationCacheBase
{
    private int _rowNumber = 17;
    private int _columnNumber = 21;
    private double _waferDiameter = 300_000;
    private double _columnCellWidth = 15300;
    private double _rowCellHeight = 16600;
    private int _calculateContainRowMinCount = 8;
    private int _calculateContainColumnMinCount = 8;
    private int _repeatCount = 10;

    [ObservableProperty]
    private int _brightFieldRowNumber = 17;

    [ObservableProperty]
    private int _brightFieldColumnNumber = 21;

    [ObservableProperty]
    private double _brightFieldWaferDiameter = 300_000;

    [ObservableProperty]
    private double _brightFieldColumnCellWidth = 15300;

    [ObservableProperty]
    private double _brightFieldRowCellHeight = 16600;

    [ObservableProperty]
    private int _brightFieldCalculateContainRowMinCount = 8;

    [ObservableProperty]
    private int _brightFieldCalculateContainColumnMinCount = 8;

    [ObservableProperty]
    private int _darkFieldRowNumber = 17;

    [ObservableProperty]
    private int _darkFieldColumnNumber = 21;

    [ObservableProperty]
    private double _darkFieldColumnCellWidth = 15300;

    [ObservableProperty]
    private double _darkFieldRowCellHeight = 16600;

    [ObservableProperty]
    private double _darkFieldWaferDiameter = 300_000;

    [ObservableProperty]
    private int _darkFieldCalculateContainRowMinCount = 8;

    [ObservableProperty]
    private int _darkFieldCalculateContainColumnMinCount = 8;

    [ObservableProperty]
    private Point _brightFieldFirstStageMapPosition;

    [ObservableProperty]
    private Point _darkFieldFirstStageMapPosition;

    [ObservableProperty]
    private bool _isDarkField;

    [ObservableProperty]
    private MicroscopeLensInformation _highMicroscopeLensInformation = new();

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.DieCorner_LeftTop;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum = OpticsMagTypeEnum.High;

    [ObservableProperty]
    private StageSpeedEnum _stageSpeedEnum = StageSpeedEnum.Low;

    [ObservableProperty]
    private int _xWidthPixel = 1000;

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

    [CustomValidation(typeof(ChuckStageMapCache), nameof(ValidateIsOutOfRowNumberRange))]
    [Comparison(1, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Calculate Contain Row MinCout: ")]
    public int CalculateContainRowMinCount
    {
        get => _calculateContainRowMinCount;
        set => SetProperty(ref _calculateContainRowMinCount, value, true);
    }

    [CustomValidation(typeof(ChuckStageMapCache), nameof(ValidateIsOutOfColumnNumberRange))]
    [Comparison(1, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Calculate Contain Column MinCout: ")]
    public int CalculateContainColumnMinCount
    {
        get => _calculateContainColumnMinCount;
        set => SetProperty(ref _calculateContainColumnMinCount, value, true);
    }

    [ComparisonRange(0, 10, NumberComparisonRangeTypeEnum.LeftOpenAndRightClosedInterval, ErrorMessage = "RepeatCount: ")]
    public int RepeatCount
    {
        get => _repeatCount;
        set => SetProperty(ref _repeatCount, value, true);
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
    private string _brightFieldTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _brightFieldTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _darkFieldTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _darkFieldTemplateImageFilePath = string.Empty;

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

    #region Method

    public void GetParam()
    {
        RowNumber = IsDarkField == false ? BrightFieldRowNumber : DarkFieldRowNumber;
        ColumnNumber = IsDarkField == false ? BrightFieldColumnNumber : DarkFieldColumnNumber;
        WaferDiameter = IsDarkField == false ? BrightFieldWaferDiameter : DarkFieldWaferDiameter;
        ColumnCellWidth = IsDarkField == false ? BrightFieldColumnCellWidth : DarkFieldColumnCellWidth;
        RowCellHeight = IsDarkField == false ? BrightFieldRowCellHeight : DarkFieldRowCellHeight;
        CalculateContainRowMinCount = IsDarkField == false ? BrightFieldCalculateContainRowMinCount : DarkFieldCalculateContainRowMinCount;
        CalculateContainColumnMinCount = IsDarkField == false ? BrightFieldCalculateContainColumnMinCount : DarkFieldCalculateContainColumnMinCount;
    }

    public void SetParam()
    {
        if (IsDarkField == false)
        {
            BrightFieldRowNumber = RowNumber;
            BrightFieldColumnNumber = ColumnNumber;
            BrightFieldWaferDiameter = WaferDiameter;
            BrightFieldColumnCellWidth = ColumnCellWidth;
            BrightFieldRowCellHeight = RowCellHeight;
            BrightFieldCalculateContainRowMinCount = CalculateContainRowMinCount;
            BrightFieldCalculateContainColumnMinCount = CalculateContainColumnMinCount;
        }
        else
        {
            DarkFieldRowNumber = RowNumber;
            DarkFieldColumnNumber = ColumnNumber;
            DarkFieldWaferDiameter = WaferDiameter;
            DarkFieldColumnCellWidth = ColumnCellWidth;
            DarkFieldRowCellHeight = RowCellHeight;
            DarkFieldCalculateContainRowMinCount = CalculateContainRowMinCount;
            DarkFieldCalculateContainColumnMinCount = CalculateContainColumnMinCount;
        }
    }

    #endregion

    #region Validators

    // 校验逻辑：校验是否超出rowNumber范围
    public static ValidationResult? ValidateIsOutOfRowNumberRange(int value, ValidationContext context)
    {
        var vm = (ChuckStageMapCache)context.ObjectInstance;

        return value > vm.RowNumber
            ? new ValidationResult("Out of row number range!")
            : ValidationResult.Success;
    }

    // 校验逻辑：校验是否超出columnNumber范围
    public static ValidationResult? ValidateIsOutOfColumnNumberRange(int value, ValidationContext context)
    {
        var vm = (ChuckStageMapCache)context.ObjectInstance;

        return value > vm.ColumnNumber
            ? new ValidationResult("Out of column number range!")
            : ValidationResult.Success;
    }

    #endregion Validators

    #region Verify

    public (bool IsSuccess, string ErrorMessage) Step2Verify()
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
        if (IsDarkField) ValidateProperty(RepeatCount, nameof(RepeatCount));
        ValidateProperty(CalculateContainRowMinCount, nameof(CalculateContainRowMinCount));
        ValidateProperty(CalculateContainColumnMinCount, nameof(CalculateContainColumnMinCount));

        return HasErrors ? (false, string.Join(Environment.NewLine, GetErrors())) : (true, string.Empty);
    }

    #endregion
}