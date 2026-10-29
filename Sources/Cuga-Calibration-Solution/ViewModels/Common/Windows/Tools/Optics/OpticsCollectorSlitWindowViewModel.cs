using System.IO;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Services.Interfaces;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using Generate = MathNet.Numerics.Generate;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Optics;

[IOCAppService(ServiceType = typeof(OpticsCollectorSlitWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsCollectorSlitWindowViewModel(
    StageViewModel stageViewModel,
    AfViewModel afViewModel,
    CIBViewModel cibViewModel,
    IOptions<ApplicationSetting> options,
    ApplicationCookie applicationCookie,
    ICacheProvider cacheProvider,
    IDialogWindowProvider dialogWindowProvider,
    ILogger<OpticsCollectorSlitWindowViewModel> logger) : ViewModelBase
{
    public string Name => "Collection Focus Align Optics Focus";

    public string ImageDirectory => Path.Combine(options.Value.AppHomeDirectory, "Images", nameof(OpticsCollectorSlitWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public ApplicationCookie ApplicationCookie => applicationCookie;

    public string[] Steps { get; } = ["Step 1 Capture"];

    public Guid HtmlLogUniqueId { get; private set; }

    [DefaultCache]
    [ObservableProperty]
    public partial OpticsCollectorSlitCache Cache { get; set; } = new();

    [RelayCommand]
    private void Loaded()
    {
        Cache = cacheProvider.GetOrDefault<OpticsCollectorSlitCache>();
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GetECSAsync(CancellationToken cancellationToken)
    {
        stageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(Cache.HazeBrightFieldPosition, CalChipSiteModelEnum.HazeModel);
        afViewModel.ToggleDarkFieldEnable(true);
        await Task.Delay(TimeSpan.FromMilliseconds(1000d), cancellationToken).ConfigureAwait(false);

        Cache.CenterECS = afViewModel.GetSensorAverageEcsValue();
        afViewModel.ToggleDarkFieldEnable(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(0, async () =>
    {
        Cache.BinWidthPixel = 0;
        Cache.DiscardedPixelCount = 0;

        var cibInformations = ApplicationCookie.CIBInformations.Where(t => t.PMTId == Cache.PmtId).OrderBy(t => t.ChannelId).ToArray();
        Guard.IsNotEmpty(cibInformations, $"PMTId {Cache.PmtId} has no CIB information.");

        var currentPosition = cibViewModel.GetCIBInformationPosition(
            StageCoordinateSystemEnum.Dark,
            Cache.ProductivityInformation,
            cibInformations[0],
            Cache.HazeBrightFieldPosition);

        Cache.ChannelResults = [.. cibInformations.Select(t => new OpticsCollectorSlitChannelResult { ChannelId = t.ChannelId })];

        if (Cache.CaptureMode == OpticsCollectorSlitCaptureModeEnum.VariableHeight)
            await CaptureVariableHeightAsync(cancellationToken, currentPosition, cibInformations).ConfigureAwait(false);
        else
            await CaptureFixedHeightAsync(cancellationToken, currentPosition, cibInformations).ConfigureAwait(false);

        Guard.IsTrue(Cache.ChannelResults.Any(t => t.Samples.Count > 0));
        UpdateChannelResults();

        foreach (var channelResult in Cache.ChannelResults)
        {
            logger.LogHtmlInformation($"Channel Id: {channelResult.ChannelId} OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Result = new HtmlQuote(channelResult.ToHtmlAnonymous()),
                Plots = new HtmlContainer([.. channelResult.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])
            }), HtmlLogUniqueId.LoggingHtml());
        }

        return true;
    }, isSilent, cancellationToken).ConfigureAwait(false);

    private async Task CaptureFixedHeightAsync(CancellationToken cancellationToken, Point currentPosition, CIBInformation[] cibInformations)
    {
        stageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(currentPosition, CalChipSiteModelEnum.HazeModel);

        var ecss = Generate.LinearRange(Cache.CenterECS - Cache.RangeEcs, Cache.StepEcs, Cache.CenterECS + Cache.RangeEcs);
        Guard.IsNotEmpty(ecss);

        logger.LogHtmlInformation("Action", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

        foreach (var ecs in ecss)
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogHtmlInformation($"{ecs}(ECS)", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

            afViewModel.ToggleBrightFieldEnable(false);
            afViewModel.SetSensorEcsValue(ecs);
            await Task.Delay(TimeSpan.FromMilliseconds(500d), cancellationToken).ConfigureAwait(false);

            var darkFieldImageDtos = await cibViewModel.GetPMTImagesAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Dark,
                currentPosition,
                Cache.ImageWidthPixel,
                cibInformations,
                (false, CalChipSiteModelEnum.HazeModel),
                (false, Cache.HazeOpticsConfiguration),
                (false, Cache.HazeCIBConfiguration),
                (false, Cache.HazeLaserLightInformation),
                false,
                cancellationToken,
                isForward: Cache.IsForward,
                isAutoFocus: false).ConfigureAwait(false);

            foreach (var darkFieldImageDto in darkFieldImageDtos)
            {
                using var _ = darkFieldImageDto;
                using var hImage = darkFieldImageDto.Image.ToHImage();
                var matrix = Matrix<double>.Build.DenseOfArray(hImage.RAW16BitsPerPixelToMatrix());
                var filePath = SaveImg(darkFieldImageDto);
                var sample = CreateSample(darkFieldImageDto, ecs, matrix, filePath);
                var channel = Cache.ChannelResults.Single(t => t.ChannelId == darkFieldImageDto.CIBInformation.ChannelId);
                channel.Samples = [.. channel.Samples, sample];

                logger.LogHtmlInformation($"Channel Id: {sample.ChannelId}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                {
                    Image = new HtmlImage(sample.ImageFilePath),
                    Result = new HtmlQuote(sample.ToHtmlAnonymous())
                }), HtmlLogUniqueId.LoggingHtml());
            }
        }
    }

    private async Task CaptureVariableHeightAsync(CancellationToken cancellationToken, Point startPosition, CIBInformation[] cibInformations)
    {
        logger.LogHtmlInformation("Action", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

        var stopPosition = startPosition + new Vector(Cache.ScanLength, 0d);
        var (images, traceBuffers, startIndex, endIndex) = await cibViewModel.GetXzSyncColumnEcsAsync(
            Cache.ProductivityInformation,
            StageCoordinateSystemEnum.Dark,
            startPosition,
            stopPosition,
            Cache.CenterECS - Cache.RangeEcs,
            Cache.CenterECS + Cache.RangeEcs,
            cibInformations,
            (true, null),
            (false, Cache.HazeOpticsConfiguration),
            (false, Cache.HazeCIBConfiguration),
            (false, Cache.HazeLaserLightInformation),
            false,
            cancellationToken,
            isForward: Cache.IsForward).ConfigureAwait(false);

        foreach (var channel in Cache.ChannelResults)
        {
            channel.TraceBufferX = [.. traceBuffers.Select(t => t.X)];
            channel.TraceBufferECS = [.. traceBuffers.Select(t => t.Ecs)];
            channel.StartIndex = startIndex;
            channel.EndIndex = endIndex;
        }

        var ecsRange = Math.Abs(Cache.RangeEcs * 2d);
        foreach (var (darkFieldImageDto, columnEcs) in images)
        {
            using var _ = darkFieldImageDto;
            cancellationToken.ThrowIfCancellationRequested();

            var filePath = SaveImg(darkFieldImageDto);
            var samples = CreateSamplesFromColumns(darkFieldImageDto, filePath, ecsRange, columnEcs);

            if (samples.Count == 0) continue;

            var channel = Cache.ChannelResults.Single(t => t.ChannelId == darkFieldImageDto.CIBInformation.ChannelId);
            channel.Samples = [.. channel.Samples, .. samples];

            logger.LogHtmlInformation($"Channel Id: {darkFieldImageDto.CIBInformation.ChannelId}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
            {
                Image = new HtmlImage(filePath),
                samples[0].Ny,
                Cache.BinWidthPixel,
                Cache.DiscardedPixelCount,
                darkFieldImageDto.RawImageFilePath
            }), HtmlLogUniqueId.LoggingHtml());
        }
    }

    private IReadOnlyList<OpticsCollectorSlitSample> CreateSamplesFromColumns(DarkFieldImageDTO darkFieldImageDto,
        string filePath,
        double ecsRange,
        double[] columnEcs)
    {
        using var hImage = darkFieldImageDto.Image.ToHImage();
        var matrix = Matrix<double>.Build.DenseOfArray(hImage.RAW16BitsPerPixelToMatrix());
        var columnCount = matrix.ColumnCount;
        //bin 宽度取 0.5 个 ECS 对应的像素数并向下取整,一个 ECS 对应的像素数为 NX 除以图像覆盖的 ECS 扫描范围
        Cache.BinWidthPixel = ecsRange == 0d ? 0 : (int)Math.Floor(0.5 * columnCount / ecsRange);

        if (Cache.BinWidthPixel < 1) return [];

        var binCount = columnCount / Cache.BinWidthPixel;
        Cache.DiscardedPixelCount = columnCount % Cache.BinWidthPixel;
        var samples = new OpticsCollectorSlitSample[binCount];

        for (var binIndex = 0; binIndex < binCount; binIndex++)
        {
            var columnStart = binIndex * Cache.BinWidthPixel;
            var averaged = Matrix<double>.Build.Dense(matrix.RowCount, 1, (row, _) => Enumerable.Range(columnStart, Cache.BinWidthPixel).Average(column => matrix[row, column]));
            var ecs = (columnEcs[columnStart] + columnEcs[columnStart + Cache.BinWidthPixel - 1]) / 2d;
            samples[binIndex] = CreateSample(darkFieldImageDto, ecs, averaged, filePath);
        }

        return samples;
    }

    private OpticsCollectorSlitSample CreateSample(DarkFieldImageDTO darkFieldImageDto, double ecs, Matrix<double> matrix, string filePath)
    {
        var baseSize = matrix.RowCount / 3;
        var remainder = matrix.RowCount % 3;

        var topRows = baseSize;
        var middleRows = baseSize;
        var bottomRows = baseSize + remainder;

        return new()
        {
            ECS = ecs,
            ChannelId = darkFieldImageDto.CIBInformation.ChannelId,
            ImageFilePath = filePath,
            RawImageFilePath = darkFieldImageDto.RawImageFilePath,
            Ny = [topRows, middleRows, bottomRows],
            TopAverageGray = AverageRows(0, topRows),
            MiddleAverageGray = AverageRows(topRows, middleRows),
            BottomAverageGray = AverageRows(topRows + middleRows, bottomRows),
            StandardDeviation = matrix.Enumerate().StandardDeviation()
        };

        double AverageRows(int startRow, int rowCount) =>
            matrix.SubMatrix(startRow, rowCount, 0, matrix.ColumnCount).Enumerate().Average();
    }

    private string SaveImg(DarkFieldImageDTO darkFieldImageDto)
    {
        var filePath = Path.Combine(ImageDirectory, $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
        DirectoryHelper.CreateFileDirectoryIfNotExists(filePath);
        darkFieldImageDto.Image.SaveImage(filePath);

        return filePath;
    }

    private void UpdateChannelResults()
    {
        foreach (var channel in Cache.ChannelResults)
        {
            var ordered = channel.Samples.OrderBy(t => t.ECS).ToArray();
            if (ordered.Length == 0) continue;

            var collectionPeak = ordered.Index().MaxBy(t => t.Item.MiddleAverageGray);
            var illuminationPeak = ordered.MaxBy(t => t.StandardDeviation);

            Guard.IsNotNull(collectionPeak);
            Guard.IsNotNull(illuminationPeak);

            var collectionBestFocus = new Point(collectionPeak.Item.ECS, collectionPeak.Item.MiddleAverageGray);
            var illuminationBestFocus = new Point(illuminationPeak.ECS, illuminationPeak.StandardDeviation);
            var threshold = 0.95 * collectionPeak.Item.MiddleAverageGray;
            var left = ordered.Take(collectionPeak.Index + 1).Reverse().TakeWhile(t => t.MiddleAverageGray >= threshold).Last();
            var right = ordered.Skip(collectionPeak.Index).TakeWhile(t => t.MiddleAverageGray >= threshold).Last();

            channel.CollectionBestFocusPoint = collectionBestFocus;
            channel.PeakRegionLeftEcs = left.ECS;
            channel.PeakRegionRightEcs = right.ECS;
            channel.PeakRegionWidth = right.ECS - left.ECS;
            channel.PeakRegionReachedEndpoint = left == ordered[0] || right == ordered[^1];
            channel.IlluminationBestFocusPoint = illuminationBestFocus;
            channel.Samples = ordered;
            channel.Offset = collectionBestFocus.X - illuminationBestFocus.X;
            channel.HasBestFocus = true;
        }
    }

    [RelayCommand]
    private void Close()
    {
        if (Step0Command.IsRunning) Step0CancelCommand.Execute(null);

        try
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5d));

            cacheProvider.Set(Cache, cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save cache");
        }

        CloseView(null);
    }

    private async Task<bool> InvokeAsync(
        int stepIndex,
        Func<Task<bool>> func,
        bool isSilent,
        CancellationToken cancellationToken)
    {
        return await Task.Run(async () =>
        {
            var isInitHtmlLog = isSilent == false || stepIndex == 0;
            var isEndHtml = isSilent == false || stepIndex == Steps.Length - 1;

            HtmlLogUniqueId = isInitHtmlLog ? Guid.NewGuid() : HtmlLogUniqueId;

            if (isInitHtmlLog) logger.LogHtmlInformation(Name, HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation(Steps[stepIndex], HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Cache.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            var isSuccess = false;
            try
            {
                isSuccess = await func().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    if (isSilent) isEndHtml = true;

                    dialogWindowProvider.ShowDialog($"{Name}: {Steps[stepIndex]} Canceled", dialogButtonsEnum: DialogButtonsEnum.OK, dialogIconEnum: DialogIconEnum.Warning);
                    logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    return false;
                }

                dialogWindowProvider.ShowDialog($"""
                                                 {Name}: {Steps[stepIndex]} Failed
                                                 {ex.Message}
                                                 """, dialogButtonsEnum: DialogButtonsEnum.OK, dialogIconEnum: DialogIconEnum.Warning);
                logger.LogHtmlError(ex, Name, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            }
            finally
            {
                if (isEndHtml || isSuccess == false)
                    logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{(isSilent ? "All" : Steps[stepIndex].Replace(" ", string.Empty))}_{(isSuccess ? "OK" : "Failed")}"));
            }

            if (isSuccess)
            {
                if (isEndHtml) dialogWindowProvider.ShowDialog($"{Name}: {(isSilent ? "All" : Steps[stepIndex])} Success");
            }
            else
                dialogWindowProvider.ShowDialog($"{Name}: {Steps[stepIndex]} Error", dialogButtonsEnum: DialogButtonsEnum.OK, dialogIconEnum: DialogIconEnum.Warning);

            return isSuccess;
        }, cancellationToken).ConfigureAwait(false);
    }
}