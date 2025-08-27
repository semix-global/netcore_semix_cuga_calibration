using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Cuga.Data.DataStruct.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.Pattern;

public sealed partial class MicroscopeLensInformation : ObservableCacheBase, ICloneable<MicroscopeLensInformation>, IAdaptTo<CgMicroscopeInfo>, IAdaptIn<CgMicroscopeInfo, MicroscopeLensInformation>
{
    public static readonly MicroscopeLensInformation Default = new();

    [ObservableProperty]
    private string _lensName = "N/A";

    [ObservableProperty]
    private int _lensCode = -1;

    [ObservableProperty]
    private int _magnification = -1;

    #region Mapper

    public MicroscopeLensInformation Clone() => new()
    {
        LensName = LensName,
        LensCode = LensCode,
        Magnification = Magnification
    };

    public CgMicroscopeInfo AdaptTo() => new()
    {
        LensName = LensName,
        LensCode = Enum.IsDefined(typeof(CgMicroscopeLens), LensCode)
            ? (CgMicroscopeLens)LensCode
            : throw new ArgumentException("Invalid LensCode value"),
        Lens = Magnification
    };

    public MicroscopeLensInformation AdaptIn(CgMicroscopeInfo obj)
    {
        Guard.IsNotNull(obj, nameof(obj));

        LensCode = (int)obj.LensCode;
        LensName = obj.LensName;
        Magnification = obj.Lens;

        return this;
    }

    #endregion Mapper

    #region Equals

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj is MicroscopeLensInformation other && Equals(other);
    }

    public bool Equals(MicroscopeLensInformation? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (LensCode != other.LensCode) return false;

        return string.Equals(LensName, other.LensName, StringComparison.Ordinal) &&
               Magnification == other.Magnification;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LensCode, LensName, Magnification);
    }

    public static bool operator ==(MicroscopeLensInformation? left, MicroscopeLensInformation? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(MicroscopeLensInformation? left, MicroscopeLensInformation? right)
    {
        return !(left == right);
    }

    #endregion Equals
}