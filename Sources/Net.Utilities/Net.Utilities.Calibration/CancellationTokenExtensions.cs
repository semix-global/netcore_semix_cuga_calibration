namespace Net.Utilities.Calibration;

public static class CancellationTokenExtensions
{
    extension(CancellationToken @this)
    {
        public async Task WaitUntilCanceledAsync()
        {
            try
            {
                await Task.Delay(Timeout.Infinite, @this);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}