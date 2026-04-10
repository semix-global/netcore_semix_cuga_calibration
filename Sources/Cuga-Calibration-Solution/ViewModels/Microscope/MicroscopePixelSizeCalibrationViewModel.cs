using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Models;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.PixelSize;
using Core.Utilities.SourceGenerators.Attributes;
using Local.SQL.Cache.Providers.Extensions;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Microscope;

[IOCAppService(ServiceType = typeof(MicroscopePixelSizeCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopePixelSizeCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.MicroscopeLensInformation.LensName);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.MicroscopeLensInformation.LensName);

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select a lens" },
        new() { StepName = "Select a location" },
        new() { StepName = "Find Pixel Size" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ObservableCollection<MicroscopePixelSizeItemDto> _microscopePixelSizeItemDtoList = [];

    [ObservableProperty]
    private ObservableCollection<MicroscopePixelSizeItemDto> _resultMicroscopePixelSizeItemDtoList = [];

    [ObservableProperty]
    private MicroscopePixelSizeItemDto? _selectMicroscopePixelSizeItemDto;

    [ObservableProperty]
    private MicroscopePixelSizeItemDto? _resultMicroscopePixelSizeItemDto;

    [ObservableProperty]
    private IReadOnlyList<MicroscopeLensInformationStatus> _calibratingStatuses = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ObservableCollection<MicroscopePixelSizeItemDto> _reviewList = [];

    [ObservableProperty]
    private MicroscopePixelSizeItemDto? _selectReviewItemDto;

    [ObservableProperty]
    private List<(string Microscope, Size OldPixelSize, Size NewPixelSize, Size OffsetPixelSize)> _reviewResultList = [];

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private MicroscopePixelSizeCache _cache = new();

    [ObservableProperty]
    private MicroscopePixelSizeCacheItem _selectMicroscopePixelSizeCacheItem = new();

    [ObservableProperty]
    private MicroscopeCalChipDTO _microscopeCalChip = new();

    [DefaultCache]
    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _calibrations = [];

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务重载

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false) return false;

        MicroscopeCalChip = CalibrationStatusService.GetCalibration<MicroscopeCalChipDTO>();

        (_, Cache) = RecipeCacheProvider.TryGetOrDefault<MicroscopePixelSizeCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<MicroscopePixelSizeItemDto>();

        Calibrations = [.. Calibrations.Where(t => ApplicationCookie.MicroscopeLensInformations.Contains(t.LensInformation))]; // 过滤掉变更静态配置后原来的缓存
        if (CalibratingStatuses.Count == 0)
            CalibratingStatuses = [.. ApplicationCookie.MicroscopeLensInformations.Select(t => new MicroscopeLensInformationStatus { SelectedItem = t })];

        Calibrations =
        [
            .. Calibrations
                .Where(t => ApplicationCookie.MicroscopeLensInformations.Contains(t.LensInformation))
                .Select(t =>
                {
                    CalibratingStatuses
                        .Single(tt => tt.SelectedItem == t.LensInformation)
                        .IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewList =
        [
            .. Calibrations
                .Where(t => t.IsCalibrated)
                .Select(t => t.Clone())
                .OrderBy(t => t.LensInformation.ObjectiveMagnification)
                .ThenBy(t => t.LensInformation.LensCode)
        ];

        return ReviewList.Count != 0 && ReviewList.Any(t => t.IsCalibrated);
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        switch (CalibrationStepIndex)
        {
            case 0:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(SelectMicroscopePixelSizeCacheItem.FindPosition, Cache.CalChipSiteModelEnum);
                if (Cache.CalChipSiteModelEnum is CalChipSiteModelEnum.DswModel)
                {
                    StageViewModel.SetAbsoluteStageTheta(MicroscopeCalChip.DSWAlignmentDegree);
                    StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.DSWBrightFieldMachineAffinePosition), Cache.CalChipSiteModelEnum);
                }

                return true;

            case 1:
                var result = StageViewModel.GetBrightFieldStagePosition();

                Cache.SetFindFocusPosition(result);
                return true;

            case 2:
                CalibratingStatuses
                    .Single(t => t.SelectedItem == Cache.MicroscopeLensInformation)
                    .IsCalibrated = true;

                IsCalibrated = CalibratingStatuses.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                ClearCalibrationTemp();

                return true;

            default:
                return false;
        }
    }

    protected override async Task<bool> CancelingAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);

        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

        return true;
    }

    #endregion 控制校准业务重载

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            SelectMicroscopePixelSizeCacheItem = Cache.CurrentCalibrationCacheItem;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
                Cache.CalChipSiteModelEnum
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(async () =>
        {
            var (isSuccess, errorMessage) = Cache.Verify();
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var result = StageViewModel.GetBrightFieldStagePosition();
            Cache.SetFindFocusPosition(result);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
                SelectMicroscopePixelSizeCacheItem.FindPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = false;
        await InvokeCalibrateAsync(() =>
        {
            ClearCalibrationTemp();
            var detectImageDirectory = ImageFileDirectory;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
                SelectMicroscopePixelSizeCacheItem.FindPosition,
                ImageFileDirectory = detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            if (GetPixelSize(detectImageDirectory, cancellationToken) == false)
                return false;

            var averageWidth = MicroscopePixelSizeItemDtoList.Average(t => t.PixelSize.Width);
            var averageHeight = MicroscopePixelSizeItemDtoList.Average(t => t.PixelSize.Height);
            var averagePixelSize = new Size(averageWidth, averageHeight);

            SelectMicroscopePixelSizeItemDto = MicroscopePixelSizeItemDtoList.OrderBy(t => ((Point)t.PixelSize - (Vector)averagePixelSize).ToOriginLength).First();
            ResultMicroscopePixelSizeItemDto = SelectMicroscopePixelSizeItemDto.Clone();
            ResultMicroscopePixelSizeItemDto.PixelSize = averagePixelSize;
            SynchronizationContextProvider.Send(() => ResultMicroscopePixelSizeItemDtoList.Add(ResultMicroscopePixelSizeItemDto));

            ResultMicroscopePixelSizeItemDto.IsCalibrated = true;
            Guard.IsTrue(Save([ResultMicroscopePixelSizeItemDto], cancellationToken));

            Logger.LogHtmlInformation("OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                ResultMicroscopePixelSizeItemDto.LensInformation.LensName,
                SelectMicroscopePixelSizeCacheItem.FindPosition,
                ResultMicroscopePixelSizeItemDto.PixelSize,
                HtmlTab = new HtmlTab(new
                {
                    OriginImage = new HtmlImage(ResultMicroscopePixelSizeItemDto.OriginFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    DrawingImage = new HtmlImage(ResultMicroscopePixelSizeItemDto.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                }),
                Plot2DWidthHeight = new HtmlPlot2DLinesChart(
                [
                    ("Width", MicroscopePixelSizeItemDtoList.Select(t => t.PixelSize.Width).ToPoints()),
                    ("Height", MicroscopePixelSizeItemDtoList.Select(t => t.PixelSize.Height).ToPoints())
                ], "PlotWidthHeight")
            }), HtmlLogUniqueId.LoggingHtml());
            result = true;
            return result;
        });
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> VerifyActionAsync(CancellationToken cancellationToken)
    {
        if (SelectReviewItemDto is null)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        return await InvokeVerifyAsync(async () => await VerifyCalibrationAsync(SelectReviewItemDto, cancellationToken)).ConfigureAwait(false);
    }

    private async Task<bool> VerifyCalibrationAsync(MicroscopePixelSizeItemDto? selectReviewItemDto, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            ClearCalibrationTemp();
            var detectImageDirectory = ImageFileDirectory;

            if (selectReviewItemDto is null)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Please select a review item!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            if (Cache.CalChipSiteModelEnum is CalChipSiteModelEnum.DswModel)
                StageViewModel.SetAbsoluteStageTheta(MicroscopeCalChip.DSWAlignmentDegree);

            selectReviewItemDto.IsVerified = false;
            Cache.MicroscopeLensInformation = SelectReviewItemDto!.LensInformation;
            SelectMicroscopePixelSizeCacheItem = Cache.CurrentCalibrationCacheItem;
            SelectMicroscopePixelSizeCacheItem.FindPosition = selectReviewItemDto.FindPosition;

            MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
            Logger.LogHtmlInformation($"{Cache.MicroscopeLensInformation.LensName}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
                SelectMicroscopePixelSizeCacheItem.FindPosition,
                ImageFileDirectory = detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            if (GetPixelSize(detectImageDirectory, cancellationToken, 1) == false) return false;

            var averageWidth = MicroscopePixelSizeItemDtoList.Select(t => t.PixelSize.Width).Average();
            var averageHeight = MicroscopePixelSizeItemDtoList.Select(t => t.PixelSize.Height).Average();
            var averagePixelSize = new Size(averageWidth, averageHeight);
            var error = (Size)((Vector)averagePixelSize - (Vector)selectReviewItemDto.PixelSize);
            ReviewResultList.Add((Cache.MicroscopeLensInformation.LensName, selectReviewItemDto.PixelSize, averagePixelSize, error));
            var result = error.DiagonalLength < Cache.Threshold.DiagonalLength;

            Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                NowOffset = averagePixelSize,
                OldOffset = selectReviewItemDto.PixelSize,
                Error = error
            }), HtmlLogUniqueId.LoggingHtml());
            selectReviewItemDto.IsVerified = result;

            Guard.IsTrue(Save([selectReviewItemDto], cancellationToken));

            DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}, New Offset: ({averagePixelSize}) Old Offset: ({SelectReviewItemDto!.PixelSize}) Error: ({error})", DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        }, cancellationToken);
    }

    private bool GetPixelSize(string detectImageDirectory, CancellationToken cancellationToken, int repeatCount = 10)
    {
        try
        {
            MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
            StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(SelectMicroscopePixelSizeCacheItem.FindPosition, Cache.CalChipSiteModelEnum);
            foreach (var times in Enumerable.Range(1, repeatCount))
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var image = ReviewViewModel.GetBrightFieldImage();

                var pixelSize = CalibrationAlgorithmService.GetPixelSize(image, SelectMicroscopePixelSizeCacheItem.AlgorithmStandardMaskSquareSizeEnum.ToSize(), out var drawingImage, out var angle);
                if (Math.Abs(angle) > Cache.AngleThreshold)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment($"The current horizontal angle exceeds the limit! Angle:{angle}"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                using var _2 = drawingImage;
                var microscopePixelSizeItemDto = new MicroscopePixelSizeItemDto
                {
                    LensInformation = Cache.MicroscopeLensInformation,
                    FindPosition = SelectMicroscopePixelSizeCacheItem.FindPosition,
                    OriginFilePath = $"{detectImageDirectory}\\PixelSize({pixelSize})_Times({times})_Guid({HtmlLogUniqueId}).jpg",
                    FilePath = $"{detectImageDirectory}\\PixelSize({pixelSize})_Drawing_Times({times})_Guid({HtmlLogUniqueId}).jpg",
                    PixelSize = pixelSize
                };

                image.Save(microscopePixelSizeItemDto.OriginFilePath);
                drawingImage.Save(microscopePixelSizeItemDto.FilePath);

                var htmlBulletList = new HtmlBullet(new
                {
                    microscopePixelSizeItemDto.LensInformation.LensName,
                    SelectMicroscopePixelSizeCacheItem.FindPosition,
                    microscopePixelSizeItemDto.PixelSize,
                    HtmlTab = new HtmlTab(new
                    {
                        OriginImage = new HtmlImage(microscopePixelSizeItemDto.OriginFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        DrawingImage = new HtmlImage(microscopePixelSizeItemDto.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                });

                Logger.LogHtmlInformation($"Get Pixel Size OK! Time:{times}", HtmlHeaderLevelEnum.Header5, htmlBulletList, HtmlLogUniqueId.LoggingHtml());

                SynchronizationContextProvider.Send(() => MicroscopePixelSizeItemDtoList.Add(microscopePixelSizeItemDto));
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get Pixel Size Failed", Name);
            return false;
        }
    }

    private bool Save(IReadOnlyList<MicroscopePixelSizeItemDto> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                .. Calibrations.Where(t => t.LensInformation != dto.LensInformation),
                dto
            ];
        }

        foreach (var microscopeFocusCacheItem in Cache.MicroscopePixelSizeCacheItemDic)
        {
            if (ApplicationCookie.MicroscopeLensInformations.SingleOrDefault(t => t.LensName == microscopeFocusCacheItem.Key) is null)
                Cache.MicroscopePixelSizeCacheItemDic.TryRemove(microscopeFocusCacheItem.Key, out _);
        }

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(MicroscopePixelSizeItemDtoList.Clear);
        SelectMicroscopePixelSizeItemDto = null;
        ResultMicroscopePixelSizeItemDto = null;
    }

    #endregion 校准
}