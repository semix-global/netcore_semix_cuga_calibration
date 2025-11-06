using CommunityToolkit.Diagnostics;
using Cuga.Data.DataStruct.DTO.Swath;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;


namespace Core.Models.Models.Common.Pattern
{
    public sealed class SwathSpeedInformation :
        ObservableCacheBase,
        IAdaptIn<CgSwathSpeedInfo, SwathSpeedInformation>,
        IComparable,
        IComparable<SwathSpeedInformation>,
        IEquatable<SwathSpeedInformation>,
        ICloneable<SwathSpeedInformation>
    {
        public static readonly SwathSpeedInformation Default = new();

        private double _yPixelSize = -1;
        private double _yPixel = -1;

        public double YPixelSize
        {
            get => _yPixelSize;
            init => SetProperty(ref _yPixelSize, value);
        }

        public double YPixel
        {
            get => _yPixel;
            init => SetProperty(ref _yPixel, value);
        }

        public SwathSpeedInformation()
        {
        }

        #region IEquatable、IComparable

        public int CompareTo(SwathSpeedInformation? other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;

            var yPixelSizeComparison = YPixelSize.CompareTo(other.YPixelSize);
            if (yPixelSizeComparison != 0) return yPixelSizeComparison;

            var yPixelComparison = YPixel.CompareTo(other.YPixel);
            if (yPixelComparison != 0) return yPixelComparison;

            return 1;
        }

        public int CompareTo(object? obj)
        {
            if (obj is null) return 1;

            return obj is ProductivityInformation other ? CompareTo(other) : ThrowHelper.ThrowArgumentException<int>($"Object must be of type {nameof(SwathSpeedInformation)}. ");
        }

        public bool Equals(SwathSpeedInformation? other) => this == other;

        public override bool Equals(object? obj) => obj is SwathSpeedInformation other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(YPixelSize, YPixel);


        #endregion IEquatable

        #region Operator

        public static bool operator ==(SwathSpeedInformation? left, SwathSpeedInformation? right) => (left, right) switch
        {
            (null, null) => true,
            (null, _) => false,
            (_, null) => false,
            (_, _) => ReferenceEquals(left, right) || (Equals(left.YPixelSize, right.YPixelSize) &&
                                                       Equals(left.YPixel, right.YPixel))
        };

        public static bool operator !=(SwathSpeedInformation? left, SwathSpeedInformation? right) => !(left == right);

        #endregion Operator

        public SwathSpeedInformation AdaptIn(CgSwathSpeedInfo obj)
        {
            Guard.IsNotNull(obj, nameof(obj));

            return new()
            {
                YPixelSize = obj.YPixelSize,
                YPixel = obj.YPixel
            };
        }


        public SwathSpeedInformation Clone() => new()
        {
            YPixelSize = YPixelSize,
            YPixel = YPixel
        };

    }
}
