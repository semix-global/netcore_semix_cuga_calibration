using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Events;
using Core.Models.Helper;
using Core.Recipe.Models;
using Core.Utilities;
using CugaCalibration.ViewModels.Common.Windows.Recipe.Edit.Children;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Controls;

namespace CugaCalibration.ViewModels.Common.Windows.Recipe.Edit;

[IOCAppService(ServiceType = typeof(RecipeSettingViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class RecipeSettingViewModel : ViewModelBase, IRecipient<ValueChangedMessage<ToggleRecipeEvent>>
{
    // 子 VM 对外暴露
    public RecipeCommonSettingViewModel RecipeCommonSettingViewModel { get; }
    public RecipeWaferMapViewModel RecipeWaferMapViewModel { get; }
    public RecipeReticleMaskViewModel RecipeReticleMaskViewModel { get; }
    public RecipeAlignmentViewModel RecipeAlignmentViewModel { get; }

    private string TemplateFileDirectory =>
        Path.Combine(
            _options.Value.AppHomeDirectory,
            "Template", "Recipe",
            EditRecipeTypeName,
            DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    [ObservableProperty]
    private object? _editRecipeTypeObj;

    private string EditRecipeTypeName
    {
        get
        {
            if (_editRecipeTypeObj is null || _editRecipeTypeObj is not TabItem tabItem)
                return string.Empty;
            return tabItem.Header.ToString();
        }
    }

    [ObservableProperty]
    private bool _isEditWaferMapEnable = true;

    [ObservableProperty]
    private CalibrationRecipeDTO _editDTO = new();

    private CancellationTokenSource? _cancellationTokenSource;
    private readonly ICacheProvider _recipeCacheProvider;
    private readonly IMessenger _messenger;
    private readonly ISynchronizationContextProvider _contextProvider;
    private readonly IOptions<ApplicationSetting> _options;
    private readonly IDialogWindowProvider _dialogWindowProvider;
    private readonly RecipeCookie _recipeCookie;

    public RecipeSettingViewModel(
        [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
        ICacheProvider recipeCacheProvider, IMessenger messenger,
        ISynchronizationContextProvider contextProvider,
        IOptions<ApplicationSetting> options,
        IDialogWindowProvider dialogWindowProvider,
        RecipeCookie recipeCookie,
        RecipeCommonSettingViewModel recipeCommonSettingViewModel,
        RecipeWaferMapViewModel recipeWaferMapViewModel,
        RecipeReticleMaskViewModel recipeReticleMaskViewModel,
        RecipeAlignmentViewModel recipeAlignmentViewModel)
    {
        _recipeCacheProvider = recipeCacheProvider;
        _messenger = messenger;
        _contextProvider = contextProvider;
        _options = options;
        _dialogWindowProvider = dialogWindowProvider;
        _recipeCookie = recipeCookie;
        RecipeCommonSettingViewModel = recipeCommonSettingViewModel;
        RecipeWaferMapViewModel = recipeWaferMapViewModel;
        RecipeReticleMaskViewModel = recipeReticleMaskViewModel;
        RecipeAlignmentViewModel = recipeAlignmentViewModel;

        _messenger.RegisterAll(this);
    }

    [MemberNotNull(nameof(_cancellationTokenSource))]
    private CancellationToken RefreshToken()
    {
        CancelToken();
        _cancellationTokenSource = new CancellationTokenSource();
        return _cancellationTokenSource.Token;
    }

    private void CancelToken()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    [RelayCommand]
    private async Task LoadedAsync()
    {
        EditDTO = _recipeCookie.CalibrationRecipeDto.Clone();

        // 初始化子 VM 间的互相引用
        RecipeReticleMaskViewModel.EditingDTO = EditDTO;
        RecipeReticleMaskViewModel.TemplateFileDirectory = TemplateFileDirectory;
        RecipeReticleMaskViewModel.RecipeWaferMapViewModel = RecipeWaferMapViewModel;

        // 初始化各子 VM
        RecipeCommonSettingViewModel.Initialize(EditDTO);
        RecipeWaferMapViewModel.Initialize(EditDTO, RefreshToken());
        await RecipeAlignmentViewModel.InitializeAsync(EditDTO);

        IsEditWaferMapEnable = EditDTO.WaferDTO.IsAlignmentResultLegal();
    }

    [RelayCommand]
    private void Save()
    {
        // 把最新 EditRecipeTypeName 同步给子 VM
        RecipeReticleMaskViewModel.EditRecipeTypeName = EditRecipeTypeName;
        RecipeReticleMaskViewModel.TemplateFileDirectory = TemplateFileDirectory;

        RecipeAlignmentViewModel.Save();

        _recipeCacheProvider.Set(EditDTO, CancellationToken.None);

        _dialogWindowProvider.TryShowDialog("Do you want to apply this recipe? ",
            out DialogResultEnum dialogResultEnum,
            DialogButtonsEnum.YesNo,
            DialogIconEnum.Question);

        if (dialogResultEnum == DialogResultEnum.Yes) _recipeCookie.CalibrationRecipeDto.AdaptIn(EditDTO);

        CloseAction();
    }

    [RelayCommand]
    private void Close()
    {
        CloseAction();
    }

    private void CloseAction()
    {
        CloseView(null);

        _messenger.Send(ToggleCalibrateEventFactory.RefreshWindow(true));
        _messenger.Send(ToggleRecipeEventFactory.RefreshRecipeManagementView(true));
        _messenger.Send(ToggleRecipeEventFactory.UpdateIsRecipeAlignment(false));

        CancelToken();
    }

    [RelayCommand]
    private void ParamTypeChanged(object obj)
    {
        if (EditRecipeTypeName.Contains("Reticle"))
        {
            RecipeReticleMaskViewModel.EditRecipeTypeName = EditRecipeTypeName;
            RecipeReticleMaskViewModel.ParamTypeChanged(obj);
        }

        // 切换到 Alignment Tab 时，UI 树刚创建，重新广播属性通知让 binding 刷新，不重新加载数据
        if (EditRecipeTypeName.Contains("Alignment"))
            RecipeAlignmentViewModel.NotifyAll();

        if (EditRecipeTypeName.Contains("Reticle"))
            RecipeReticleMaskViewModel.NotifyAll();
    }

    public void Receive(ValueChangedMessage<ToggleRecipeEvent> message)
    {
        _contextProvider.Post(() =>
        {
            if (message.Value.IsEnableWaferMapEdit.HasValue)
                IsEditWaferMapEnable = message.Value.IsEnableWaferMapEdit.Value;
        });
    }
}