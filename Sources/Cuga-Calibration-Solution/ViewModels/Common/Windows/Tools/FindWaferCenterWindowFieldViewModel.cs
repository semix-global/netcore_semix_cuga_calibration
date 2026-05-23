using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.Alignment;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(FindWaferCenterWindowFieldViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class FindWaferCenterWindowFieldViewModel(
    IDialogWindowProvider dialogWindowProvider,
    FindWaferCenterByManuallyWindowViewModel findWaferCenterByManuallyWindowViewModel) : ViewModelBase
{
    public FindWaferCenterByManuallyWindowViewModel FindWaferCenterByManuallyWindowViewModel { get; } = findWaferCenterByManuallyWindowViewModel;

    #region 界面

    [ObservableProperty]
    public partial AlignmentFindCenterCache AlignmentFindCenterCache { get; set; } = new();

    [ObservableProperty]
    public partial bool IsFindWaferCenterOffsetPositionEnabled { get; set; } = true;

    #endregion 界面

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
            dialogWindowProvider.ShowDialog("Find Wafer Center failed.", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand]
    private void Close()
    {
        CloseView(true);
    }
}