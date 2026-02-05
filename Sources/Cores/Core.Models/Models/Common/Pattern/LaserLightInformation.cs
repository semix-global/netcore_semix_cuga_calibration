using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Cookies;
using Cuga.Data.DataStruct.PMT;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.WPF.MVVM;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace Core.Models.Models.Common.Pattern;

[JsonConverter(typeof(LaserLightInformationConverter))]
public sealed class LaserLightInformation :
    ObservableObject,
    IComparable<LaserLightInformation>,
    IComparable,
    IEquatable<LaserLightInformation>,
    IFormattable,
    IAdaptTo<CgLightConfig>,
    IAdaptIn<CgLightConfig, LaserLightInformation>,
    ICloneable<LaserLightInformation>
{
    public static readonly LaserLightInformation Default = new();

    public double Level
    {
        get;
        private set => SetProperty(ref field, value);
    } = -1;

    public double Coefficient
    {
        get;
        private set => SetProperty(ref field, value);
    } = -1;

    private LaserLightInformation()
    {
    }

    #region IEquatable、IComparable、IFormattable

    public int CompareTo(LaserLightInformation? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;

        var coefficientComparison = Coefficient.CompareTo(other.Coefficient);
        if (coefficientComparison != 0) return coefficientComparison;

        return Level.CompareTo(other.Level);
    }

    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;

        return obj is LaserLightInformation other ? CompareTo(other) : ThrowHelper.ThrowArgumentException<int>($"Object must be of type {nameof(LaserLightInformation)}. ");
    }

    public bool Equals(LaserLightInformation? other) => this == other;

    public override bool Equals(object? obj) => obj is LaserLightInformation other && Equals(other);

    // ReSharper disable NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => HashCode.Combine(Level, Coefficient);
    // ReSharper restore NonReadonlyMemberInGetHashCode

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

    #region Deconstruct

    public void Deconstruct(out double level, out double coefficient) => (level, coefficient) = (Level, Coefficient);

    #endregion Deconstruct

    #region Mapper

    public CgLightConfig AdaptTo() => new()
    {
        LightProp = Level,
        LightCoeff = Coefficient
    };

    public LaserLightInformation AdaptIn(CgLightConfig obj)
    {
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

    private sealed class LaserLightInformationConverter : JsonConverter<LaserLightInformation?>
    {
        private static readonly Lazy<ApplicationCookie> ApplicationCookie = new(HostApplication.GetRequiredService<ApplicationCookie>);

        public override void WriteJson(JsonWriter writer, LaserLightInformation? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();

                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName(nameof(Level));
            writer.WriteValue(value.Level);
            writer.WritePropertyName(nameof(Coefficient));
            writer.WriteValue(value.Coefficient);
            writer.WriteEndObject();
        }

        public override LaserLightInformation ReadJson(JsonReader reader, Type objectType, LaserLightInformation? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return Default;

            var jsonObject = JObject.Load(reader);

            var level = jsonObject[nameof(Level)]?.Value<double>() ?? Default.Level;
            var coefficient = jsonObject[nameof(Coefficient)]?.Value<double>() ?? Default.Coefficient;

            var temp = new LaserLightInformation
            {
                Level = level,
                Coefficient = coefficient
            };

            return ApplicationCookie.Value.LaserLightInformations.SingleOrDefault(t => t == temp, Default);
        }
    }
}