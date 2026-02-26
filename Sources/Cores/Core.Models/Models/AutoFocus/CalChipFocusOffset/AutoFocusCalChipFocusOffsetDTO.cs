using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.AutoFocus;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.AutoFocus;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Optics;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.AutoFocus.CalChipFocusOffset;

public sealed partial class AutoFocusCalChipFocusOffsetDTO : CalibrationDtoBase, ICloneable<AutoFocusCalChipFocusOffsetDTO>, IAdaptTo<CalibrationAutoFocusCalChipFocusOffset>
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private RuntimeAfCalibrationResultDTO _dSWRuntimeAfCalibrationResultDTO = RuntimeAfCalibrationResultDTO.Default;

    [ObservableProperty]
    private RuntimeAfCalibrationResultDTO _hazeRuntimeAfCalibrationResultDTO = RuntimeAfCalibrationResultDTO.Default;

    [ObservableProperty]
    private RuntimeAfCalibrationResultDTO _chuckRuntimeAfCalibrationResultDTO = RuntimeAfCalibrationResultDTO.Default;

    public double DswToChuckEcsValue => DSWRuntimeAfCalibrationResultDTO.ECSValue - ChuckRuntimeAfCalibrationResultDTO.ECSValue;

    public double DswToChuckMotorValue => DSWRuntimeAfCalibrationResultDTO.MotorValue - ChuckRuntimeAfCalibrationResultDTO.MotorValue;

    public double HazeToChuckEcsValue => HazeRuntimeAfCalibrationResultDTO.ECSValue - ChuckRuntimeAfCalibrationResultDTO.ECSValue;

    public double HazeToChuckMotorValue => HazeRuntimeAfCalibrationResultDTO.MotorValue - ChuckRuntimeAfCalibrationResultDTO.MotorValue;

    public AutoFocusCalChipFocusOffsetDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        DSWRuntimeAfCalibrationResultDTO = DSWRuntimeAfCalibrationResultDTO.Clone(),
        HazeRuntimeAfCalibrationResultDTO = HazeRuntimeAfCalibrationResultDTO.Clone(),
        ChuckRuntimeAfCalibrationResultDTO = ChuckRuntimeAfCalibrationResultDTO.Clone(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    public CalibrationAutoFocusCalChipFocusOffset AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Speed = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType() : CgSpeedLevelType.ErrorCgSpeedLevelType,
        IsAFServo = ChuckRuntimeAfCalibrationResultDTO.IsAFServo,
        ChuckEcsValue = ChuckRuntimeAfCalibrationResultDTO.ECSValue,
        ChuckMotorValue = ChuckRuntimeAfCalibrationResultDTO.MotorValue,
        HazeEcsValue = HazeRuntimeAfCalibrationResultDTO.ECSValue,
        HazeMotorValue = HazeRuntimeAfCalibrationResultDTO.MotorValue,
        DswEcsValue = DSWRuntimeAfCalibrationResultDTO.ECSValue,
        DswMotorValue = DSWRuntimeAfCalibrationResultDTO.MotorValue,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };
}