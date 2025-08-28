using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Cuga.Data.DataStruct.PMT;
using Net.Utilities.Mapper.Interfaces;
using System.Globalization;

namespace Core.Models.Models.Common.Pattern;

public partial class LaserLightInformation :
    ObservableObject,
    IEquatable<LaserLightInformation>,
    IFormattable,
    IAdaptIn<CgLightConfig, LaserLightInformation>,
    ICloneable<LaserLightInformation>
{
    public static readonly LaserLightInformation Default = new();

    [ObservableProperty]
    private double _level;

    [ObservableProperty]
    private double _coefficient;

    #region IEquatable、IFormattable

    public bool Equals(LaserLightInformation? other) => this == other;

    public override bool Equals(object? obj) => obj is LaserLightInformation other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Level, Coefficient);

    public override string ToString() => ToString(null);

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        format ??= "0.###";
        formatProvider ??= CultureInfo.CurrentCulture;

        return $"{Level.ToString(format, formatProvider)}({Coefficient.ToString(format, formatProvider)})";
    }

    #endregion IEquatable、IFormattable

    #region Operator

    public static bool operator ==(LaserLightInformation? left, LaserLightInformation? right) => (left, right) switch
    {
        (null, null) => true,
        (null, _) => false,
        (_, null) => false,
        (_, _) => ReferenceEquals(left, right) || (Equals(left.Level, right.Level) &&
                                                   Equals(left.Coefficient, right.Coefficient))
    };

    public static bool operator !=(LaserLightInformation? left, LaserLightInformation? right) => !(left == right);

    #endregion Operator

    #region Mapper

    public LaserLightInformation AdaptIn(CgLightConfig obj)
    {
        Guard.IsNotNull(obj);

        Level = obj.LightProp;
        Coefficient = obj.LightCoeff;

        return this;
    }

    public LaserLightInformation Clone() => new()
    {
        Level = Level,
        Coefficient = Coefficient
    };

    #endregion Mapper
}