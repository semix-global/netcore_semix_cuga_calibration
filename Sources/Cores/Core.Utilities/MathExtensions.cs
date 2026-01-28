#if !NETCOREAPP

using CommunityToolkit.Diagnostics;
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace System;

public static class MathExtensions
{
    extension(Math)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Clamp(byte value, byte min, byte max)
        {
            Guard.IsLessThanOrEqualTo(min, max);

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Clamp(decimal value, decimal min, decimal max)
        {
            Guard.IsLessThanOrEqualTo(min, max);

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(double value, double min, double max)
        {
            Guard.IsLessThanOrEqualTo(min, max);

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Clamp(short value, short min, short max)
        {
            Guard.IsLessThanOrEqualTo(min, max);

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Clamp(int value, int min, int max)
        {
            Guard.IsLessThanOrEqualTo(min, max);

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Clamp(long value, long min, long max)
        {
            Guard.IsLessThanOrEqualTo(min, max);

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        /// <summary>Returns <paramref name="value" /> clamped to the inclusive range of <paramref name="min" /> and <paramref name="max" />.</summary>
        /// <param name="value">The value to be clamped.</param>
        /// <param name="min">The lower bound of the result.</param>
        /// <param name="max">The upper bound of the result.</param>
        /// <returns>
        ///   <paramref name="value" /> if <paramref name="min" /> \u2264 <paramref name="value" /> \u2264 <paramref name="max" />.
        ///
        ///   -or-
        ///
        ///   <paramref name="min" /> if <paramref name="value" /> &lt; <paramref name="min" />.
        ///
        ///   -or-
        ///
        ///   <paramref name="max" /> if <paramref name="max" /> &lt; <paramref name="value" />.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint Clamp(nint value, nint min, nint max)
        {
            Guard.IsLessThanOrEqualTo(min, max);

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte Clamp(sbyte value, sbyte min, sbyte max)
        {
            Guard.IsLessThanOrEqualTo(min, max);

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float value, float min, float max)
        {
            Guard.IsLessThanOrEqualTo(min, max);

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Clamp(ushort value, ushort min, ushort max)
        {
            Guard.IsLessThanOrEqualTo(min, max);

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static uint Clamp(uint value, uint min, uint max)
        {
            Guard.IsLessThanOrEqualTo(min, max);

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static ulong Clamp(ulong value, ulong min, ulong max)
        {
            Guard.IsLessThanOrEqualTo(min, max);

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        /// <summary>Returns <paramref name="value" /> clamped to the inclusive range of <paramref name="min" /> and <paramref name="max" />.</summary>
        /// <param name="value">The value to be clamped.</param>
        /// <param name="min">The lower bound of the result.</param>
        /// <param name="max">The upper bound of the result.</param>
        /// <returns>
        ///   <paramref name="value" /> if <paramref name="min" /> \u2264 <paramref name="value" /> \u2264 <paramref name="max" />.
        ///
        ///   -or-
        ///
        ///   <paramref name="min" /> if <paramref name="value" /> &lt; <paramref name="min" />.
        ///
        ///   -or-
        ///
        ///   <paramref name="max" /> if <paramref name="max" /> &lt; <paramref name="value" />.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static nuint Clamp(nuint value, nuint min, nuint max)
        {
            Guard.IsLessThanOrEqualTo(min, max);

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}

#endif