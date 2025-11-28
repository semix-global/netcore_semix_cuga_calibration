using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using System.Globalization;

namespace Core.Models.Models.Common.Pattern;

public sealed class CIBInformation :
    ObservableObject,
    IComparable<CIBInformation>,
    IComparable,
    IEquatable<CIBInformation>,
    IFormattable,
    IAdaptIn<(int id, int chl, bool used), CIBInformation>,
    ICloneable<CIBInformation>
{
    public static readonly CIBInformation Default = new();

    private int _pMTId = -1;
    private int _channelId = -1;

    public int PMTId
    {
        get => _pMTId;
        private set => SetProperty(ref _pMTId, value);
    }

    public int ChannelId
    {
        get => _channelId;
        private set => SetProperty(ref _channelId, value);
    }

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

    public override int GetHashCode() => HashCode.Combine(PMTId, ChannelId);

    public override string ToString() => ToString(null);

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.CurrentCulture;

        return $"{PMTId.ToString(format, formatProvider)}-{ChannelId.ToString(format, formatProvider)}";
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

    public CIBInformation Clone() => new()
    {
        PMTId = PMTId,
        ChannelId = ChannelId
    };

    #endregion Mapper
}