using CommunityToolkit.Diagnostics;
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
    IEquatable<ProductivityInformation>,
    IFormattable,
    IAdaptTo<C2MProductivityInfo>,
    IAdaptIn<C2MProductivityInfo, ProductivityInformation>,
    ICloneable<ProductivityInformation>
{
    
    public static readonly ProductivityInformation Default = new();
    
    private string _name = "N/A";
    private int _opticsMagType = -1;
    private int _stageSpeedType = -1;

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

    private ProductivityInformation()
    {
    }

    #region IEquatable、IFormattable

    public bool Equals(ProductivityInformation? other) => this == other;

    public override bool Equals(object? obj) => obj is ProductivityInformation other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Name, OpticsMagType, StageSpeedType);

    public override string ToString() => ToString(null);

    public string ToString(string? format, IFormatProvider? formatProvider = null) => Name;

    #endregion IEquatable、IFormattable

    #region Operator

    public static bool operator ==(ProductivityInformation? left, ProductivityInformation? right) => (left, right) switch
    {
        (null, null) => true,
        (null, _) => false,
        (_, null) => false,
        (_, _) => ReferenceEquals(left, right) || (Equals(left.Name, right.Name) &&
                                                   Equals(left.OpticsMagType, right.OpticsMagType) &&
                                                   Equals(left.StageSpeedType, right.StageSpeedType))
    };

    public static bool operator !=(ProductivityInformation? left, ProductivityInformation? right) => !(left == right);

    #endregion Operator

    #region Deconstruct

    public void Deconstruct(out string name, out int opticsMagType, out int stageSpeedType) => (name, opticsMagType, stageSpeedType) = (Name, OpticsMagType, StageSpeedType);

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
            : ThrowHelper.ThrowArgumentOutOfRangeException<SxSpeedEnum>(nameof(StageSpeedType)),
    };

    public ProductivityInformation AdaptIn(C2MProductivityInfo obj)
    {
        Guard.IsNotNull(obj, nameof(obj));

        Name = obj.Name;
        OpticsMagType = (int)obj.Mag;
        StageSpeedType = (int)obj.Speed;

        return this;
    }

    public ProductivityInformation Clone() => new()
    {
        Name = Name,
        OpticsMagType = OpticsMagType,
        StageSpeedType = StageSpeedType
    };

    #endregion Mapper
}