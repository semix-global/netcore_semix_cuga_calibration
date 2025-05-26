using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Wcf.Models.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Mapper;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;

namespace Core.Models.Models.Microscope.CalChip;

public sealed partial class MicroscopeCalChipDto : CalibrationDtoBase, ICloneable<MicroscopeCalChipDto>, IAdaptTo<CalibrationMicroscopeCalChip>
{
    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum;

    [ObservableProperty]
    private Point _dswPosition;

    [ObservableProperty]
    private Point _dswBrightFieldMachinePosition;

    [ObservableProperty]
    private Point _dswDarkFieldMachinePosition;

    [ObservableProperty]
    private Point _undefinedPosition;

    [ObservableProperty]
    private Point _undefinedBrightFieldMachinePosition;

    [ObservableProperty]
    private Point _undefinedDarkFieldMachinePosition;

    [ObservableProperty]
    private Point _hazePosition;

    [ObservableProperty]
    private Point _hazeBrightFieldMachinePosition;

    [ObservableProperty]
    private Point _hazeDarkFieldMachinePosition;

    [ObservableProperty]
    private Point _shinyWaferPosition;

    [ObservableProperty]
    private Point _shinyWaferBrightFieldMachinePosition;

    [ObservableProperty]
    private Point _shinyWaferDarkFieldMachinePosition;

    [ObservableProperty]
    private double _dswEcsValue;

    [ObservableProperty]
    private double _undefinedEcsValue;

    [ObservableProperty]
    private double _hazeEcsValue;

    [ObservableProperty]
    private double _shinyWaferEcsValue;

    [ObservableProperty]
    private double _dswQuality;

    [ObservableProperty]
    private double _undefinedQuality;

    [ObservableProperty]
    private double _hazeQuality;

    [ObservableProperty]
    private double _shinyWaferQuality;

    [ObservableProperty]
    private string _dswFilePath = string.Empty;

    [ObservableProperty]
    private string _undefinedFilePath = string.Empty;

    [ObservableProperty]
    private string _hazeFilePath = string.Empty;

    [ObservableProperty]
    private string _shinyWaferFilePath = string.Empty;

    public Point GetFindFocusPosition(CalChipSiteModelEnum calChipSiteModelEnum) => calChipSiteModelEnum switch
    {
        CalChipSiteModelEnum.DswModel => DswPosition,
        CalChipSiteModelEnum.UndefinedModel => UndefinedPosition,
        CalChipSiteModelEnum.HazeModel => HazePosition,
        CalChipSiteModelEnum.ShinyWaferModel => ShinyWaferPosition,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(calChipSiteModelEnum))
    };

    public Point GetBrightFieldMachinePosition(CalChipSiteModelEnum calChipSiteModelEnum) => calChipSiteModelEnum switch
    {
        CalChipSiteModelEnum.DswModel => DswBrightFieldMachinePosition,
        CalChipSiteModelEnum.UndefinedModel => UndefinedBrightFieldMachinePosition,
        CalChipSiteModelEnum.HazeModel => HazeBrightFieldMachinePosition,
        CalChipSiteModelEnum.ShinyWaferModel => ShinyWaferBrightFieldMachinePosition,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(calChipSiteModelEnum))
    };

    public Point GetDarkFieldMachinePosition(CalChipSiteModelEnum calChipSiteModelEnum) => calChipSiteModelEnum switch
    {
        CalChipSiteModelEnum.DswModel => DswDarkFieldMachinePosition,
        CalChipSiteModelEnum.UndefinedModel => UndefinedDarkFieldMachinePosition,
        CalChipSiteModelEnum.HazeModel => HazeDarkFieldMachinePosition,
        CalChipSiteModelEnum.ShinyWaferModel => ShinyWaferDarkFieldMachinePosition,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(calChipSiteModelEnum))
    };

    public double GetEcsValue(CalChipSiteModelEnum calChipSiteModelEnum) => calChipSiteModelEnum switch
    {
        CalChipSiteModelEnum.DswModel => DswEcsValue,
        CalChipSiteModelEnum.UndefinedModel => UndefinedEcsValue,
        CalChipSiteModelEnum.HazeModel => HazeEcsValue,
        CalChipSiteModelEnum.ShinyWaferModel => ShinyWaferEcsValue,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(calChipSiteModelEnum))
    };

    public double GetQuality(CalChipSiteModelEnum calChipSiteModelEnum) => calChipSiteModelEnum switch
    {
        CalChipSiteModelEnum.DswModel => DswQuality,
        CalChipSiteModelEnum.UndefinedModel => UndefinedQuality,
        CalChipSiteModelEnum.HazeModel => HazeQuality,
        CalChipSiteModelEnum.ShinyWaferModel => ShinyWaferQuality,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(calChipSiteModelEnum))
    };

    public string GetFilePath(CalChipSiteModelEnum calChipSiteModelEnum) => calChipSiteModelEnum switch
    {
        CalChipSiteModelEnum.DswModel => DswFilePath,
        CalChipSiteModelEnum.UndefinedModel => UndefinedFilePath,
        CalChipSiteModelEnum.HazeModel => HazeFilePath,
        CalChipSiteModelEnum.ShinyWaferModel => ShinyWaferFilePath,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<string>(nameof(calChipSiteModelEnum))
    };

    public void SetEcsValue(CalChipSiteModelEnum calChipSiteModelEnum, double value)
    {
        switch (calChipSiteModelEnum)
        {
            case CalChipSiteModelEnum.DswModel:
                DswEcsValue = value;
                break;

            case CalChipSiteModelEnum.UndefinedModel:
                UndefinedEcsValue = value;
                break;

            case CalChipSiteModelEnum.HazeModel:
                HazeEcsValue = value;
                break;

            case CalChipSiteModelEnum.ShinyWaferModel:
                ShinyWaferEcsValue = value;
                break;

            default:
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(calChipSiteModelEnum));
                break;
        }
    }

    public void SetQuality(CalChipSiteModelEnum calChipSiteModelEnum, double value)
    {
        switch (calChipSiteModelEnum)
        {
            case CalChipSiteModelEnum.DswModel:
                DswQuality = value;
                break;

            case CalChipSiteModelEnum.UndefinedModel:
                UndefinedQuality = value;
                break;

            case CalChipSiteModelEnum.HazeModel:
                HazeQuality = value;
                break;

            case CalChipSiteModelEnum.ShinyWaferModel:
                ShinyWaferQuality = value;
                break;

            default:
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(calChipSiteModelEnum));
                break;
        }
    }

    public void SetFilePath(CalChipSiteModelEnum calChipSiteModelEnum, string filePath)
    {
        switch (calChipSiteModelEnum)
        {
            case CalChipSiteModelEnum.DswModel:
                DswFilePath = filePath;
                break;

            case CalChipSiteModelEnum.UndefinedModel:
                UndefinedFilePath = filePath;
                break;

            case CalChipSiteModelEnum.HazeModel:
                HazeFilePath = filePath;
                break;

            case CalChipSiteModelEnum.ShinyWaferModel:
                ShinyWaferFilePath = filePath;
                break;

            default:
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(calChipSiteModelEnum));
                break;
        }
    }

    #region Mapper

    public MicroscopeCalChipDto Clone() => new()
    {
        MicroscopeMagnificationEnum = MicroscopeMagnificationEnum,
        DswPosition = DswPosition,
        DswBrightFieldMachinePosition = DswBrightFieldMachinePosition,
        DswDarkFieldMachinePosition = DswDarkFieldMachinePosition,
        UndefinedPosition = UndefinedPosition,
        UndefinedBrightFieldMachinePosition = UndefinedBrightFieldMachinePosition,
        UndefinedDarkFieldMachinePosition = UndefinedDarkFieldMachinePosition,
        HazePosition = HazePosition,
        HazeBrightFieldMachinePosition = HazeBrightFieldMachinePosition,
        HazeDarkFieldMachinePosition = HazeDarkFieldMachinePosition,
        ShinyWaferPosition = ShinyWaferPosition,
        ShinyWaferBrightFieldMachinePosition = ShinyWaferBrightFieldMachinePosition,
        ShinyWaferDarkFieldMachinePosition = ShinyWaferDarkFieldMachinePosition,
        DswEcsValue = DswEcsValue,
        UndefinedEcsValue = UndefinedEcsValue,
        HazeEcsValue = HazeEcsValue,
        ShinyWaferEcsValue = ShinyWaferEcsValue,
        DswQuality = DswQuality,
        UndefinedQuality = UndefinedQuality,
        HazeQuality = HazeQuality,
        ShinyWaferQuality = ShinyWaferQuality,
        DswFilePath = DswFilePath,
        UndefinedFilePath = UndefinedFilePath,
        HazeFilePath = HazeFilePath,
        ShinyWaferFilePath = ShinyWaferFilePath,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationMicroscopeCalChip AdaptTo() => new()
    {
        CgMicroscopeLens = CustomerAdaptToMapper.Mapper<MicroscopeMagnificationEnum, CgMicroscopeLens>(MicroscopeMagnificationEnum),
        DswBrightFieldMachinePosition = DswBrightFieldMachinePosition.ToCgPoint(),
        DswDarkFieldMachinePosition = DswDarkFieldMachinePosition.ToCgPoint(),
        DswEcsValue = DswEcsValue,
        UndefinedBrightFieldMachinePosition = UndefinedBrightFieldMachinePosition.ToCgPoint(),
        UndefinedDarkFieldMachinePosition = UndefinedDarkFieldMachinePosition.ToCgPoint(),
        UndefinedEcsValue = UndefinedEcsValue,
        HazeBrightFieldMachinePosition = HazeBrightFieldMachinePosition.ToCgPoint(),
        HazeDarkFieldMachinePosition = HazeDarkFieldMachinePosition.ToCgPoint(),
        HazeEcsValue = HazeEcsValue,
        ShinyWaferBrightFieldMachinePosition = ShinyWaferBrightFieldMachinePosition.ToCgPoint(),
        ShinyWaferDarkFieldMachinePosition = ShinyWaferDarkFieldMachinePosition.ToCgPoint(),
        ShinyWaferEcsValue = ShinyWaferEcsValue,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    #endregion Mapper
}