using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Cookies;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.WPF.MVVM;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Core.Models.Models.Common.Pattern;

[JsonConverter(typeof(CIBInformationConverter))]
public sealed partial class CIBInformation :
    ObservableObject,
    IComparable<CIBInformation>,
    IComparable,
    IEquatable<CIBInformation>,
    IFormattable,
    IAdaptIn<(int id, int chl, bool used), CIBInformation>,
    IAdaptIn<CIBInformation, CIBInformation>,
    ICloneable<CIBInformation>
{
    public static readonly CIBInformation Default = new();
    public static readonly Regex Regex = new(@"^(-?\d+)\((-?\d+)\)$", RegexOptions.Compiled);
    public static readonly Regex PMTChannelRegex = new(@"PMT(-?\d+)-CH(-?\d+)", RegexOptions.Compiled);

    [ObservableProperty]
    public partial int PMTId { get; private set; } = -1;

    [ObservableProperty]
    public partial int ChannelId { get; private set; } = -1;

    private CIBInformation()
    {
    }

    #region IEquatable、IComparable、IFormattable

    public int CompareTo(CIBInformation? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;

        var pmtIdComparison = PMTId.CompareTo(other.PMTId);
        if (pmtIdComparison != 0) return pmtIdComparison;

        return ChannelId.CompareTo(other.ChannelId);
    }

    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;

        return obj is CIBInformation other ? CompareTo(other) : ThrowHelper.ThrowArgumentException<int>($"Object must be of type {nameof(CIBInformation)}. ");
    }

    public bool Equals(CIBInformation? other) => this == other;

    public override bool Equals(object? obj) => obj is CIBInformation other && Equals(other);

    // ReSharper disable NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => HashCode.Combine(PMTId, ChannelId);
    // ReSharper restore NonReadonlyMemberInGetHashCode

    public override string ToString() => ToString(null);

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.CurrentCulture;

        return $"{PMTId.ToString(format, formatProvider)}({ChannelId.ToString(format, formatProvider)})";
    }

    #endregion IEquatable、IFormattable

    #region Operator

    public static bool operator ==(CIBInformation? left, CIBInformation? right) => (left, right) switch
    {
        (null, null) => true,
        (null, _) => false,
        (_, null) => false,
        (_, _) => ReferenceEquals(left, right) || (Equals(left.PMTId, right.PMTId) &&
                                                   Equals(left.ChannelId, right.ChannelId))
    };

    public static bool operator !=(CIBInformation? left, CIBInformation? right) => !(left == right);

    #endregion Operator

    #region Deconstruct

    public void Deconstruct(out int pmtId, out int channelId) => (pmtId, channelId) = (PMTId, ChannelId);

    #endregion Deconstruct

    #region Mapper

    public CIBInformation AdaptIn((int id, int chl, bool used) obj)
    {
        Guard.IsTrue(obj.used);

        PMTId = obj.id;
        ChannelId = obj.chl;

        return this;
    }

    public CIBInformation AdaptIn(CIBInformation obj)
    {
        PMTId = obj.PMTId;
        ChannelId = obj.ChannelId;

        return this;
    }

    public CIBInformation Clone() => new()
    {
        PMTId = PMTId,
        ChannelId = ChannelId
    };

    #endregion Mapper

    public object ToHtmlAnonymous() => new
    {
        PMTId,
        ChannelId
    };

    private sealed class CIBInformationConverter : JsonConverter<CIBInformation?>
    {
        private static readonly Lazy<ApplicationCookie> ApplicationCookie = new(HostApplication.GetRequiredService<ApplicationCookie>);

        public override void WriteJson(JsonWriter writer, CIBInformation? value, JsonSerializer serializer)
        {
            value ??= Default;

            writer.WriteStartObject();
            writer.WritePropertyName(nameof(PMTId));
            writer.WriteValue(value.PMTId);
            writer.WritePropertyName(nameof(ChannelId));
            writer.WriteValue(value.ChannelId);
            writer.WriteEndObject();
        }

        public override CIBInformation ReadJson(JsonReader reader, Type objectType, CIBInformation? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return Default;

            var jsonObject = JObject.Load(reader);

            var pmtId = jsonObject[nameof(PMTId)]?.Value<int>() ?? -1;
            var channelId = jsonObject[nameof(ChannelId)]?.Value<int>() ?? -1;

            var temp = new CIBInformation
            {
                PMTId = pmtId,
                ChannelId = channelId
            };

            return ApplicationCookie.Value.CIBInformations.SingleOrDefault(t => t == temp, Default);
        }
    }
}