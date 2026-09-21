#if NET48

// ReSharper disable once CheckNamespace
namespace System;

public static class DoubleExtensions
{
    extension(double)
    {
        public static bool IsFinite(double value)
        {
            return double.IsNaN(value) == false && double.IsInfinity(value) == false;
        }
    }
}

#endif