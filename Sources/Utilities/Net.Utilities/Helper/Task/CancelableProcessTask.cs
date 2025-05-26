using System.Diagnostics;

namespace Net.Utilities.Helper.Task;

public sealed class CancelableProcessTask(string filename, string arguments) : IDisposable
{
    private Process? _process;

    private int _isRunning;

    public Task<bool> RunAsync(CancellationToken token)
    {
        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 1)
            throw new InvalidOperationException("Task is already running");

        var tcs = new TaskCompletionSource<bool>();

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = filename,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        _process.Start();
        _process.EnableRaisingEvents = true;
        _process.Exited += (_, _) =>
        {
            if (_process.ExitCode == 0)
                tcs.SetResult(true);
            else
            {
                if (token.IsCancellationRequested)
                    tcs.TrySetCanceled(token);
                else
                    tcs.TrySetException(new Exception($"Process exited with code {_process.ExitCode}"));
            }
        };

        token.Register(() =>
        {
            if (Interlocked.CompareExchange(ref _isRunning, 0, 1) == 1) _process.Kill();
        });

        return tcs.Task;
    }

    public void Dispose()
    {
        _process?.Dispose();
    }
}