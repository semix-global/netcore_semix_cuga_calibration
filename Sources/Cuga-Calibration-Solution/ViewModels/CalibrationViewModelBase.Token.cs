using System.Diagnostics.CodeAnalysis;

namespace CugaCalibration.ViewModels;

public partial class CalibrationViewModelBase : IDisposable
{
    protected CancellationTokenSource? _cancellationTokenSource;

    private void CancelToken()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    [MemberNotNull(nameof(_cancellationTokenSource))]
    private void RefreshToken()
    {
        CancelToken();

#pragma warning disable IDE0079
#pragma warning disable IDISP003

        _cancellationTokenSource = new CancellationTokenSource();

#pragma warning restore IDISP003
#pragma warning restore IDE0079
    }

    public virtual void Dispose()
    {
        CancelToken();

        GC.SuppressFinalize(this);
    }
}