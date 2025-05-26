using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Core.Models.Extensions;
using Core.Wcf.Models.Chuck;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Mapper;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;

namespace Core.Models.Models.Chuck.Center;

public sealed partial class ChuckCenterObjDto : CalibrationDtoBase, ICloneable<ChuckCenterObjDto>, IAdaptTo<CalibrationCenterObj>
{
    [ObservableProperty]
    private MicroscopeMagnificationEnum _lowMicroscopeMagnificationEnum;

    [ObservableProperty]
    private MicroscopeMagnificationEnum _highMicroscopeMagnificationEnum;

    [ObservableProperty]
    private Point _positiveTopPosition;

    [ObservableProperty]
    private Point _positiveRightPosition;

    [ObservableProperty]
    private Point _positiveBottomPosition;

    [ObservableProperty]
    private Point _positiveLeftPosition;

    [ObservableProperty]
    private Point _negativeTopPosition;

    [ObservableProperty]
    private Point _negativeRightPosition;

    [ObservableProperty]
    private Point _negativeBottomPosition;

    [ObservableProperty]
    private Point _negativeLeftPosition;

    [ObservableProperty]
    private Point _chuckCenterPosition;

    [ObservableProperty]
    private double _chuckRotationAngle;

    [ObservableProperty]
    private Point _bFCenterStagePosition;

    [ObservableProperty]
    private Point _newBFCenterStagePosition;

    [ObservableProperty]
    private string _positiveTopFilePath = string.Empty;

    [ObservableProperty]
    private string _positiveRightFilePath = string.Empty;

    [ObservableProperty]
    private string _positiveLeftFilePath = string.Empty;

    [ObservableProperty]
    private string _positiveBottomFilePath = string.Empty;

    [ObservableProperty]
    private string _negativeTopFilePath = string.Empty;

    [ObservableProperty]
    private string _negativeRightFilePath = string.Empty;

    [ObservableProperty]
    private string _negativeLeftFilePath = string.Empty;

    [ObservableProperty]
    private string _negativeBottomFilePath = string.Empty;

    [ObservableProperty]
    private string _filePath = string.Empty;

    #region Mapper

    public ChuckCenterObjDto Clone() => new()
    {
        LowMicroscopeMagnificationEnum = LowMicroscopeMagnificationEnum,
        HighMicroscopeMagnificationEnum = HighMicroscopeMagnificationEnum,
        PositiveTopPosition = PositiveTopPosition,
        PositiveRightPosition = PositiveRightPosition,
        PositiveBottomPosition = PositiveBottomPosition,
        PositiveLeftPosition = PositiveLeftPosition,
        NegativeTopPosition = NegativeTopPosition,
        NegativeRightPosition = NegativeRightPosition,
        NegativeBottomPosition = NegativeBottomPosition,
        NegativeLeftPosition = NegativeLeftPosition,
        ChuckCenterPosition = ChuckCenterPosition,
        ChuckRotationAngle = ChuckRotationAngle,
        BFCenterStagePosition = BFCenterStagePosition,
        NewBFCenterStagePosition = NewBFCenterStagePosition,
        PositiveTopFilePath = PositiveTopFilePath,
        PositiveRightFilePath = PositiveRightFilePath,
        PositiveLeftFilePath = PositiveLeftFilePath,
        PositiveBottomFilePath = PositiveBottomFilePath,
        NegativeTopFilePath = NegativeTopFilePath,
        NegativeRightFilePath = NegativeRightFilePath,
        NegativeLeftFilePath = NegativeLeftFilePath,
        NegativeBottomFilePath = NegativeBottomFilePath,
        FilePath = FilePath,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationCenterObj AdaptTo() => new()
    {
        CgMicroscopeLens = CustomerAdaptToMapper.Mapper<MicroscopeMagnificationEnum, CgMicroscopeLens>(HighMicroscopeMagnificationEnum),
        NewBFCenterStagePosition = NewBFCenterStagePosition.ToCgPoint(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    #endregion Mapper
}