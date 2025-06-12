using MathNet.Numerics;

namespace Net.Utilities.Extensions;

public static class GenerateHelper
{
    public static int[] LinearIndexRange(int start, int stop)
    {
        return stop < start ? [] : Generate.LinearRangeInt32(start, stop);
    }

    public static int[] LinearIndexRange(int start, int step, int stop)
    {
        return stop < start ? [] : Generate.LinearRangeInt32(start, step, stop);
    }
}