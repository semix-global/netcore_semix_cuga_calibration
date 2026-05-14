using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using System.ComponentModel.DataAnnotations;

namespace Core.Models.Models.Chuck.StageMap;

public sealed partial class ChuckStageMapCache : CalibrationCacheBase<ChuckStageMapCache>
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial OpticsIlluminationModeEnum OpticsIlluminationModeEnum { get; set; } = CalibrationConstantsHelper.MainOpticsIlluminationModeEnum;

    [ObservableProperty]
    public partial int BrightFieldRowNumber { get; set; } = 17;

    [ObservableProperty]
    public partial int BrightFieldColumnNumber { get; set; } = 21;

    [ObservableProperty]
    public partial double BrightFieldWaferDiameter { get; set; } = 300_000;

    [ObservableProperty]
    public partial double BrightFieldColumnCellWidth { get; set; } = 15300;

    [ObservableProperty]
    public partial double BrightFieldRowCellHeight { get; set; } = 16600;

    [ObservableProperty]
    public partial int BrightFieldCalculateContainRowMinCount { get; set; } = 8;

    [ObservableProperty]
    public partial int BrightFieldCalculateContainColumnMinCount { get; set; } = 8;

    [ObservableProperty]
    public partial int DarkFieldRowNumber { get; set; } = 17;

    [ObservableProperty]
    public partial int DarkFieldColumnNumber { get; set; } = 21;

    [ObservableProperty]
    public partial double DarkFieldColumnCellWidth { get; set; } = 15300;

    [ObservableProperty]
    public partial double DarkFieldRowCellHeight { get; set; } = 16600;

    [ObservableProperty]
    public partial double DarkFieldWaferDiameter { get; set; } = 300_000;

    [ObservableProperty]
    public partial int DarkFieldCalculateContainRowMinCount { get; set; } = 8;

    [ObservableProperty]
    public partial int DarkFieldCalculateContainColumnMinCount { get; set; } = 8;

    [ObservableProperty]
    public partial Point BrightFieldFirstStageMapPosition { get; set; }

    [ObservableProperty]
    public partial Point DarkFieldFirstStageMapPosition { get; set; }

    [ObservableProperty]
    public partial bool IsDarkField { get; set; }

    [ObservableProperty]
    public partial MicroscopeLensInformation HighMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial WaferMaskTypeEnum WaferMaskTypeEnum { get; set; } = WaferMaskTypeEnum.DieCorner_LeftTop;

    [ObservableProperty]
    public partial int XWidthPixel { get; set; } = 1000;

    [Comparison(1, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Row Number: ")]
    public int RowNumber
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 17;

    [Comparison(1, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Column Number: ")]
    public int ColumnNumber
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 21;

    [Comparison(0.1d, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Column Cell Width must be greater than 0.1.")]
    public double ColumnCellWidth
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 15300;

    [Comparison(0.1d, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Row Cell Height must be greater than 0.1.")]
    public double RowCellHeight
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 16600;

    [Comparison(1000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Chuck Diameter: ")]
    public double WaferDiameter
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 300_000;

    [CustomValidation(typeof(ChuckStageMapCache), nameof(ValidateIsOutOfRowNumberRange))]
    [Comparison(1, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Calculate Contain Row MinCout: ")]
    public int CalculateContainRowMinCount
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 8;

    [CustomValidation(typeof(ChuckStageMapCache), nameof(ValidateIsOutOfColumnNumberRange))]
    [Comparison(1, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Calculate Contain Column MinCout: ")]
    public int CalculateContainColumnMinCount
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 8;

    [ComparisonRange(0, 10, NumberComparisonRangeTypeEnum.LeftOpenAndRightClosedInterval, ErrorMessage = "RepeatCount: ")]
    public int RepeatCount
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 10;

    [ObservableProperty]
    public partial double P5Angle { get; set; }

    [ObservableProperty]
    public partial Point FirstStageMapPosition { get; set; }

    [ObservableProperty]
    public partial string TemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BrightFieldTemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BrightFieldTemplateImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DarkFieldTemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DarkFieldTemplateImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double CalibrationAlignmentThreshold { get; set; } = 1.466d;

    [ObservableProperty]
    public partial double CalibrationGantryThreshold { get; set; } = 5d;

    [ObservableProperty]
    public partial double CalibrationScaleThreshold { get; set; } = 5d;

    [ObservableProperty]
    public partial double VerifyAlignmentThreshold { get; set; } = 1.466d;

    [ObservableProperty]
    public partial double VerifyGantryThreshold { get; set; } = 1.466d;

    [ObservableProperty]
    public partial double VerifyScaleThreshold { get; set; } = 1.466d;

    [ObservableProperty]
    public partial Point Threshold { get; set; }

    [ObservableProperty]
    private bool _isDarkFieldAlignment;

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

    #endregion Method

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

    #endregion Verify

    public override ChuckStageMapCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        OpticsIlluminationModeEnum = OpticsIlluminationModeEnum,
        BrightFieldRowNumber = BrightFieldRowNumber,
        BrightFieldColumnNumber = BrightFieldColumnNumber,
        BrightFieldWaferDiameter = BrightFieldWaferDiameter,
        BrightFieldColumnCellWidth = BrightFieldColumnCellWidth,
        BrightFieldRowCellHeight = BrightFieldRowCellHeight,
        BrightFieldCalculateContainRowMinCount = BrightFieldCalculateContainRowMinCount,
        BrightFieldCalculateContainColumnMinCount = BrightFieldCalculateContainColumnMinCount,
        DarkFieldRowNumber = DarkFieldRowNumber,
        DarkFieldColumnNumber = DarkFieldColumnNumber,
        DarkFieldColumnCellWidth = DarkFieldColumnCellWidth,
        DarkFieldRowCellHeight = DarkFieldRowCellHeight,
        DarkFieldWaferDiameter = DarkFieldWaferDiameter,
        DarkFieldCalculateContainRowMinCount = DarkFieldCalculateContainRowMinCount,
        DarkFieldCalculateContainColumnMinCount = DarkFieldCalculateContainColumnMinCount,
        BrightFieldFirstStageMapPosition = BrightFieldFirstStageMapPosition,
        DarkFieldFirstStageMapPosition = DarkFieldFirstStageMapPosition,
        IsDarkField = IsDarkField,
        HighMicroscopeLensInformation = HighMicroscopeLensInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        CIBConfiguration = CIBConfiguration.Clone(),
        WaferMaskTypeEnum = WaferMaskTypeEnum,
        XWidthPixel = XWidthPixel,
        RowNumber = RowNumber,
        ColumnNumber = ColumnNumber,
        WaferDiameter = WaferDiameter,
        ColumnCellWidth = ColumnCellWidth,
        RowCellHeight = RowCellHeight,
        CalculateContainRowMinCount = CalculateContainRowMinCount,
        CalculateContainColumnMinCount = CalculateContainColumnMinCount,
        RepeatCount = RepeatCount,
        P5Angle = P5Angle,
        FirstStageMapPosition = FirstStageMapPosition,
        TemplateFilePath = TemplateFilePath,
        TemplateImageFilePath = TemplateImageFilePath,
        BrightFieldTemplateFilePath = BrightFieldTemplateFilePath,
        BrightFieldTemplateImageFilePath = BrightFieldTemplateImageFilePath,
        DarkFieldTemplateFilePath = DarkFieldTemplateFilePath,
        DarkFieldTemplateImageFilePath = DarkFieldTemplateImageFilePath,
        CalibrationAlignmentThreshold = CalibrationAlignmentThreshold,
        CalibrationGantryThreshold = CalibrationGantryThreshold,
        CalibrationScaleThreshold = CalibrationScaleThreshold,
        VerifyAlignmentThreshold = VerifyAlignmentThreshold,
        VerifyGantryThreshold = VerifyGantryThreshold,
        VerifyScaleThreshold = VerifyScaleThreshold,
        Threshold = Threshold,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };
}