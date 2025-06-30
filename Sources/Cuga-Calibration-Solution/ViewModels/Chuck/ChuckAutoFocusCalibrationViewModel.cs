using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models;
using Core.Models.Models.Chuck.AutoFocus;
using Core.Models.Models.Chuck.Prealigner;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.IO;

namespace CugaCalibration.ViewModels.Chuck;

[IOCAppService(ServiceType = typeof(ChuckAutoFocusCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckAutoFocusCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Param" },
        new() { StepName = "Auto Focus Calibration" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ChuckAutoFocusDto? _resultChuckAutoFocusDto;

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ChuckAutoFocusDto? _reviewDto;

    #endregion Review

    [ObservableProperty]
    private (int Row, int Column) _currentRowColumn = (-1, -1);

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private ChuckAutoFocusCache _cache = new();

    [ObservableProperty]
    private ChuckAutoFocusDto _calibration = new();

    [ObservableProperty]
    private ChuckPrealignerObjDto _chuckPrealigner = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务重载

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (CalibrationStatusService.GetAdsCalibrationIsOKStatus() == false)
        {
            DialogWindowProvider.ShowDialog("The ADS precondition is Failure", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckPrealignerObjDto>(out _, out var errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<ChuckAutoFocusCache>();
        Calibration = CacheProvider.GetOrDefault<ChuckAutoFocusDto>();

        return isHasCache || RecipeCacheProvider.Set(Cache, cancellationToken);
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewDto = Calibration.Clone();

        return ReviewDto.IsCalibrated;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);
                MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);
                return true;

            case 1:
                if (ResultChuckAutoFocusDto is null)
                {
                    DialogWindowProvider.TryShowDialog("Please find chuck auto focus!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);

                    return dialogButtonsEnum != DialogResultEnum.Retry;
                }

                ResultChuckAutoFocusDto.IsCalibrated = true;
                if (Save(ResultChuckAutoFocusDto, cancellationToken) == false)
                {
                    ResultChuckAutoFocusDto.IsCalibrated = false;
                    Logger.LogError("{@Name} Error: Save Failed!", Name);
                    return false;
                }

                IsCalibrated = true;

                return true;

            default:
                return false;
        }
    }

    #endregion 控制校准业务重载

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeMagnificationEnum,
                Cache.RowNumber,
                Cache.ColumnNumber,
                Cache.ChuckDiameter,
                Cache.ColumnCellWidth,
                Cache.RowCellHeight,
                Cache.WaitTime,
                Cache.Threshold
            }), HtmlLogUniqueId.LoggingHtml());

            var (isSuccess, errorMessage) = Cache.Verify();
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Cache varify error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            // 中心点的索引
            var centerX = (Cache.ColumnNumber - 1) / 2;
            var centerY = (Cache.RowNumber - 1) / 2;

            ResultChuckAutoFocusDto = new ChuckAutoFocusDto
            {
                RowNumber = Cache.RowNumber,
                ColumnNumber = Cache.ColumnNumber,
                RowCellHeight = Cache.RowCellHeight,
                ColumnCellWidth = Cache.ColumnCellWidth,
                ChuckDiameter = Cache.ChuckDiameter,
                Map = []
            };

            for (var row = 0; row < Cache.RowNumber; row++)
            {
                for (var column = 0; column < Cache.ColumnNumber; column++)
                {
                    var position = new Point((column - centerX) * Cache.ColumnCellWidth, -(row - centerY) * Cache.RowCellHeight);
                    ResultChuckAutoFocusDto.Map.Add(new ChuckAutoFocusItemDto
                    {
                        Position = position,
                        Row = row,
                        Column = column,
                        IsInscribedSquareSide = true
                    });
                }
            }

            Logger.LogHtmlInformation("Map", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Map = new HtmlTable([
                    .. ResultChuckAutoFocusDto.Map.Select(t => new
                    {
                        t.Row,
                        t.Column,
                        t.Position
                    })
                ])
            }), HtmlLogUniqueId.LoggingHtml());

            OnPropertyChanged(nameof(ResultChuckAutoFocusDto));

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var (isSuccess, errorMessage) = Cache.Verify();
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Cache varify error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            // 中心点的索引
            int centerX, centerY;
            foreach (var chuckAutoFocusItemDto in ResultChuckAutoFocusDto!.Map.Where(t => t.IsInscribedSquareSide))
            {
                cancellationToken.ThrowIfCancellationRequested();

                CurrentRowColumn = (chuckAutoFocusItemDto.Row, chuckAutoFocusItemDto.Column);
                StageViewModel.SetBrightFieldAbsoluteStageXy(chuckAutoFocusItemDto.Position);
                MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);

                await Task.Delay(TimeSpan.FromSeconds(Cache.WaitTime), cancellationToken).ConfigureAwait(false);

                var value = AfViewModel.GetSensorEcsValue();
                chuckAutoFocusItemDto.EcsValue = value;
                var filePath = Path.Combine(ImageFileDirectory, $"({chuckAutoFocusItemDto.Row},{chuckAutoFocusItemDto.Column})_Ecs({value:F3})_Guid({HtmlLogUniqueId}).jpg");
                ReviewViewModel.SaveCurrentBrightFieldImage(filePath);

                Logger.LogHtmlInformation($"Get Ecs:({chuckAutoFocusItemDto.Row},{chuckAutoFocusItemDto.Column})", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    chuckAutoFocusItemDto.Position,
                    chuckAutoFocusItemDto.Row,
                    chuckAutoFocusItemDto.Column,
                    chuckAutoFocusItemDto.EcsValue,
                    HtmlTab = new HtmlTab(new
                    {
                        Image = new HtmlImage(filePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                OnPropertyChanged(nameof(ResultChuckAutoFocusDto));
            }

            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            CurrentRowColumn = (-1, -1);
            OnPropertyChanged(nameof(ResultChuckAutoFocusDto));

            var rowCount = (int)Math.Ceiling(Cache.ChuckDiameter / 2d / Cache.ColumnCellWidth) * 2 + 1;
            var colCount = (int)Math.Ceiling(Cache.ChuckDiameter / 2d / Cache.RowCellHeight) * 2 + 1;

            centerX = (rowCount - 1) / 2;
            centerY = (colCount - 1) / 2;

            // 外扩
            for (var row = 0; row < rowCount; row++)
            {
                for (var column = 0; column < colCount; column++)
                {
                    var position = new Point((column - centerX) * Cache.ColumnCellWidth, -(row - centerY) * Cache.RowCellHeight);
                    var chuckAutoFocusItemDto = ResultChuckAutoFocusDto.Map
                        .Where(t => t.IsInscribedSquareSide)
                        .OrderBy(t => (t.Position - (Vector)position).ToOriginLength)
                        .First();
                    var autoFocusItemDto = ResultChuckAutoFocusDto.Map.SingleOrDefault(t => t.Position == position);
                    if (autoFocusItemDto is not null)
                    {
                        autoFocusItemDto.Row = row;
                        autoFocusItemDto.Column = column;

                        if (autoFocusItemDto.IsInscribedSquareSide == false) autoFocusItemDto.EcsValue = chuckAutoFocusItemDto.EcsValue;

                        continue;
                    }

                    ResultChuckAutoFocusDto.Map.Add(new ChuckAutoFocusItemDto
                    {
                        Position = position,
                        Row = row,
                        Column = column,
                        EcsValue = chuckAutoFocusItemDto.EcsValue,
                        IsInscribedSquareSide = false
                    });
                }
            }

            OnPropertyChanged(nameof(ResultChuckAutoFocusDto));

            Logger.LogHtmlInformation("Get Chuck AutoFocus Ok", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                AllRowCount = rowCount,
                AllColCount = colCount,
                Map = new HtmlTable([
                    .. ResultChuckAutoFocusDto.Map.Select(t => new
                    {
                        t.Row,
                        t.Column,
                        t.EcsValue,
                        t.Position
                    })
                ])
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        var chuckAutoFocusDto = ReviewDto;
        if (chuckAutoFocusDto is null)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(async () =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeMagnificationEnum,
                Cache.RowNumber,
                Cache.ColumnNumber,
                Cache.ChuckDiameter,
                Cache.ColumnCellWidth,
                Cache.RowCellHeight,
                Cache.WaitTime,
                Cache.Threshold
            }), HtmlLogUniqueId.LoggingHtml());

            var (isSuccess, errorMessage) = Cache.Verify();
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Cache varify error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            chuckAutoFocusDto.IsVerified = false;
            ReviewDto = chuckAutoFocusDto.Clone();
            ReviewDto.IsVerified = false;

            var resultList = new List<bool>();

            foreach (var chuckAutoFocusItemDto in ReviewDto.Map.Where(t => t.IsInscribedSquareSide))
            {
                cancellationToken.ThrowIfCancellationRequested();

                CurrentRowColumn = (chuckAutoFocusItemDto.Row, chuckAutoFocusItemDto.Column);
                StageViewModel.SetBrightFieldAbsoluteStageXy(chuckAutoFocusItemDto.Position);
                MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);

                await Task.Delay(TimeSpan.FromSeconds(Cache.WaitTime), cancellationToken).ConfigureAwait(false);

                var value = AfViewModel.GetSensorEcsValue();

                var ecsValue = chuckAutoFocusItemDto.EcsValue;
                chuckAutoFocusItemDto.EcsValue = value;
                var filePath = Path.Combine(ImageFileDirectory, $"({chuckAutoFocusItemDto.Row},{chuckAutoFocusItemDto.Column})_Ecs({value:F3})_Guid({HtmlLogUniqueId}).jpg");
                ReviewViewModel.SaveCurrentBrightFieldImage(filePath);

                var itemResult = Math.Abs(ecsValue - value) <= Cache.Threshold;
                resultList.Add(itemResult);

                Logger.LogHtmlInformation($"Get Ecs:({chuckAutoFocusItemDto.Row},{chuckAutoFocusItemDto.Column}) {(itemResult ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    result = itemResult,
                    chuckAutoFocusItemDto.Position,
                    chuckAutoFocusItemDto.Row,
                    chuckAutoFocusItemDto.Column,
                    chuckAutoFocusItemDto.EcsValue,
                    HtmlTab = new HtmlTab(new
                    {
                        Image = new HtmlImage(filePath, htmlImageOverlays: [new HtmlImageCrossOverlay(false)])
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                OnPropertyChanged(nameof(ReviewDto));
            }

            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            CurrentRowColumn = (-1, -1);
            OnPropertyChanged(nameof(ReviewDto));

            var result = resultList.All(t => t);

            chuckAutoFocusDto.IsVerified = result;
            ReviewDto.IsVerified = result;

            if (Save(chuckAutoFocusDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                chuckAutoFocusDto.IsVerified = false;
                return false;
            }

            Logger.LogHtmlInformation($"Chuck Auto Focus Verify {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Map = new HtmlTable([
                    .. ReviewDto.Map.Select(t => new
                    {
                        t.Row,
                        t.Column,
                        t.EcsValue,
                        t.Position
                    })
                ])
            }), HtmlLogUniqueId.LoggingHtml());

            if (result)
                DialogWindowProvider.ShowDialog("Verify OK");
            else
                DialogWindowProvider.ShowDialog("Verify Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            return ReviewDto.IsVerified;
        }).ConfigureAwait(false);
    }

    private bool Save(ChuckAutoFocusDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        dto.MicroscopeMagnificationEnum = Cache.MicroscopeMagnificationEnum;

        Calibration = dto.Clone();

        return CacheProvider.Set(dto, cancellationToken) && RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}