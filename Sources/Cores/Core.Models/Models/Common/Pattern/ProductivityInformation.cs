using CommunityToolkit.Diagnostics;
using Core.Models.Extensions;
using Cuga.Data.DataStruct.DTO.Swath;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

#if NET
using Semix.GRPC.DTO;
#else
using Semix.WcfTransfer.DTO;
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

    private string _name = "N/A";
    private int _opticsMagType = -1;
    private int _stageSpeedType = -1;
    private double _xPixelSize = -1;
    private double _yPixelSize = -1;
    private int _yPixel = -1;
    private int _originYPixel = -1;
    private double _sampleRate = -1;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public string Name
    {
        get => _name;
        private set => SetProperty(ref _name, value);
    }

    public int OpticsMagType
    {
        get => _opticsMagType;
        private set => SetProperty(ref _opticsMagType, value);
    }

    public int StageSpeedType
    {
        get => _stageSpeedType;
        private set => SetProperty(ref _stageSpeedType, value);
    }

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public double XPixelSize
    {
        get => _xPixelSize;
        set => SetProperty(ref _xPixelSize, value);
    }

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public double YPixelSize
    {
        get => _yPixelSize;
        private set => SetProperty(ref _yPixelSize, value);
    }

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public int YPixel
    {
        get => _yPixel;
        private set => SetProperty(ref _yPixel, value);
    }

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public int OriginYPixel
    {
        get => _originYPixel;
        private set => SetProperty(ref _originYPixel, value);
    }

    /// <summary>
    /// KHz
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public double SampleRate
    {
        get => _sampleRate;
        private set => SetProperty(ref _sampleRate, value);
    }

    private ProductivityInformation()
    {
    }

    #region IEquatable、IComparable、IFormattable

    public int CompareTo(ProductivityInformation? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;

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

    public override int GetHashCode() => HashCode.Combine(OpticsMagType, StageSpeedType);

    public override string ToString() => ToString(null);

    public string ToString(string? format, IFormatProvider? formatProvider = null) => $"{Name}({((SxMAGEnum)OpticsMagType).ToString()[0]}-{((SxSpeedEnum)StageSpeedType).ToString()[0]})";

    #endregion IEquatable、IFormattable

    #region Operator

    public static bool operator ==(ProductivityInformation? left, ProductivityInformation? right) => (left, right) switch
    {
        (null, null) => true,
        (null, _) => false,
        (_, null) => false,
        (_, _) => ReferenceEquals(left, right) || (Equals(left.OpticsMagType, right.OpticsMagType) &&
                                                   Equals(left.StageSpeedType, right.StageSpeedType))
    };

    public static bool operator !=(ProductivityInformation? left, ProductivityInformation? right) => !(left == right);

    #endregion Operator

    #region Deconstruct

    public void Deconstruct(out string name, out int opticsMagType, out int stageSpeedType, out double xPixelSize, out double yPixelSize, out int yPixel, out double originYPixel, out double sampleRate)
        => (name, opticsMagType, stageSpeedType, xPixelSize, yPixelSize, yPixel, originYPixel, sampleRate) = (Name, OpticsMagType, StageSpeedType, XPixelSize, YPixelSize, YPixel, OriginYPixel, SampleRate);

    #endregion Deconstruct

    #region Mapper

    public C2MProductivityInfo AdaptTo() => new()
    {
        Name = Name,
        Mag = Enum.IsDefined(typeof(SxMAGEnum), OpticsMagType)
            ? (SxMAGEnum)OpticsMagType
            : ThrowHelper.ThrowArgumentOutOfRangeException<SxMAGEnum>(nameof(OpticsMagType)),
        Speed = Enum.IsDefined(typeof(SxSpeedEnum), StageSpeedType)
            ? (SxSpeedEnum)StageSpeedType
            : ThrowHelper.ThrowArgumentOutOfRangeException<SxSpeedEnum>(nameof(StageSpeedType))
    };

    public ProductivityInformation AdaptIn(C2MProductivityInfo obj, CgSwathSpeedInfo swathSpeedInfo, double originYPixel)
    {
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