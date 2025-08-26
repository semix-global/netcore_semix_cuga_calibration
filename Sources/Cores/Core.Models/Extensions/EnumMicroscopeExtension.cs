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
}