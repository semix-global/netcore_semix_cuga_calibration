using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.Alignment;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.IOC.Providers;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;


namespace CugaCalibration.ViewModels.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(FindWaferCenterWindowFieldViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class FindWaferCenterWindowFieldViewModel : ViewModelBase
{
    private readonly ISynchronizationContextProvider _contextProvider;
    private readonly ILogger<FindWaferCenterWindowFieldViewModel> _logger;
    private readonly IDialogWindowProvider _dialogWindowProvider;
    public FindWaferCenterByManuallyWindowViewModel FindWaferCenterByManuallyWindowViewModel { get; }

    #region 界面

    [ObservableProperty]
    private AlignmentFindCenterCache _alignmentFindCenterCache = new();

    [ObservableProperty]
    private bool isFindWaferCenterOffsetPositionEnabled = true;

    #endregion 界面

    public FindWaferCenterWindowFieldViewModel(
        IDialogWindowProvider dialogWindowProvider,
        ILogger<FindWaferCenterWindowFieldViewModel> logger,
        ISynchronizationContextProvider contextProvider,
        FindWaferCenterByManuallyWindowViewModel findWaferCenterByManuallyWindowViewModel)
    {
        _dialogWindowProvider = dialogWindowProvider;
        _logger = logger;
        _contextProvider = contextProvider;

        FindWaferCenterByManuallyWindowViewModel = findWaferCenterByManuallyWindowViewModel;
    }

    [RelayCommand]
    private void Loaded()
    {
        FindWaferCenterByManuallyWindowViewModel.AlignmentFindCenterCache = AlignmentFindCenterCache;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ActionAsync(CancellationToken cancellationToken)
    {
        IsFindWaferCenterOffsetPositionEnabled = true;
        var result = await FindWaferCenterByManuallyWindowViewModel.ActionAsync(cancellationToken).ConfigureAwait(false);
        if (result) AlignmentFindCenterCache = FindWaferCenterByManuallyWindowViewModel.Cache;
        else
        {
            IsFindWaferCenterOffsetPositionEnabled = false;
            _dialogWindowProvider.ShowDialog("Find Wafer Center failed.", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand]
    private void Close()
    {
        CloseView(true);
    }
}