using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Net.Utilities.WPF.MVVM.Events;

namespace Net.Utilities.WPF.MVVM.ViewModels.Bases;

public abstract partial class PopupWindowViewModelBase : ViewModelBase, IRecipient<PopupWindowEvent>
{
    private readonly ILogger<PopupWindowViewModelBase> _logger;

    protected CancellationTokenSource? CancellationTokenSource;

    [ObservableProperty]
    private bool _isEnable = true;

    protected PopupWindowViewModelBase(IMessenger messenger, ILogger<PopupWindowViewModelBase> logger)
    {
        messenger.RegisterAll(this);
        _logger = logger;
    }

    public bool Show()
    {
        var isShow = false;
        try
        {
            isShow = ShowView(false);
        }
        catch (Exception ex)
        {
            isShow = false;
            _logger.LogCritical(ex, "{@Name} show view is failed", nameof(PopupWindowViewModelBase));
        }

        if (isShow == false) CloseView(null);

        return isShow;
    }

    protected abstract void Loadeding(CancellationToken cancellationToken);

    [RelayCommand]
    private async Task LoadedAsync()
    {
        await Task.Run(() =>
        {
            CancelToken();
            CancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = CancellationTokenSource.Token;
            Loadeding(cancellationToken);
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private void Close()
    {
        try
        {
            CloseView(null);
        }
        finally
        {
            CancelToken();
        }
    }

    private void CancelToken()
    {
        CancellationTokenSource?.Cancel();
        CancellationTokenSource?.Dispose();

        CancellationTokenSource = null;
    }

    public virtual void Receive(PopupWindowEvent popupWindowEvent)
    {
        IsEnable = popupWindowEvent.IsPopupWindowEnable;
    }
}