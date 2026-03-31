using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Events;
using Core.Models.Helper;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Microscope.PixelSize;
using Core.Models.Models.Setting;
using Core.Recipe.Models;
using Core.Utilities;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Recipe.Edit.Children;
using Local.SQL.Cache.Providers.Extensions;
using Local.SQL.Cache.Providers.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
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

    public ApplicationCookie ApplicationCookie => _applicationCookie;

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
    private CalibrationRecipeDTO _editDto = new();


    private CancellationTokenSource? _cancellationTokenSource;
    private readonly ICacheProvider _cacheProvider;
    private readonly ICacheProvider _recipeCacheProvider;
    private readonly IMessenger _messenger;
    private readonly ISynchronizationContextProvider _contextProvider;
    private readonly ICalibrationRecipeService _calibrationRecipeService;
    private readonly IOptions<ApplicationSetting> _options;
    private readonly StageViewModel _stageViewModel;
    private readonly MicroscopeViewModel _microscopeViewModel;
    private readonly ReviewViewModel _reviewViewModel;
    private readonly CalibrationSetting _calibrationSetting;
    private readonly ApplicationCookie _applicationCookie;
    private readonly RecipeCookie _recipeCookie;

    /// <inheritdoc/>
    public RecipeSettingViewModel(ICacheProvider cacheProvider,
        [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
        ICacheProvider recipeCacheProvider,
        IMessenger messenger,
        ISynchronizationContextProvider contextProvider,
        ICalibrationRecipeService calibrationRecipeService,
        IOptions<ApplicationSetting> options,
        StageViewModel stageViewModel,
        MicroscopeViewModel microscopeViewModel,
        ReviewViewModel reviewViewModel,
        CalibrationSetting calibrationSetting,
        ApplicationCookie applicationCookie,
        RecipeCookie recipeCookie,
        RecipeCommonSettingViewModel recipeCommonSettingViewModel,
        RecipeWaferMapViewModel recipeWaferMapViewModel,
        RecipeReticleMaskViewModel recipeReticleMaskViewModel,
        RecipeAlignmentViewModel recipeAlignmentViewModel)
    {
        _cacheProvider = cacheProvider;
        _recipeCacheProvider = recipeCacheProvider;
        _messenger = messenger;
        _contextProvider = contextProvider;
        _calibrationRecipeService = calibrationRecipeService;
        _options = options;
        _stageViewModel = stageViewModel;
        _microscopeViewModel = microscopeViewModel;
        _reviewViewModel = reviewViewModel;
        _calibrationSetting = calibrationSetting;
        _applicationCookie = applicationCookie;
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
        EditDto = _recipeCookie.CalibrationRecipeDto.Clone();

        // 初始化子 VM 间的互相引用
        RecipeReticleMaskViewModel.EditingDto = EditDto;
        RecipeReticleMaskViewModel.TemplateFileDirectory = TemplateFileDirectory;
        RecipeReticleMaskViewModel.WaferMapSubViewModel = RecipeWaferMapViewModel;

        // 初始化各子 VM
        RecipeCommonSettingViewModel.Initialize(EditDto);
        RecipeWaferMapViewModel.Initialize(EditDto, RefreshToken());
        await RecipeAlignmentViewModel.InitializeAsync(EditDto);

        IsEditWaferMapEnable = EditDto.WaferDto.RequireActionIsOk();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        // 把最新 EditRecipeTypeName 同步给子 VM
        RecipeReticleMaskViewModel.EditRecipeTypeName = EditRecipeTypeName;
        RecipeReticleMaskViewModel.TemplateFileDirectory = TemplateFileDirectory;

        RecipeAlignmentViewModel.Save();
        await RecipeCommonSettingViewModel.SaveAsync(CloseAction);
    }

    [RelayCommand]
    private void Close()
    {
        CloseAction();
    }

    private void CloseAction()
    {
        CloseView(null);
        _messenger.Send(ToggleRecipeEventFactory.RefreshRecipeManagementView(true));
        CancelToken();
    }

    [RelayCommand]
    private async Task RefreshWaferMapAsync(object? name)
    {
        await RecipeWaferMapViewModel.RefreshWaferMapAsync(name);
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

    [RelayCommand]
    private void Debug()
    {
        var dto = Guard.IsNotNullAndReturn(RecipeWaferMapViewModel.EditingDto);
        var document = RecipeWaferMapViewModel.WaferMapCanvasViewModel.Document;
        var waferCenterBrightFieldPosition = dto.WaferDto.WaferCenterWaferPosition!.Value;
        var originReticleWaferPosition = document.ReticleBuilder.OriginalDiePoint;

        var alignmentCacheBrightField = _recipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();
        var alignmentResult = _stageViewModel.Alignment(
            alignmentCacheBrightField.LowSite1, alignmentCacheBrightField.LowSite2,
            alignmentCacheBrightField.HighSite1, alignmentCacheBrightField.HighSite2,
            alignmentCacheBrightField.LowMag, alignmentCacheBrightField.HighMag,
            alignmentCacheBrightField.AlgorithmWaferTypeEnum);

        var recipeAlignmentResult = dto.WaferDto.AlignmentResultDto;
        var offsetX = ((alignmentResult.MarkPoint1.X - recipeAlignmentResult!.MarkPoint1.X) +
                       (alignmentResult.MarkPoint2.X - recipeAlignmentResult.MarkPoint2.X)) / 2;
        var offsetY = ((alignmentResult.MarkPoint1.Y - recipeAlignmentResult.MarkPoint1.Y) +
                       (alignmentResult.MarkPoint2.Y - recipeAlignmentResult.MarkPoint2.Y)) / 2;
        var offsetPosition = new Point(offsetX, offsetY);

        _stageViewModel.SetBrightFieldAbsoluteStageXy(originReticleWaferPosition);
        var ideaOriginReticleBrightPosition = originReticleWaferPosition
                                              + (Vector)waferCenterBrightFieldPosition
                                              + (Vector)offsetPosition;
        _stageViewModel.SetBrightFieldAbsoluteStageXy(ideaOriginReticleBrightPosition);

        var ideaReticleDieCornerWaferPosition = document.ReticleModel.Single(t => t.Index is { X: 4, Y: 4 });
        var realReticleDieCornerBrightFieldPosition = ideaReticleDieCornerWaferPosition.Rect.Point
                                                      + (Vector)waferCenterBrightFieldPosition
                                                      + (Vector)offsetPosition;
        _stageViewModel.SetBrightFieldAbsoluteStageXy(realReticleDieCornerBrightFieldPosition);

        var reticleBuilder = document.ReticleBuilder;
        var reticleHeight = reticleBuilder.DiePitchSize.Height;
        var selectItem = RecipeReticleMaskViewModel.SelectReticleMarkItem!;
        var ideaReticleMaskPosition = selectItem.MaskWaferCellPosition;

        var realReticleMaskBrightFieldPosition = ideaReticleMaskPosition
                                                 + (Vector)offsetPosition
                                                 + (Vector)waferCenterBrightFieldPosition
                                                 + (originReticleWaferPosition - new Point(0, reticleHeight));
        _microscopeViewModel.SwitchMicroscopeLensInformation(selectItem.RecipeBrightFieldTemplateDto.MicroscopeLensInformation);
        _stageViewModel.SetBrightFieldAbsoluteStageXy(realReticleMaskBrightFieldPosition);

        var maskWaferPosition = ideaReticleMaskPosition +
                                (ideaReticleDieCornerWaferPosition.Rect.Point - new Point(0, reticleHeight));
        var realMaskBrightFieldPosition = maskWaferPosition + (Vector)offsetPosition + (Vector)waferCenterBrightFieldPosition;
        _stageViewModel.SetBrightFieldAbsoluteStageXy(realMaskBrightFieldPosition);
    }

    [RelayCommand]
    private void Verify()
    {
        _recipeCookie.CalibrationReviseRecipeDto = _calibrationRecipeService.GetCorrectWaferMapByOffset(_recipeCookie.CalibrationRecipeDto, true);
        var reviseRecipeDto = _recipeCookie.CalibrationReviseRecipeDto;

        _microscopeViewModel.SwitchMicroscopeLensInformation(_calibrationSetting.SettingCommonParam.HighMicroscopeLensInformation);

        var originDieDto = reviseRecipeDto.WaferDto.WaferMapCanvasDocument.DieBuilder.OriginalDiePoint;
        _stageViewModel.SetBrightFieldAbsoluteStageXy(originDieDto + (Vector)reviseRecipeDto.WaferDto.WaferCenterWaferPosition);

        var originReticleDto = reviseRecipeDto.WaferDto.WaferMapCanvasDocument.ReticleBuilder.OriginalDiePoint;
        _stageViewModel.SetBrightFieldAbsoluteStageXy(originReticleDto + (Vector)reviseRecipeDto.WaferDto.WaferCenterWaferPosition);

        var diePitchDto = reviseRecipeDto.WaferDto.WaferMapCanvasDocument.DieModel.Single(t => t.Index is { X: 12, Y: 4 });
        _stageViewModel.SetBrightFieldAbsoluteStageXy(diePitchDto.Rect.Point + (Vector)reviseRecipeDto.WaferDto.WaferCenterWaferPosition);

        var reticleDto = reviseRecipeDto.WaferDto.WaferMapCanvasDocument.ReticleModel.Single(t => t.Index is { X: 10, Y: 4 });
        _stageViewModel.SetBrightFieldAbsoluteStageXy(reticleDto.Rect.Point + (Vector)reviseRecipeDto.WaferDto.WaferCenterWaferPosition);

        var originDie = reviseRecipeDto.WaferDto.WaferMapCanvasDocument.DieModel.Single(t => t.Index is { X: 0, Y: 0 });

        var selectItem = RecipeReticleMaskViewModel.SelectReticleMarkItem;
        if (selectItem is null) return;

        _calibrationRecipeService.GetMicroscopeReticleMaskInfo(
            _recipeCookie.CalibrationRecipeDto.ReticleMarkDto,
            selectItem.ReticleMaskTypeEnum,
            selectItem.RecipeBrightFieldTemplateDto.MicroscopeLensInformation,
            null,
            out var maskDto);

        _calibrationRecipeService.GetDieMaskBrightFieldPosition(
            _recipeCookie.CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument,
            originDie,
            maskDto,
            out var originReticleMaskBrightFieldPosition);

        _microscopeViewModel.SwitchMicroscopeLensInformation(selectItem.RecipeBrightFieldTemplateDto.MicroscopeLensInformation);
        _stageViewModel.SetBrightFieldAbsoluteStageXy(originReticleMaskBrightFieldPosition);

        _calibrationRecipeService.GetReticleMaskBrightFieldPosition(
            _recipeCookie.CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument,
            reticleDto,
            maskDto,
            out var reticleMaskBrightFieldPosition);
        _stageViewModel.SetBrightFieldAbsoluteStageXy(reticleMaskBrightFieldPosition);

        var microscopePixelSizeItems = _cacheProvider.GetArray<MicroscopePixelSizeItemDto>();
        var template = selectItem.RecipeBrightFieldTemplateDto;

        _reviewViewModel.TryGetMatchPosition(
            template.AlgorithmTemplateTypeEnum,
            microscopePixelSizeItems!,
            reticleMaskBrightFieldPosition,
            template.MicroscopeLensInformation,
            template.TemplateFilePath,
            Path.GetDirectoryName(template.TemplateImageFilePath),
            null, null,
            "Magnification",
            out _, out _, out _, out _, out _);
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