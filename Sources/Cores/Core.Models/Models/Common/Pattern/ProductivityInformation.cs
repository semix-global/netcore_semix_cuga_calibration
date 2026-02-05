using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Cookies;
using Cuga.Data.DataStruct.DTO.Swath;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.WPF.MVVM;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#if NET
using Semix.GRPC.DTO;
#else
using Semix.WcfTransfer.DTO;
using Core.Models.Extensions;

#endif

namespace Core.Models.Models.Common.Pattern;

[JsonConverter(typeof(ProductivityInformationConverter))]
public sealed class ProductivityInformation :
    ObservableObject,
    IComparable,
    IComparable<ProductivityInformation>,
    IEquatable<ProductivityInformation>,
    IFormattable,
    IAdaptTo<C2MProductivityInfo>,
    /*IAdaptIn<C2MProductivityInfo, ProductivityInformation>,*/
    ICloneable<ProductivityInformation>
{
    public static readonly ProductivityInformation Default = new();

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public string Name
    {
        get;
        private set => SetProperty(ref field, value);
    } = "N/A";

    public OpticsIlluminationModeEnum OpticsIlluminationModeEnum
    {
        get;
        private set => SetProperty(ref field, value);
    } = OpticsIlluminationModeEnum.OI;

    public int OpticsMagType
    {
        get;
        private set => SetProperty(ref field, value);
    } = -1;

    public int StageSpeedType
    {
        get;
        private set => SetProperty(ref field, value);
    } = -1;

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public double XPixelSize
    {
        get;
        set => SetProperty(ref field, value);
    } = -1;

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public double YPixelSize
    {
        get;
        private set => SetProperty(ref field, value);
    } = -1;

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public int YPixel
    {
        get;
        private set => SetProperty(ref field, value);
    } = -1;

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public int OriginYPixel
    {
        get;
        private set => SetProperty(ref field, value);
    } = -1;

    /// <summary>
    /// KHz
    /// </summary>
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public double SampleRate
    {
        get;
        private set => SetProperty(ref field, value);
    } = -1;

    /// <summary>
    /// um/s
    /// </summary>
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public double XSpeedValue
    {
        get;
        set => SetProperty(ref field, value);
    } = -1;

    private ProductivityInformation()
    {
    }

    #region IEquatable、IComparable、IFormattable

    public int CompareTo(ProductivityInformation? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;

        var opticsIlluminationModeEnumComparison = OpticsIlluminationModeEnum.CompareTo(other.OpticsIlluminationModeEnum);
        if (opticsIlluminationModeEnumComparison != 0) return opticsIlluminationModeEnumComparison;

        var opticsMagTypeComparison = OpticsMagType.CompareTo(other.OpticsMagType);
        if (opticsMagTypeComparison != 0) return -opticsMagTypeComparison;

        return StageSpeedType.CompareTo(other.StageSpeedType);
    }

    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;

        return obj is ProductivityInformation other ? CompareTo(other) : ThrowHelper.ThrowArgumentException<int>($"Object must be of type {nameof(ProductivityInformation)}. ");
    }

    public bool Equals(ProductivityInformation? other) => this == other;

    public override bool Equals(object? obj) => obj is ProductivityInformation other && Equals(other);

    // ReSharper disable NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => HashCode.Combine(OpticsIlluminationModeEnum, OpticsMagType, StageSpeedType);
    // ReSharper restore NonReadonlyMemberInGetHashCode

    public override string ToString() => ToString(null);

    public string ToString(string? format, IFormatProvider? formatProvider = null) => $"{OpticsIlluminationModeEnum.ToString()}_{Name}({((SxMAGEnum)OpticsMagType).ToString()[0]}-{((SxSpeedEnum)StageSpeedType).ToString()[0]})";

    #endregion IEquatable、IFormattable

    #region Operator

    public static bool operator ==(ProductivityInformation? left, ProductivityInformation? right) => (left, right) switch
    {
        (null, null) => true,
        (null, _) => false,
        (_, null) => false,
        (_, _) => ReferenceEquals(left, right) || (Equals(left.OpticsIlluminationModeEnum, right.OpticsIlluminationModeEnum) &&
                                                   Equals(left.OpticsMagType, right.OpticsMagType) &&
                                                   Equals(left.StageSpeedType, right.StageSpeedType))
    };

    public static bool operator !=(ProductivityInformation? left, ProductivityInformation? right) => !(left == right);

    #endregion Operator

    #region Deconstruct

    public void Deconstruct(out string name, out OpticsIlluminationModeEnum opticsIlluminationModeEnum, out int opticsMagType, out int stageSpeedType, out double xPixelSize, out double yPixelSize, out int yPixel, out double originYPixel, out double sampleRate, out double xSpeedValue)
        => (name, opticsIlluminationModeEnum, opticsMagType, stageSpeedType, xPixelSize, yPixelSize, yPixel, originYPixel, sampleRate, xSpeedValue) = (Name, OpticsIlluminationModeEnum, OpticsMagType, StageSpeedType, XPixelSize, YPixelSize, YPixel, OriginYPixel, SampleRate, XSpeedValue);

    #endregion Deconstruct

    #region Mapper

    public C2MProductivityInfo AdaptTo() => new()
    {
        Name = Name,
#if NETFRAMEWORK
        NIOI = OpticsIlluminationModeEnum.ToSxNIOIEnum(),

#endif
        Mag = Enum.IsDefined(typeof(SxMAGEnum), OpticsMagType)
            ? (SxMAGEnum)OpticsMagType
            : ThrowHelper.ThrowArgumentOutOfRangeException<SxMAGEnum>(nameof(OpticsMagType)),
        Speed = Enum.IsDefined(typeof(SxSpeedEnum), StageSpeedType)
            ? (SxSpeedEnum)StageSpeedType
            : ThrowHelper.ThrowArgumentOutOfRangeException<SxSpeedEnum>(nameof(StageSpeedType))
    };

    public ProductivityInformation AdaptIn(C2MProductivityInfo obj, CgSwathSpeedInfo swathSpeedInfo, double originYPixel, double sampleRate, double xSpeedValue)
    {
        Name = obj.Name;
#if NETFRAMEWORK
        OpticsIlluminationModeEnum = obj.NIOI.ToOpticsIlluminationModeEnum();
#endif
        OpticsMagType = (int)obj.Mag;
        StageSpeedType = (int)obj.Speed;
        YPixelSize = swathSpeedInfo.YPixelSize;
        YPixel = Convert.ToInt32(swathSpeedInfo.YPixel);
        OriginYPixel = Convert.ToInt32(originYPixel);
        SampleRate = sampleRate;
        XSpeedValue = xSpeedValue;
        XPixelSize /*um/px*/ = XSpeedValue /* um/s */ / 1_000d / SampleRate /* KHz */;

        return this;
    }

    public ProductivityInformation Clone() => new()
    {
        Name = Name,
        OpticsIlluminationModeEnum = OpticsIlluminationModeEnum,
        OpticsMagType = OpticsMagType,
        StageSpeedType = StageSpeedType,
        XPixelSize = XPixelSize,
        YPixelSize = YPixelSize,
        YPixel = YPixel,
        OriginYPixel = OriginYPixel,
        SampleRate = SampleRate,
        XSpeedValue = XSpeedValue
    };

    #endregion Mapper

    private sealed class ProductivityInformationConverter : JsonConverter<ProductivityInformation?>
    {
        private static readonly Lazy<ApplicationCookie> ApplicationCookie = new(HostApplication.GetRequiredService<ApplicationCookie>);

        public override void WriteJson(JsonWriter writer, ProductivityInformation? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();

                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName(nameof(OpticsIlluminationModeEnum));
            writer.WriteValue((int)value.OpticsIlluminationModeEnum);
            writer.WritePropertyName(nameof(OpticsMagType));
            writer.WriteValue(value.OpticsMagType);
            writer.WritePropertyName(nameof(StageSpeedType));
            writer.WriteValue(value.StageSpeedType);
            writer.WriteEndObject();
        }

        public override ProductivityInformation ReadJson(JsonReader reader, Type objectType, ProductivityInformation? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return Default;

            var jsonObject = JObject.Load(reader);

            var opticsIlluminationModeEnum = (OpticsIlluminationModeEnum)(jsonObject[nameof(OpticsIlluminationModeEnum)]?.Value<int>() ?? (int)OpticsIlluminationModeEnum.OI);
            var opticsMagType = jsonObject[nameof(OpticsMagType)]?.Value<int>() ?? Default.OpticsMagType;
            var stageSpeedType = jsonObject[nameof(StageSpeedType)]?.Value<int>() ?? Default.StageSpeedType;

            var temp = new ProductivityInformation
            {
                OpticsIlluminationModeEnum = opticsIlluminationModeEnum,
                OpticsMagType = opticsMagType,
                StageSpeedType = stageSpeedType
            };

            return ApplicationCookie.Value.ProductivityInformations.SingleOrDefault(t => t == temp, Default);
        }
    }
}