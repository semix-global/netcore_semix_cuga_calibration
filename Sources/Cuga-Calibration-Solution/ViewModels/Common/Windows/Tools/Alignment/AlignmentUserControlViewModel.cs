using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Enums.Stage;
using Core.Models.Events;
using Core.Models.Helper;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.MVVM.Events;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;

[IOCAppService(ServiceType = typeof(AlignmentUserControlViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AlignmentUserControlViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial AlignmentCacheBrightField AlignmentCacheBrightField { get; set; } = new();

    [DefaultCache]
    [RecipeCache]
    [ObservableProperty]
    public partial AlignmentCacheBrightField[] AlignmentCacheBrightFields { get; set; } = [];

    [ObservableProperty]
    public partial AlignmentCacheDarkField AlignmentCacheDarkField { get; set; } = new();

    [RecipeCache]
    [ObservableProperty]
    public partial AlignmentCacheDarkField[] AlignmentCacheDarkFields { get; set; } = [];

    [ObservableProperty]
    public partial bool IsDarkFieldAlignment { get; set; }

    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; }

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

    private readonly IDialogWindowProvider _dialogWindowProvider;
    private readonly IWindowManagerService _windowManagerService;
    private readonly StageViewModel _stageViewModel;
    private readonly AlignmentWindowBrightFieldViewModel _alignmentWindowBrightFieldViewModel;
    private readonly AlignmentWindowDarkFieldViewModel _alignmentWindowDarkFieldViewModel;
    private readonly ICacheProvider _cacheProvider;
    private readonly ICacheProvider _recipeCacheProvider;
    private readonly IMessenger _messenger;

    public AlignmentUserControlViewModel(IDialogWindowProvider dialogWindowProvider,
        IWindowManagerService windowManagerService,
        StageViewModel stageViewModel,
        AlignmentWindowBrightFieldViewModel alignmentWindowBrightFieldViewModel,
        AlignmentWindowDarkFieldViewModel alignmentWindowDarkFieldViewModel,
        ICacheProvider cacheProvider,
        [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
        ICacheProvider recipeCacheProvider,
        IMessenger messenger)
    {
        _dialogWindowProvider = dialogWindowProvider;
        _windowManagerService = windowManagerService;
        _stageViewModel = stageViewModel;
        _alignmentWindowBrightFieldViewModel = alignmentWindowBrightFieldViewModel;
        _alignmentWindowDarkFieldViewModel = alignmentWindowDarkFieldViewModel;
        _cacheProvider = cacheProvider;
        _recipeCacheProvider = recipeCacheProvider;
        _messenger = messenger;

        _messenger.RegisterAll(this);
    }

    public async Task AlignmentAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            try
            {
                _messenger.Send(ToggleToolsEventFactory.RefreshToolsWindowEnableStatus(false));

                AlignmentResultDto alignmentResult;
                if (IsDarkFieldAlignment)
                {
                    AlignmentCacheDarkFields = _recipeCacheProvider.GetOrDefaultArray<AlignmentCacheDarkField>();

                    AlignmentCacheDarkField = AlignmentCacheDarkFields.SingleOrDefault(t =>
                       t.ProductivityInformation == ProductivityInformation
                        , new AlignmentCacheDarkField
                        {
                            ProductivityInformation = ProductivityInformation
                        });

                    if (AlignmentCacheDarkField.IsOk == false)
                    {
                        // 校准期间 UpdateDisableAll 已将其 IsEnable 置 false, 此处必须恢复, 否则窗口内容灰化无法操作
                        _messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());

                        _alignmentWindowDarkFieldViewModel.Cache = AlignmentCacheDarkField;

                        Guard.IsTrue(_windowManagerService.ShowDialog(_alignmentWindowDarkFieldViewModel) == true, nameof(_alignmentWindowDarkFieldViewModel));

                        AlignmentCacheDarkFields = _recipeCacheProvider.GetOrDefaultArray<AlignmentCacheDarkField>();
                        AlignmentCacheDarkField = AlignmentCacheDarkFields.Single(t =>
                            t.ProductivityInformation == ProductivityInformation);
                    }

                    alignmentResult = _stageViewModel.AlignmentDarkField(
                        AlignmentCacheDarkField.LowSite1,
                        AlignmentCacheDarkField.LowSite2,
                        AlignmentCacheDarkField.HighSite1,
                        AlignmentCacheDarkField.HighSite2,
                        ProductivityInformation,
                        AlignmentCacheDarkField.LowMag);
                }
                else
                {
                    AlignmentCacheBrightFields = [.. _recipeCacheProvider.GetOrDefaultArray<AlignmentCacheBrightField>(), .. _cacheProvider.GetOrDefaultArray<AlignmentCacheBrightField>()];
                    AlignmentCacheBrightField = AlignmentCacheBrightFields
                        .SingleOrDefault(t => t.CalChipSiteModelEnum == CalChipSiteModelEnum
                            , new AlignmentCacheBrightField
                            {
                                CalChipSiteModelEnum = CalChipSiteModelEnum
                            });

                    if (AlignmentCacheBrightField.IsOk == false)
                    {
                        // 校准期间 UpdateDisableAll 已将其 IsEnable 置 false, 此处必须恢复, 否则窗口内容灰化无法操作
                        _messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());

                        _alignmentWindowBrightFieldViewModel.Cache = AlignmentCacheBrightField;
                        Guard.IsTrue(_windowManagerService.ShowDialog(_alignmentWindowBrightFieldViewModel) == true,
                            nameof(_alignmentWindowBrightFieldViewModel));

                        AlignmentCacheBrightFields = [.. _recipeCacheProvider.GetOrDefaultArray<AlignmentCacheBrightField>(), .. _cacheProvider.GetOrDefaultArray<AlignmentCacheBrightField>()];
                        AlignmentCacheBrightField = AlignmentCacheBrightFields.Single(t => t.CalChipSiteModelEnum == CalChipSiteModelEnum);
                    }

                    alignmentResult = _stageViewModel.Alignment(
                        AlignmentCacheBrightField.LowSite1,
                        AlignmentCacheBrightField.LowSite2,
                        AlignmentCacheBrightField.HighSite1,
                        AlignmentCacheBrightField.HighSite2,
                        AlignmentCacheBrightField.LowMag,
                        AlignmentCacheBrightField.HighMag,
                        CalChipSiteModelEnum);
                }

                AlignmentResult = alignmentResult;
            }
            finally
            {
                _messenger.Send(ToggleToolsEventFactory.RefreshToolsWindowEnableStatus(true));
            }
        }, cancellationToken);
    }
}