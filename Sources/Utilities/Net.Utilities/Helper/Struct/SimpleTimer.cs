using System.Diagnostics;

namespace Net.Utilities.Helper.Struct;

public readonly ref struct SimpleTimer(Action<TimeSpan> callback)
{
#if NET
    private readonly long _start = Stopwatch.GetTimestamp();

    public void Dispose()
    {
        callback.Invoke(Stopwatch.GetElapsedTime(_start));
    }
#else
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public void Dispose()
    {
        _stopwatch.Stop();
        callback.Invoke(_stopwatch.Elapsed);
    }

#endif
}