using CommunityToolkit.Diagnostics;
using Cuga.Data.DataStruct.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.Pattern;

public sealed class MicroscopeLensInformation :
    ObservableCacheBase,
    IEquatable<MicroscopeLensInformation>,
    IFormattable,
    IAdaptTo<CgMicroscopeInfo>,
    IAdaptIn<CgMicroscopeInfo, MicroscopeLensInformation>,
    ICloneable<MicroscopeLensInformation>
{
    public static readonly MicroscopeLensInformation Default = new();

    private string _lensName = "N/A";
    private int _lensCode = -1;
    private double _magnification = -1;

    public string LensName
    {
        get => _lensName;
        private set => SetProperty(ref _lensName, value);
    }

    public int LensCode
    {
        get => _lensCode;
        private set => SetProperty(ref _lensCode, value);
    }

    public double Magnification
    {
        get => _magnification;
        private set => SetProperty(ref _magnification, value);
    }

    internal MicroscopeLensInformation()
    {
    }

    #region IEquatable、IFormattable

    public bool Equals(MicroscopeLensInformation? other) => this == other;

    public override bool Equals(object? obj) => obj is MicroscopeLensInformation other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(LensName, LensCode, Magnification);

    public override string ToString() => ToString(null);

    public string ToString(string? format, IFormatProvider? formatProvider = null) => LensName;

    #endregion IEquatable、IFormattable

    #region Operator

    public static bool operator ==(MicroscopeLensInformation? left, MicroscopeLensInformation? right) => (left, right) switch
    {
        (null, null) => true,
        (null, _) => false,
        (_, null) => false,
        (_, _) => ReferenceEquals(left, right) || (Equals(left.LensName, right.LensName) &&
                                                   Equals(left.LensCode, right.LensCode) &&
                                                   Equals(left.Magnification, right.Magnification))
    };

    public static bool operator !=(MicroscopeLensInformation? left, MicroscopeLensInformation? right) => !(left == right);

    #endregion Operator

    #region Deconstruct

    public void Deconstruct(out string lensName, out int lensCode, out double magnification) => (lensName, lensCode, magnification) = (LensName, LensCode, Magnification);

    #endregion Deconstruct

    #region Mapper

    public CgMicroscopeInfo AdaptTo() => new()
    {
        LensName = LensName,
        LensCode = Enum.IsDefined(typeof(CgMicroscopeLens), LensCode)
            ? (CgMicroscopeLens)LensCode
            : ThrowHelper.ThrowArgumentOutOfRangeException<CgMicroscopeLens>(nameof(LensCode)),
        Lens = Convert.ToInt32(Magnification)
    };

    public MicroscopeLensInformation AdaptIn(CgMicroscopeInfo obj)
    {
        Guard.IsNotNull(obj, nameof(obj));

        LensCode = (int)obj.LensCode;
        LensName = obj.LensName;
        Magnification = obj.Lens;

        return this;
    }

    public MicroscopeLensInformation Clone() => new()
    {
        LensName = LensName,
        LensCode = LensCode,
        Magnification = Magnification
    };

    #endregion Mapper
}