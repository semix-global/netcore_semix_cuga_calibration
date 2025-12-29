using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Cuga.Data.DataStruct.DTO.Swath;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

#if NET
using Semix.GRPC.DTO;
#else
using Semix.WcfTransfer.DTO;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;

#endif

namespace Core.Models.Models.Common.Pattern;

public sealed class ProductivityInformation :
    ObservableCacheBase,
    IComparable,
    IComparable<ProductivityInformation>,
    IEquatable<ProductivityInformation>,
    IFormattable,
    IAdaptTo<C2MProductivityInfo>,
    /*IAdaptIn<C2MProductivityInfo, ProductivityInformation>,*/
    ICloneable<ProductivityInformation>
{
    public static readonly ProductivityInformation Default = new();

    [Newtonsoft.Json.JsonIgnore]
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

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public double XPixelSize
    {
        get;
        set => SetProperty(ref field, value);
    } = -1;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public double YPixelSize
    {
        get;
        private set => SetProperty(ref field, value);
    } = -1;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public int YPixel
    {
        get;
        private set => SetProperty(ref field, value);
    } = -1;

    [Newtonsoft.Json.JsonIgnore]
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
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public double SampleRate
    {
        get;
        private set => SetProperty(ref field, value);
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

    public string ToString(string? format, IFormatProvider? formatProvider = null) => $"{OpticsIlluminationModeEnum.ToString()}-{Name}({((SxMAGEnum)OpticsMagType).ToString()[0]}-{((SxSpeedEnum)StageSpeedType).ToString()[0]})";

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

    public void Deconstruct(out string name, out OpticsIlluminationModeEnum opticsIlluminationModeEnum, out int opticsMagType, out int stageSpeedType, out double xPixelSize, out double yPixelSize, out int yPixel, out double originYPixel, out double sampleRate)
        => (name, opticsIlluminationModeEnum, opticsMagType, stageSpeedType, xPixelSize, yPixelSize, yPixel, originYPixel, sampleRate) = (Name, OpticsIlluminationModeEnum, OpticsMagType, StageSpeedType, XPixelSize, YPixelSize, YPixel, OriginYPixel, SampleRate);

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

    public ProductivityInformation AdaptIn(C2MProductivityInfo obj, CgSwathSpeedInfo swathSpeedInfo, double originYPixel)
    {
#if NETFRAMEWORK
        OpticsIlluminationModeEnum = obj.NIOI.ToOpticsIlluminationModeEnum();
#endif
        Name = obj.Name;
        OpticsMagType = (int)obj.Mag;
        StageSpeedType = (int)obj.Speed;
#if NET48
        XPixelSize = swathSpeedInfo.Speed
            .Single(t => t.Key == obj.Speed.ToCgSpeedLevelType())
            .Value
            .XPixelSize;
#endif
        YPixelSize = swathSpeedInfo.YPixelSize;
        YPixel = Convert.ToInt32(swathSpeedInfo.YPixel);
        OriginYPixel = Convert.ToInt32(originYPixel);
        SampleRate = swathSpeedInfo.Hz;

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
        SampleRate = SampleRate
    };

    #endregion Mapper
}