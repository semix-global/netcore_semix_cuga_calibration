namespace Net.Utilities.Helper.Task;

public sealed class CancelableThreadTask(Action action, Action<Exception>? onError = null, Action? onCompleted = null)
{
    private Thread? _thread;
    private readonly Action _action = action ?? throw new ArgumentNullException(nameof(action));

    private int _isRunning;

    public Task<bool> RunAsync(CancellationToken token)
    {
        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 1)
            throw new InvalidOperationException("Task is already running");

        var tcs = new TaskCompletionSource<bool>();

        _thread = new Thread(() =>
        {
            try
            {
                _action.Invoke();
                tcs.SetResult(true);
                onCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                if (ex is ThreadInterruptedException)
                    tcs.TrySetCanceled(token);
                else
                    tcs.TrySetException(ex);
                onError?.Invoke(ex);
            }
            finally
            {
                Interlocked.Exchange(ref _isRunning, 0);
            }
        });

        token.Register(() =>
        {
            if (Interlocked.CompareExchange(ref _isRunning, 0, 1) != 1) return;

            _thread.Interrupt();
            _thread.Join();
        });

        _thread.Start();

        return tcs.Task;
    }
}