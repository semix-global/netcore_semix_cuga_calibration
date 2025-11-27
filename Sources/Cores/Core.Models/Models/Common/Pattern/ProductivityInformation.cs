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
    private double _yPixelSize = -1;
    private double _yPixel = -1;
    private double _sampleRate = -1;

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

    public double YPixelSize
    {
        get => _yPixelSize;
        private set => SetProperty(ref _yPixelSize, value);
    }

    public double YPixel
    {
        get => _yPixel;
        private set => SetProperty(ref _yPixel, value);
    }

    /// <summary>
    /// KHz
    /// </summary>
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

    public override int GetHashCode() => HashCode.Combine(Name, OpticsMagType, StageSpeedType, YPixelSize, YPixel, SampleRate);

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

    public void Deconstruct(out string name, out int opticsMagType, out int stageSpeedType, out double yPixelSize, out double yPixel, out double sampleRate)
        => (name, opticsMagType, stageSpeedType, yPixelSize, yPixel, sampleRate) = (Name, OpticsMagType, StageSpeedType, YPixelSize, YPixel, SampleRate);

    #endregion Deconstruct

    #region Mapper

    public C2MProductivityInfo AdaptTo() => this != Default
        ? new C2MProductivityInfo
        {
            Name = Name,
            Mag = Enum.IsDefined(typeof(SxMAGEnum), OpticsMagType)
                ? (SxMAGEnum)OpticsMagType
                : ThrowHelper.ThrowArgumentOutOfRangeException<SxMAGEnum>(nameof(OpticsMagType)),
            Speed = Enum.IsDefined(typeof(SxSpeedEnum), StageSpeedType)
                ? (SxSpeedEnum)StageSpeedType
                : ThrowHelper.ThrowArgumentOutOfRangeException<SxSpeedEnum>(nameof(StageSpeedType))
        }
        : new C2MProductivityInfo
        {
            Name = Name,
            Mag = (SxMAGEnum)OpticsMagType,
            Speed = (SxSpeedEnum)StageSpeedType
        };

    public ProductivityInformation AdaptIn(C2MProductivityInfo obj, CgSwathSpeedInfo swathSpeedInfo)
    {
        Guard.IsTrue(obj.Mag.ToCgMagTypeEnum() == swathSpeedInfo.Mag);

        Name = obj.Name;
        OpticsMagType = (int)obj.Mag;
        StageSpeedType = (int)obj.Speed;
        YPixelSize = swathSpeedInfo.YPixelSize;
        YPixel = swathSpeedInfo.YPixel;
        SampleRate = swathSpeedInfo.Hz;

        return this;
    }

    public ProductivityInformation Clone() => new()
    {
        Name = Name,
        OpticsMagType = OpticsMagType,
        StageSpeedType = StageSpeedType,
        YPixelSize = YPixelSize,
        YPixel = YPixel,
        SampleRate = SampleRate
    };

    #endregion Mapper
}