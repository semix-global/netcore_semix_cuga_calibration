using CommunityToolkit.Diagnostics;
using Cuga.Data.DataStruct.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;

namespace Core.Models.Models.Common.Pattern;

public sealed class MicroscopeLensInformation :
    ObservableCacheBase,
    IComparable,
    IComparable<MicroscopeLensInformation>,
    IEquatable<MicroscopeLensInformation>,
    IFormattable,
    IAdaptTo<CgMicroscopeInfo>,
    IAdaptIn<CgMicroscopeInfo, MicroscopeLensInformation>,
    ICloneable<MicroscopeLensInformation>
{
    public static readonly MicroscopeLensInformation Default = new();

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public string LensName
    {
        get;
        private set => SetProperty(ref field, value);
    } = "N/A";

    public int LensCode
    {
        get;
        private set => SetProperty(ref field, value);
    } = -1;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public double ObjectiveMagnification
    {
        get;
        private set => SetProperty(ref field, value);
    } = -1;

    private MicroscopeLensInformation()
    {
    }

    #region IEquatable、IComparable、IFormattable

    public int CompareTo(MicroscopeLensInformation? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;

        var objectiveMagnificationComparison = ObjectiveMagnification.CompareTo(other.ObjectiveMagnification);
        if (objectiveMagnificationComparison != 0) return objectiveMagnificationComparison;

        var lensCodeComparison = LensCode.CompareTo(other.LensCode);
        if (lensCodeComparison != 0) return lensCodeComparison;

        return string.Compare(LensName, other.LensName, StringComparison.Ordinal);
    }

    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;

        return obj is MicroscopeLensInformation other ? CompareTo(other) : ThrowHelper.ThrowArgumentException<int>($"Object must be of type {nameof(MicroscopeLensInformation)}. ");
    }

    public bool Equals(MicroscopeLensInformation? other) => this == other;

    public override bool Equals(object? obj) => obj is MicroscopeLensInformation other && Equals(other);

    // ReSharper disable NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => HashCode.Combine(LensCode);
    // ReSharper restore NonReadonlyMemberInGetHashCode

    public override string ToString() => ToString(null);

    public string ToString(string? format, IFormatProvider? formatProvider = null) => LensName;

    #endregion IEquatable、IFormattable

    #region Operator

    public static bool operator ==(MicroscopeLensInformation? left, MicroscopeLensInformation? right) => (left, right) switch
    {
        (null, null) => true,
        (null, _) => false,
        (_, null) => false,
        (_, _) => ReferenceEquals(left, right) || (Equals(left.LensCode, right.LensCode))
    };

    public static bool operator !=(MicroscopeLensInformation? left, MicroscopeLensInformation? right) => !(left == right);

    #endregion Operator

    #region Deconstruct

    public void Deconstruct(out string lensName, out int lensCode, out double magnification) => (lensName, lensCode, magnification) = (LensName, LensCode, ObjectiveMagnification);

    #endregion Deconstruct

    #region Mapper

    public CgMicroscopeInfo AdaptTo() => new()
    {
        LensName = LensName,
        LensCode = Enum.IsDefined(typeof(CgMicroscopeLens), LensCode)
            ? (CgMicroscopeLens)LensCode
            : ThrowHelper.ThrowArgumentOutOfRangeException<CgMicroscopeLens>(nameof(LensCode)),
        Lens = Convert.ToInt32(ObjectiveMagnification)
    };

    public MicroscopeLensInformation AdaptIn(CgMicroscopeInfo obj)
    {
        LensName = GuardUtils.IsNotNullAndReturn(obj.LensName);
        LensCode = (int)obj.LensCode;
        ObjectiveMagnification = obj.Lens;

        return this;
    }

    public MicroscopeLensInformation Clone() => new()
    {
        LensName = LensName,
        LensCode = LensCode,
        ObjectiveMagnification = ObjectiveMagnification
    };

    #endregion Mapper
}