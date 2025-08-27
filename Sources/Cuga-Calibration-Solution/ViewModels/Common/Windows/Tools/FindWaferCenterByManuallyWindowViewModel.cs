using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.Alignment;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.Helper;

namespace CugaCalibration.ViewModels.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(FindWaferCenterByManuallyWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class FindWaferCenterByManuallyWindowViewModel(
    StageViewModel stageViewModel,
    ILogger<FindWaferCenterByManuallyWindowViewModel> logger,
    ISynchronizationContextProvider contextProvider) : CalibrationViewModelBase
{
    [ObservableProperty]
    private StageViewModel _stageViewModel = stageViewModel;

    [ObservableProperty]
    private AlignmentFindCenterCache _cache = new();

    [ObservableProperty]
    private AlignmentFindCenterCache? _alignmentFindCenterCache;

    [ObservableProperty]
    private bool _isEnable = true;

    [RelayCommand]
    private Task FindWaferCenterLoadedAsync()
    {
        return InvokeAsync(() =>
        {
            try
            {
                if (AlignmentFindCenterCache is null) Cache = RecipeCacheProvider.GetOrDefault<AlignmentFindCenterCache>();
                else Cache = AlignmentFindCenterCache;
            }
            catch (Exception ex)
            {
                DialogWindowProvider.ShowDialog("Find wafer center load failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                logger.LogError(ex, "{@Name}: Loaded Failed", nameof(FindWaferCenterByManuallyWindowViewModel));
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    internal async Task<bool> ActionAsync(CancellationToken cancellationToken)
    {
        var result = true;
        await InvokeAsync(() =>
        {
            StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

            List<Point> waferEdgeOffsets =
            [
                Cache.FindWaferCenterOffset1,
                Cache.FindWaferCenterOffset2,
                Cache.FindWaferCenterOffset3,
                Cache.FindWaferCenterOffset4,
                Cache.FindWaferCenterOffset5,
                Cache.FindWaferCenterOffset6,
                Cache.FindWaferCenterOffset7,
                Cache.FindWaferCenterOffset8
            ];

            var (offsetPosition, bitmapMemoryBytes) = StageViewModel.FindWaferCenterByManually(Point.Origin, waferEdgeOffsets);
            Cache.OffsetPosition = offsetPosition;
            if (bitmapMemoryBytes is not null && bitmapMemoryBytes.Count > 0)
            {
                Cache.WaferCenterThumb1 = bitmapMemoryBytes[0];
                Cache.WaferCenterThumb2 = bitmapMemoryBytes[1];
                Cache.WaferCenterThumb3 = bitmapMemoryBytes[2];
                Cache.WaferCenterThumb4 = bitmapMemoryBytes[3];
                Cache.WaferCenterThumb5 = bitmapMemoryBytes[4];
                Cache.WaferCenterThumb6 = bitmapMemoryBytes[5];
                Cache.WaferCenterThumb7 = bitmapMemoryBytes[6];
                Cache.WaferCenterThumb8 = bitmapMemoryBytes[7];
                SaveWaferCenterThumbImages(bitmapMemoryBytes);
                Save();
            }

            if (Math.Abs(Cache.OffsetPosition.X) > Cache.PositionErrorThreshold || Math.Abs(Cache.OffsetPosition.Y) > Cache.PositionErrorThreshold)
            {
                logger.LogHtmlInformation("Find wafer center offset exceeds threshold, please manually adjust EFEM.", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    OffsetX = Cache.OffsetPosition.X,
                    OffsetY = Cache.OffsetPosition.Y
                }), HtmlLogUniqueId.LoggingHtml());

                DialogWindowProvider.ShowDialog("Find wafer center offset exceeds, please manually adjust EFEM.", DialogButtonsEnum.OK, DialogIconEnum.Error);
                result = false;
                return;
            }

            logger.LogHtmlInformation("Find wafer center result OK", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                Cache.OffsetPosition,
                Cache.FindWaferCenterOffset1,
                Cache.FindWaferCenterOffset2,
                Cache.FindWaferCenterOffset3,
                Cache.FindWaferCenterOffset4,
                Cache.FindWaferCenterOffset5,
                Cache.FindWaferCenterOffset6,
                Cache.FindWaferCenterOffset7,
                Cache.FindWaferCenterOffset8
            }), HtmlLogUniqueId.LoggingHtml());
        });
        return result;
    }

    private Task InvokeAsync(Action action)
    {
        return Task.Run(() =>
        {
            try
            {
                contextProvider.Send(() => IsEnable = false);
                action();
            }
            catch (Exception e)
            {
                logger.LogError(e, "{@Name}: Invoke Failed", nameof(FindWaferCenterByManuallyWindowViewModel));
            }
            finally
            {
                contextProvider.Send(() => IsEnable = true);
            }
        });
    }

    private void SaveWaferCenterThumbImages(List<byte[]> bitmapMemoryBytes)
    {
        logger.LogHtmlInformation("Save Wafer Center Thumb Images", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
        for (var i = 0; i < bitmapMemoryBytes.Count; i++)
        {
            var waferCenterThumbPath = $"{ImageFileDirectory}\\WaferCenterThumb\\WaferCenterThumb{i + 1}_Guid{HtmlLogUniqueId}.jpg";
            var waferCenterThumbBitmapSource = BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(bitmapMemoryBytes[i]);
            BitmapSourceHelper.Save(waferCenterThumbBitmapSource, waferCenterThumbPath);
            logger.LogHtmlInformation($"Find center edge image {i}", HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
            {
                HtmlTab = new HtmlTab(new
                {
                    WaferCenterThumb = new HtmlImage(waferCenterThumbPath, htmlImageOverlays: [new HtmlImageCrossOverlay(false)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
        }
    }

    private void Save()
    {
        Cache.IsOk = true;
        if (RecipeCacheProvider.Set(Cache, CancellationToken.None) == false)
        {
            Cache.IsOk = false;
            DialogWindowProvider.ShowDialog("Failed to save cache!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        //DialogWindowProvider.ShowDialog("Save Ok");
    }
}