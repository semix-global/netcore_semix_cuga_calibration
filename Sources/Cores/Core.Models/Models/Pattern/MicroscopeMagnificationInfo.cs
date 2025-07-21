using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Cuga.Data.DataStruct.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Pattern;

public sealed partial class MicroscopeMagnificationInfo : ObservableCacheBase, ICloneable<MicroscopeMagnificationInfo>, IAdaptTo<CgMicroscopeInfo>, IAdaptIn<CgMicroscopeInfo, MicroscopeMagnificationInfo>
{
    [ObservableProperty]
    private string _microscopeMagnificationName = "NaA";

    [ObservableProperty]
    private int _magnification = -1;

    [ObservableProperty]
    private int _magnificationCode = -1;

    #region Mapper

    public MicroscopeMagnificationInfo Clone() => new()
    {
        MicroscopeMagnificationName = MicroscopeMagnificationName,
        Magnification = Magnification,
        MagnificationCode = MagnificationCode
    };

    public CgMicroscopeInfo AdaptTo() => new()
    {
        LensName = MicroscopeMagnificationName,
        Lens = Magnification,
        LensCode = Enum.IsDefined(typeof(CgMicroscopeLens), MagnificationCode)
            ? (CgMicroscopeLens)MagnificationCode
            : throw new ArgumentException("Invalid MagnificationCode value")
    };

    public MicroscopeMagnificationInfo AdaptIn(CgMicroscopeInfo obj)
    {
        Guard.IsNotNull(obj, nameof(obj));

        MagnificationCode = (int)obj.LensCode;
        Magnification = obj.Lens;
        MicroscopeMagnificationName = obj.LensName;

        return this;
    }

    #endregion

    #region 索引器

    public MicroscopeMagnificationInfo? this[int code] => code == MagnificationCode ? this : null;

    #endregion

    #region Equals

    /// <summary>
    /// 确定指定的对象是否等于当前对象
    /// </summary>
    /// <param name="obj">要与当前对象进行比较的对象</param>
    /// <returns>如果指定的对象等于当前对象，则为 true；否则为 false</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj is MicroscopeMagnificationInfo other && Equals(other);
    }

    /// <summary>
    /// 确定指定的 MicroscopeMagnificationInfo 是否等于当前 MicroscopeMagnificationInfo
    /// </summary>
    /// <param name="other">要与当前对象进行比较的 MicroscopeMagnificationInfo</param>
    /// <returns>如果指定的 MicroscopeMagnificationInfo 等于当前 MicroscopeMagnificationInfo，则为 true；否则为 false</returns>
    public bool Equals(MicroscopeMagnificationInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        // 优先比较唯一标识符（如果 _magnificationCode 可以作为唯一标识）
        if (MagnificationCode != other.MagnificationCode) return false;

        // 比较其他关键属性
        return string.Equals(MicroscopeMagnificationName, other.MicroscopeMagnificationName, StringComparison.Ordinal) &&
               Magnification == other.Magnification;
    }

    /// <summary>
    /// 返回当前对象的哈希码
    /// </summary>
    /// <returns>当前对象的哈希码</returns>
    public override int GetHashCode()
    {
        // 使用 HashCode 结构体生成组合哈希值
        return HashCode.Combine(MagnificationCode, MicroscopeMagnificationName, Magnification);
    }

    /// <summary>
    /// 确定两个 MicroscopeMagnificationInfo 实例是否相等
    /// </summary>
    public static bool operator ==(MicroscopeMagnificationInfo? left, MicroscopeMagnificationInfo? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    /// <summary>
    /// 确定两个 MicroscopeMagnificationInfo 实例是否不相等
    /// </summary>
    public static bool operator !=(MicroscopeMagnificationInfo? left, MicroscopeMagnificationInfo? right)
    {
        return !(left == right);
    }

    #endregion
}