namespace Net.Utilities.Helper.IOC.Providers.Impl;

public sealed class SynchronizationContextProvider(SynchronizationContext synchronizationContext) : ISynchronizationContextProvider
{
    public void Send(Action d)
    {
        synchronizationContext.Send(_ => d.Invoke(), null);
    }

    public void Post(Action d)
    {
        synchronizationContext.Post(_ => d.Invoke(), null);
    }

    public void Send(Action<object?> d, object? state)
    {
        synchronizationContext.Send(d.Invoke, state);
    }

    public void Post(Action<object?> d, object? state)
    {
        synchronizationContext.Post(d.Invoke, state);
    }
}