using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Enums.Stage;
using Core.Models.Events;
using Core.Models.Models.Microscope.CalChip;
using Core.Recipe.Models;
using Core.Models;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Recipe.CalChip.Children;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;
using System.Windows.Controls;

namespace CugaCalibration.ViewModels.Common.Windows.Recipe.CalChip;

[IOCAppService(ServiceType = typeof(CalChipRecipeSettingViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CalChipRecipeSettingViewModel : ViewModelBase, IRecipient<ValueChangedMessage<ToggleCalChipRecipeEvent>>
{
    public CalChipWaferMapViewModel CalChipWaferMapViewModel { get; }
    public CalChipReticleMaskViewModel CalChipReticleMaskViewModel { get; }
    public CalChipAlignmentViewModel CalChipAlignmentViewModel { get; }

    private string TemplateFileDirectory => Path.Combine(_options.Value.AppHomeDirectory, "Template", "CalChipRecipe", CurrentCalChipSiteModelEnumName, DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    [ObservableProperty]
    private object? _selectedCalChipTab;

    [ObservableProperty]
    private object? _selectedOperationTab;

    [ObservableProperty]
    private int _selectedOperationTabIndex;

    [ObservableProperty]
    private CalChipSiteModelEnum _currentCalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;

    [ObservableProperty]
    private bool _isEditWaferMapEnable = true;

    [ObservableProperty]
    private CalChipRecipeDTO _editDTO = new();

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    [ObservableProperty]
    private bool _isLoading;

    private object? _lastSelectedTab;

    private string CurrentCalChipSiteModelEnumName => CurrentCalChipSiteModelEnum switch
    {
        CalChipSiteModelEnum.DswModel => "DSW",
        CalChipSiteModelEnum.HazeModel => "Haze",
        CalChipSiteModelEnum.ShinyWaferModel => "ShinyWafer",
        CalChipSiteModelEnum.UndefinedModel => "Undefined",
        _ => string.Empty
    };

    private readonly ICacheProvider _cacheProvider;
    private readonly IMessenger _messenger;
    private readonly ISynchronizationContextProvider _contextProvider;
    private readonly IOptions<ApplicationSetting> _options;
    private readonly IDialogWindowProvider _dialogWindowProvider;
    private readonly IApplicationCookieService _applicationCookieService;
    private readonly RecipeCookie _recipeCookie;
    private readonly StageViewModel _stageViewModel;

    public CalChipRecipeSettingViewModel(
        ICacheProvider cacheProvider,
        IMessenger messenger,
        ISynchronizationContextProvider contextProvider,
        IOptions<ApplicationSetting> options,
        IDialogWindowProvider dialogWindowProvider,
        IApplicationCookieService applicationCookieService,
        RecipeCookie recipeCookie,
        CalChipWaferMapViewModel calChipWaferMapViewModel,
        CalChipReticleMaskViewModel calChipReticleMaskViewModel,
        CalChipAlignmentViewModel calChipAlignmentViewModel,
        StageViewModel stageViewModel)
    {
        _cacheProvider = cacheProvider;
        _messenger = messenger;
        _contextProvider = contextProvider;
        _options = options;
        _dialogWindowProvider = dialogWindowProvider;
        _applicationCookieService = applicationCookieService;
        _recipeCookie = recipeCookie;
        CalChipWaferMapViewModel = calChipWaferMapViewModel;
        CalChipReticleMaskViewModel = calChipReticleMaskViewModel;
        CalChipAlignmentViewModel = calChipAlignmentViewModel;
        _stageViewModel = stageViewModel;

        _messenger.UnregisterAll(this);
        _messenger.RegisterAll(this);
    }


    [RelayCommand]
    private async Task LoadedAsync()
    {
        IsLoading = true;
        // await Task.Delay(50); // 让UI先渲染
        //
        // IsLoading = true;
        try
        {
            await Task.Yield(); // 让 UI 先显示

            // 克隆数据和初始化
            EditDTO = _recipeCookie.CalChipRecipeDTO.Clone();
            CurrentCalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
            EditDTO.CalChipSiteModelEnum = CurrentCalChipSiteModelEnum;

            CalChipReticleMaskViewModel.TemplateFileDirectory = TemplateFileDirectory;
            CalChipReticleMaskViewModel.CalChipWaferMapViewModel = CalChipWaferMapViewModel;

            // 后台执行耗时操作

            await Task.Run(() =>
            {
                CalChipWaferMapViewModel.Initialize(EditDTO);
                CalChipAlignmentViewModel.Initialize(EditDTO);
                CalChipReticleMaskViewModel.Initialize(EditDTO);
                MicroscopeCalChip = _applicationCookieService.GetCalibration<MicroscopeCalChipDTO>();
            });

            if (MicroscopeCalChip.IsOk == false)
            {
                _dialogWindowProvider.ShowDialog("Please calibrate CalChip first!", dialogButtonsEnum: DialogButtonsEnum.OK, dialogIconEnum: DialogIconEnum.Warning);
                CloseAction();
                return;
            }

            CalChipAlignmentViewModel.NotifyAll();
            CalChipReticleMaskViewModel.NotifyAll();

            // 延迟设置默认选中第一个 Tab，确保 UI 已加载
            await Task.Delay(50);
            _contextProvider.Post(() =>
            {
                IsEditWaferMapEnable = EditDTO.CurrentItem.CalChipMapDTO.AlignmentResultDto != null;

                //SelectedOperationTabIndex = -1; // 先设置为无效值
                SelectedOperationTabIndex = 0; // 然后设置为第一个
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CalChipSiteModelEnumChangedAsync(object obj)
    {
        if (obj is not TabItem tabItem) return;

        var newEnum = tabItem.Header.ToString() switch
        {
            "DSW" => CalChipSiteModelEnum.DswModel,
            "Haze" => CalChipSiteModelEnum.HazeModel,
            "ShinyWafer" => CalChipSiteModelEnum.ShinyWaferModel,
            "Undefined" => CalChipSiteModelEnum.UndefinedModel,
            _ => CalChipSiteModelEnum.DswModel
        };

        if (CurrentCalChipSiteModelEnum == newEnum) return;

        IsLoading = true; // 开始加载
        try
        {
            await Task.Yield(); // 让 UI 先更新

            CurrentCalChipSiteModelEnum = newEnum;
            EditDTO.CalChipSiteModelEnum = newEnum;
            CalChipReticleMaskViewModel.TemplateFileDirectory = TemplateFileDirectory;

            await Task.Run(() =>
            {
                CalChipWaferMapViewModel.Initialize(EditDTO);
                CalChipAlignmentViewModel.Initialize(EditDTO);
                CalChipReticleMaskViewModel.Initialize(EditDTO);
            });

            IsEditWaferMapEnable = EditDTO.CurrentItem.CalChipMapDTO.AlignmentResultDto != null;

            CalChipAlignmentViewModel.NotifyAll();
            CalChipReticleMaskViewModel.NotifyAll();

            // 延迟重置第二层 TabControl 选择，确保 UI 已更新完成
            await Task.Delay(50);
            _contextProvider.Post(() =>
            {
                //SelectedOperationTabIndex = -1; // 先设置为无效值触发变更
                SelectedOperationTabIndex = 0; // 然后设置为第一个 TabItem
            });
        }
        finally
        {
            IsLoading = false; // 结束加载
        }
    }

    partial void OnSelectedOperationTabChanged(object? value)
    {
        if (value == _lastSelectedTab || value is not TabItem tabItem)
            return;

        _lastSelectedTab = value;

        var header = tabItem.Header?.ToString();
        if (string.IsNullOrEmpty(header)) return;

        HandleTabChange(header!);
    }

    private void HandleTabChange(string header)
    {
        try
        {
            _contextProvider.Send(() => IsLoading = true);

            switch (header)
            {
                case "Alignment":
                    CalChipAlignmentViewModel.NotifyAll();
                    break;

                case "Reticle Mask":
                    CalChipReticleMaskViewModel.NotifyAll();
                    break;

                case "Wafer" when EditDTO.CurrentItem.AlignmentAbsoluteAngle is not null:
                    {
                        MicroscopeCalChip.CalChipSiteModelEnum = CurrentCalChipSiteModelEnum;
                        _stageViewModel.SetAbsoluteStageTheta(EditDTO.CurrentItem.AlignmentAbsoluteAngle!.Value);
                        _stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(
                            EditDTO.CurrentItem.CalChipMapDTO.WaferMapDataDTO.WaferCircleCenter,
                            CurrentCalChipSiteModelEnum);
                        CalChipWaferMapViewModel.NotifyAll();
                    }
                    break;
            }
        }
        finally
        {
            _contextProvider.Send(() => IsLoading = false);
        }
    }


    [RelayCommand]
    private void Save()
    {
        _cacheProvider.Set(EditDTO, CancellationToken.None);
        _dialogWindowProvider.TryShowDialog("Do you want to apply this recipe? ", out DialogResultEnum dialogResultEnum, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
        if (dialogResultEnum == DialogResultEnum.Yes) _recipeCookie.CalChipRecipeDTO.AdaptIn(EditDTO);
        CloseAction();
    }

    [RelayCommand]
    private void Close()
    {
        _dialogWindowProvider.TryShowDialog("Do you want to save changes?", out DialogResultEnum dialogResult, DialogButtonsEnum.YesNoCancel, DialogIconEnum.Question);
        if (dialogResult == DialogResultEnum.Yes) Save();
        else if (dialogResult == DialogResultEnum.No) CloseAction();
    }

    private void CloseAction()
    {
        CalChipAlignmentViewModel.Dispose();
        CalChipWaferMapViewModel.Dispose();
        CloseView(null);
        _messenger.Send(ToggleRecipeEventFactory.RefreshRecipeManagementView(true));
    }

    public void Receive(ValueChangedMessage<ToggleCalChipRecipeEvent> message)
    {
        _contextProvider.Post(() =>
        {
            if (message.Value.IsEnableWaferMapEdit.HasValue)
                IsEditWaferMapEnable = message.Value.IsEnableWaferMapEdit.Value;
        });
    }
}