using Core.Models.Enums.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;

#if NET
using Semix.GRPC.DTO.Basic;
#else
using Semix.WcfTransfer.DTO.Basic;

#endif

namespace Core.Models.Extensions;

public static class EnumMicroscopeExtension
{
    public static ushort ToUshort(this CgMicroscopeLens cgMicroscopeLens) => cgMicroscopeLens switch
    {
        CgMicroscopeLens.None => 0,
        CgMicroscopeLens.One => 1,
        CgMicroscopeLens.Two => 2,
        CgMicroscopeLens.Three => 3,
        CgMicroscopeLens.Four => 4,
        CgMicroscopeLens.Five => 5,
        _ => throw new ArgumentOutOfRangeException(nameof(cgMicroscopeLens), cgMicroscopeLens, null)
    };

    #region MicroscopeMagnificationEnum <=> int

    public static int ToCgLens(this MicroscopeMagnificationEnum microscopeMagnificationEnum) => microscopeMagnificationEnum switch
    {
        MicroscopeMagnificationEnum.Magnification5X => 5,
        MicroscopeMagnificationEnum.Magnification10X => 10,
        MicroscopeMagnificationEnum.Magnification50X => 50,
        MicroscopeMagnificationEnum.Magnification100X => 100,
        MicroscopeMagnificationEnum.Magnification150X => 150,
        _ => throw new ArgumentOutOfRangeException(nameof(microscopeMagnificationEnum), microscopeMagnificationEnum, null)
    };

    public static MicroscopeMagnificationEnum ToMicroscopeMagnificationEnum(this int cgLens) => cgLens switch
    {
        5 => MicroscopeMagnificationEnum.Magnification5X,
        10 => MicroscopeMagnificationEnum.Magnification10X,
        50 => MicroscopeMagnificationEnum.Magnification50X,
        100 => MicroscopeMagnificationEnum.Magnification100X,
        150 => MicroscopeMagnificationEnum.Magnification150X,
        _ => throw new ArgumentOutOfRangeException(nameof(cgLens), cgLens, null)
    };

    #endregion MicroscopeMagnificationEnum <=> int

    #region ESxMicroscopelens <=> CgMicroscopeLens

    public static ESxMicroscopelens ToESxMicroscopeLens(this CgMicroscopeLens cgMicroscopeLens) => cgMicroscopeLens switch
    {
        CgMicroscopeLens.None => ESxMicroscopelens.None,
        CgMicroscopeLens.One => ESxMicroscopelens.One,
        CgMicroscopeLens.Two => ESxMicroscopelens.Two,
        CgMicroscopeLens.Three => ESxMicroscopelens.Three,
        CgMicroscopeLens.Four => ESxMicroscopelens.Four,
        CgMicroscopeLens.Five => ESxMicroscopelens.Five,
        _ => throw new ArgumentOutOfRangeException(nameof(cgMicroscopeLens), cgMicroscopeLens, null)
    };

    public static CgMicroscopeLens ToCgMicroscopeLens(this ESxMicroscopelens sxMicroscopeLens) => sxMicroscopeLens switch
    {
        ESxMicroscopelens.None => CgMicroscopeLens.None,
        ESxMicroscopelens.One => CgMicroscopeLens.One,
        ESxMicroscopelens.Two => CgMicroscopeLens.Two,
        ESxMicroscopelens.Three => CgMicroscopeLens.Three,
        ESxMicroscopelens.Four => CgMicroscopeLens.Four,
        ESxMicroscopelens.Five => CgMicroscopeLens.Five,
        _ => throw new ArgumentOutOfRangeException(nameof(sxMicroscopeLens), sxMicroscopeLens, null)
    };

    #endregion ESxMicroscopelens <=> CgMicroscopeLens

    #region Mock CgMicroscopeLens <=> MicroscopeMagnificationEnum

    /// <summary>
    /// 为了实现Mock方法，实际逻辑不要使用。
    /// </summary>
    public static CgMicroscopeLens ToCgMicroscopeLens(this MicroscopeMagnificationEnum microscopeMagnificationEnum) => microscopeMagnificationEnum switch
    {
        MicroscopeMagnificationEnum.Magnification5X => CgMicroscopeLens.One,
        MicroscopeMagnificationEnum.Magnification10X => CgMicroscopeLens.Two,
        MicroscopeMagnificationEnum.Magnification50X => CgMicroscopeLens.Three,
        MicroscopeMagnificationEnum.Magnification100X => CgMicroscopeLens.Four,
        MicroscopeMagnificationEnum.Magnification150X => CgMicroscopeLens.Five,
        _ => throw new ArgumentOutOfRangeException(nameof(microscopeMagnificationEnum), microscopeMagnificationEnum, null)
    };

    /// <summary>
    /// 为了实现Mock方法，实际逻辑不要使用。
    /// </summary>
    public static MicroscopeMagnificationEnum ToMicroscopeMagnificationEnum(this CgMicroscopeLens cgMicroscopeLens) => cgMicroscopeLens switch
    {
        CgMicroscopeLens.One => MicroscopeMagnificationEnum.Magnification5X,
        CgMicroscopeLens.Two => MicroscopeMagnificationEnum.Magnification10X,
        CgMicroscopeLens.Three => MicroscopeMagnificationEnum.Magnification50X,
        CgMicroscopeLens.Four => MicroscopeMagnificationEnum.Magnification100X,
        CgMicroscopeLens.Five => MicroscopeMagnificationEnum.Magnification150X,
        _ => throw new ArgumentOutOfRangeException(nameof(cgMicroscopeLens), cgMicroscopeLens, null)
    };

    #endregion Mock CgMicroscopeLens <=> MicroscopeMagnificationEnum
}