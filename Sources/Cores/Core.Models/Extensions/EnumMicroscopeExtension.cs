using CommunityToolkit.Diagnostics;
using Cuga.Data.DataStruct.Microscope.Enums;

#if NET
using Semix.GRPC.DTO.Basic;

#else
using Semix.WcfTransfer.DTO.Basic;

#endif

namespace Core.Models.Extensions;

public static class EnumMicroscopeExtension
{
    public static int ToInt(this CgMicroscopeLens @this) => @this switch
    {
        CgMicroscopeLens.None => 0,
        CgMicroscopeLens.One => 1,
        CgMicroscopeLens.Two => 2,
        CgMicroscopeLens.Three => 3,
        CgMicroscopeLens.Four => 4,
        CgMicroscopeLens.Five => 5,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<int>(nameof(@this))
    };

    public static CgMicroscopeLens ToCgMicroscopeLens(this int @this) => @this switch
    {
        0 => CgMicroscopeLens.None,
        1 => CgMicroscopeLens.One,
        2 => CgMicroscopeLens.Two,
        3 => CgMicroscopeLens.Three,
        4 => CgMicroscopeLens.Four,
        5 => CgMicroscopeLens.Five,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgMicroscopeLens>(nameof(@this))
    };

    public static ESxMicroscopelens ToESxMicroscopeLens(this CgMicroscopeLens @this) => @this switch
    {
        CgMicroscopeLens.None => ESxMicroscopelens.None,
        CgMicroscopeLens.One => ESxMicroscopelens.One,
        CgMicroscopeLens.Two => ESxMicroscopelens.Two,
        CgMicroscopeLens.Three => ESxMicroscopelens.Three,
        CgMicroscopeLens.Four => ESxMicroscopelens.Four,
        CgMicroscopeLens.Five => ESxMicroscopelens.Five,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<ESxMicroscopelens>(nameof(@this))
    };

    public static CgMicroscopeLens ToCgMicroscopeLens(this ESxMicroscopelens @this) => @this switch
    {
        ESxMicroscopelens.None => CgMicroscopeLens.None,
        ESxMicroscopelens.One => CgMicroscopeLens.One,
        ESxMicroscopelens.Two => CgMicroscopeLens.Two,
        ESxMicroscopelens.Three => CgMicroscopeLens.Three,
        ESxMicroscopelens.Four => CgMicroscopeLens.Four,
        ESxMicroscopelens.Five => CgMicroscopeLens.Five,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgMicroscopeLens>(nameof(@this))
    };
}