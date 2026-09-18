using Core.Models.Enums.Algorithm;
using Core.Models.Models.Setting;
using Cuga.Data.DataStruct.Basic;
using Net.Utilities.Models.Geometries;

#if NET
using Semix.GRPC.DTO.Basic;
#else
using Semix.WcfTransfer.DTO.Basic;

#endif

namespace Core.Models.Extensions;

public static class EnumAlgorithmExtension
{
    #region TemplateSize

    public static Size ToSize(this AlgorithmTemplateSizeEnum algorithmTemplateSizeEnum) => new(Convert.ToInt32(algorithmTemplateSizeEnum), Convert.ToInt32(algorithmTemplateSizeEnum));

    #endregion TemplateSize

    #region TemplateType

    /// <summary>
    /// 匹配方式：1-形状；2-灰度值
    /// </summary>
    public static ushort ToAlgorithmTemplateType(this AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum) => algorithmTemplateTypeEnum switch
    {
        AlgorithmTemplateTypeEnum.Sharpe => 1,
        AlgorithmTemplateTypeEnum.Ncc => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithmTemplateTypeEnum), algorithmTemplateTypeEnum, null)
    };

    /// <summary>
    /// 匹配方式：1-形状；2-灰度值
    /// </summary>
    public static AlgorithmTemplateTypeEnum ToAlgorithmTemplateTypeEnum(this ushort algorithmTemplateType) => algorithmTemplateType switch
    {
        1 => AlgorithmTemplateTypeEnum.Sharpe,
        2 => AlgorithmTemplateTypeEnum.Ncc,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithmTemplateType), algorithmTemplateType, null)
    };

    public static string ToFileExtension(this AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum) => algorithmTemplateTypeEnum switch
    {
        AlgorithmTemplateTypeEnum.Sharpe => ".shm",
        AlgorithmTemplateTypeEnum.Ncc => ".ncc",
        _ => throw new ArgumentOutOfRangeException(nameof(algorithmTemplateTypeEnum), algorithmTemplateTypeEnum, null)
    };

    public static string ToFullFilePath(this AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, string templateFilePath) => $"{templateFilePath}{algorithmTemplateTypeEnum.ToFileExtension()}";

    public static double ToTemplateMatchScoreThreshold(this AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, CalibrationSetting calibrationSetting) => algorithmTemplateTypeEnum switch
    {
        AlgorithmTemplateTypeEnum.Sharpe => calibrationSetting.SettingTemplateMatchParam.SharpeTypeTemplateMatchScoreThreshold,
        AlgorithmTemplateTypeEnum.Ncc => calibrationSetting.SettingTemplateMatchParam.NccTypeTemplateMatchScoreThreshold,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithmTemplateTypeEnum), algorithmTemplateTypeEnum, null)
    };

    #endregion TemplateType

    #region WaferType

    public static ESxWaferEnum ToESxWaferEnum(this AlgorithmWaferTypeEnum algorithmWaferTypeEnum) => algorithmWaferTypeEnum switch
    {
        AlgorithmWaferTypeEnum.D200 => ESxWaferEnum.D200,
        AlgorithmWaferTypeEnum.D300 => ESxWaferEnum.D300,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithmWaferTypeEnum), algorithmWaferTypeEnum, null)
    };

    public static AlgorithmWaferTypeEnum ToAlgorithmWaferTypeEnum(this ESxWaferEnum eSxWaferEnum) => eSxWaferEnum switch
    {
        ESxWaferEnum.D200 => AlgorithmWaferTypeEnum.D200,
        ESxWaferEnum.D300 => AlgorithmWaferTypeEnum.D300,
        _ => throw new ArgumentOutOfRangeException(nameof(eSxWaferEnum), eSxWaferEnum, null)
    };

    public static CgWaferType ToCgWaferType(this AlgorithmWaferTypeEnum algorithmWaferTypeEnum) => algorithmWaferTypeEnum switch
    {
        AlgorithmWaferTypeEnum.D200 => CgWaferType.D200,
        AlgorithmWaferTypeEnum.D300 => CgWaferType.D300,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithmWaferTypeEnum), algorithmWaferTypeEnum, null)
    };

    public static AlgorithmWaferTypeEnum ToAlgorithmWaferTypeEnum(this CgWaferType cgWaferType) => cgWaferType switch
    {
        CgWaferType.D200 => AlgorithmWaferTypeEnum.D200,
        CgWaferType.D300 => AlgorithmWaferTypeEnum.D300,
        _ => throw new ArgumentOutOfRangeException(nameof(cgWaferType), cgWaferType, null)
    };

    #endregion WaferType

    #region StandardMaskSquareSize

    public static Size ToSize(this AlgorithmStandardMaskSquareSizeEnum algorithmStandardMaskSquareSizeEnum) => new(Convert.ToInt32(algorithmStandardMaskSquareSizeEnum), Convert.ToInt32(algorithmStandardMaskSquareSizeEnum));

    #endregion StandardMaskSquareSize
}