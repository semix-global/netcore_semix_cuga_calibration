using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Wcf.Models.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Mapper;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Microscope.CalChip;

public sealed partial class MicroscopeCalChipDto : CalibrationDtoBase, ICloneable<MicroscopeCalChipDto>, IAdaptTo<CalibrationMicroscopeCalChip>
{
    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum;

    #region Chuck

    [ObservableProperty]
    private double _chuckAfEcsValue;

    [ObservableProperty]
    private double _chuckAfMotorValue;

    #endregion

    #region Dsw

    [ObservableProperty]
    private Point _dswBrightFieldMachinePosition;

    [ObservableProperty]
    private Point _dswDarkFieldMachinePosition;

    [ObservableProperty]
    private double _dswEcsValue;

    [ObservableProperty]
    private double _dswQuality;

    [ObservableProperty]
    private string _dswFilePath = string.Empty;

    [ObservableProperty]
    private double _dswAfEcsValue;

    [ObservableProperty]
    private double _dswAfMotorValue;

    #endregion

    #region Undefined

    [ObservableProperty]
    private Point _undefinedBrightFieldMachinePosition;

    [ObservableProperty]
    private Point _undefinedDarkFieldMachinePosition;

    [ObservableProperty]
    private double _undefinedEcsValue;

    [ObservableProperty]
    private string _undefinedFilePath = string.Empty;

    [ObservableProperty]
    private double _undefinedQuality;

    #endregion

    #region Haze

    [ObservableProperty]
    private Point _hazeBrightFieldMachinePosition;

    [ObservableProperty]
    private Point _hazeDarkFieldMachinePosition;

    [ObservableProperty]
    private double _hazeEcsValue;

    [ObservableProperty]
    private string _hazeFilePath = string.Empty;

    [ObservableProperty]
    private double _hazeAfEcsValue;

    [ObservableProperty]
    private double _hazeAfMotorValue;

    [ObservableProperty]
    private double _hazeQuality;

    #endregion

    #region ShinyWafer

    [ObservableProperty]
    private Point _shinyWaferBrightFieldMachinePosition;

    [ObservableProperty]
    private Point _shinyWaferDarkFieldMachinePosition;

    [ObservableProperty]
    private double _shinyWaferEcsValue;

    [ObservableProperty]
    private double _shinyWaferQuality;

    [ObservableProperty]
    private string _shinyWaferFilePath = string.Empty;

    #endregion

    public double DswToChuckAfEcsValue => DswEcsValue - ChuckAfEcsValue;

    public double DswToChuckAfMotorValue => DswAfMotorValue - ChuckAfMotorValue;

    public double HazeToChuckAfEcsValue => HazeEcsValue - ChuckAfEcsValue;

    public double HazeToChuckAfMotorValue => HazeAfMotorValue - ChuckAfMotorValue;


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

    public double GetAfEcsValue(CalChipSiteModelEnum calChipSiteModelEnum) => calChipSiteModelEnum switch
    {
        CalChipSiteModelEnum.ChuckModel => ChuckAfEcsValue,
        CalChipSiteModelEnum.DswModel => DswAfEcsValue,
        CalChipSiteModelEnum.HazeModel => HazeAfEcsValue,
        _ => 0d
    };

    public double GetAfMotorValue(CalChipSiteModelEnum calChipSiteModelEnum) => calChipSiteModelEnum switch
    {
        CalChipSiteModelEnum.ChuckModel => ChuckAfMotorValue,
        CalChipSiteModelEnum.DswModel => DswAfMotorValue,
        CalChipSiteModelEnum.HazeModel => HazeAfMotorValue,
        _ => 0d
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

    public void SetAfEcsValue(CalChipSiteModelEnum calChipSiteModelEnum, double value)
    {
        switch (calChipSiteModelEnum)
        {
            case CalChipSiteModelEnum.DswModel:
                DswEcsValue = value;
                break;

            case CalChipSiteModelEnum.HazeModel:
                HazeEcsValue = value;
                break;
        }
    }

    public void SetAfMotorValue(CalChipSiteModelEnum calChipSiteModelEnum, double value)
    {
        switch (calChipSiteModelEnum)
        {
            case CalChipSiteModelEnum.DswModel:
                DswAfMotorValue = value;
                break;

            case CalChipSiteModelEnum.HazeModel:
                HazeAfMotorValue = value;
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
        DswBrightFieldMachinePosition = DswBrightFieldMachinePosition,
        DswDarkFieldMachinePosition = DswDarkFieldMachinePosition,
        UndefinedBrightFieldMachinePosition = UndefinedBrightFieldMachinePosition,
        UndefinedDarkFieldMachinePosition = UndefinedDarkFieldMachinePosition,
        HazeBrightFieldMachinePosition = HazeBrightFieldMachinePosition,
        HazeDarkFieldMachinePosition = HazeDarkFieldMachinePosition,
        ShinyWaferBrightFieldMachinePosition = ShinyWaferBrightFieldMachinePosition,
        ShinyWaferDarkFieldMachinePosition = ShinyWaferDarkFieldMachinePosition,
        ChuckAfEcsValue = ChuckAfEcsValue,
        ChuckAfMotorValue = ChuckAfMotorValue,
        DswEcsValue = DswEcsValue,
        DswAfEcsValue = DswAfEcsValue,
        DswAfMotorValue = DswAfMotorValue,
        UndefinedEcsValue = UndefinedEcsValue,
        HazeEcsValue = HazeEcsValue,
        HazeAfEcsValue = HazeAfEcsValue,
        HazeAfMotorValue = HazeAfMotorValue,
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
        ChuckAfEcsValue = ChuckAfEcsValue,
        ChuckAfMotorValue = ChuckAfMotorValue,
        DswBrightFieldMachinePosition = DswBrightFieldMachinePosition.ToCgPoint(),
        DswDarkFieldMachinePosition = DswDarkFieldMachinePosition.ToCgPoint(),
        DswEcsValue = DswEcsValue,
        DswAfEcsValue = DswAfEcsValue,
        DswAfMotorValue = DswAfMotorValue,
        UndefinedBrightFieldMachinePosition = UndefinedBrightFieldMachinePosition.ToCgPoint(),
        UndefinedDarkFieldMachinePosition = UndefinedDarkFieldMachinePosition.ToCgPoint(),
        UndefinedEcsValue = UndefinedEcsValue,
        HazeBrightFieldMachinePosition = HazeBrightFieldMachinePosition.ToCgPoint(),
        HazeDarkFieldMachinePosition = HazeDarkFieldMachinePosition.ToCgPoint(),
        HazeEcsValue = HazeEcsValue,
        HazeAfEcsValue = HazeAfEcsValue,
        HazeAfMotorValue = HazeAfMotorValue,
        ShinyWaferBrightFieldMachinePosition = ShinyWaferBrightFieldMachinePosition.ToCgPoint(),
        ShinyWaferDarkFieldMachinePosition = ShinyWaferDarkFieldMachinePosition.ToCgPoint(),
        ShinyWaferEcsValue = ShinyWaferEcsValue,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    #endregion Mapper
}