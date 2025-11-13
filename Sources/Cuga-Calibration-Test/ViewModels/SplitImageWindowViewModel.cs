using System.Buffers;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Channels;
using Core.Utilities;

namespace CugaCalibrationTest.ViewModels;

[IOCAppService(ServiceType = typeof(SplitImageWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class SplitImageWindowViewModel(ILogger<SplitImageWindowViewModel> logger) : ViewModelBase
{
    [ObservableProperty]
    private string _templateFilePath = @"\\10.10.2.19\d\Nano\Cuga-Calibration\Template\LaserXPixelSizeCalibrationViewModel\S40(H/L)\20251113\1_5X_a0ee9d42-f1e7-49e5-9029-89e477d69aad.ncc";

    [ObservableProperty]
    private string _templateImageFilePath = @"\\10.10.2.19\d\Nano\Cuga-Calibration\Template\LaserXPixelSizeCalibrationViewModel\S40(H/L)\20251113\1_5X_a0ee9d42-f1e7-49e5-9029-89e477d69aad.jpg";

    [ObservableProperty]
    private string _rawImageFilePath = @"C:\Users\DELL\Pictures\20251113_1196_0_0_1_short_840536_PMT08-CH3_8.raw";

    [ObservableProperty]
    private double _dieWidthUm = 15300;

    [ObservableProperty]
    private double _imageMatchThreshold = 0.8;

    [ObservableProperty]
    private double _realUmPerPixel;

    [ObservableProperty]
    private int _imageWidthPixel = 1200;

    [ObservableProperty]
    private int _imageCount = 19;

    [ObservableProperty]
    private int _waferDiameterUm = 300_000;

    [ObservableProperty]
    private int _verifyThreasholdPixel = 15;

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task SplitImageAsync(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            var startNew = Stopwatch.StartNew();
            var guid = Guid.NewGuid();
            try
            {
                var detectImageDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", nameof(SplitImageWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

                using var templateId = TemplateFilePath.ReadNccTemplate();
                var (_, templateImageSize) = ImageHelper.GetImageInfo(TemplateImageFilePath);

                logger.LogHtmlInformation("1. Get Um Per Pixel", HtmlHeaderLevelEnum.Header3, guid.LoggingHtml());
                logger.LogHtmlInformation("1.1 Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    TemplateFilePath,
                    TemplateImage = new HtmlImage(TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    RawImageFilePath,
                    DieWidthUm,
                    ImageMatchThreshold,
                    detectImageDirectory
                }), guid.LoggingHtml());

#if NET
                await using
#else
                using
#endif
                    var fileSteam = File.OpenRead(RawImageFilePath);
                using var binaryReader = new BinaryReader(fileSteam, Encoding.UTF8, true);

                var (size, bodyBytesStartIndex, bodyBytesLength) = RawImageFactory.GetSize(binaryReader);
                var (_, heightPixel) = (SizeI)size;
                var heightPixelByteLength = heightPixel * 2;

                #region RealUmPerPixel

                var windowWidthSize = ((SizeI)templateImageSize).Width * 3;
                var windowWidthStep = ((SizeI)templateImageSize).Width;
                var windowImageAllPixelByteLength = windowWidthSize * heightPixelByteLength;
                var windowStepAllPixelByteLength = windowWidthStep * heightPixelByteLength;
                var totalCount = MathHelper.SlideCount(windowImageAllPixelByteLength, windowStepAllPixelByteLength, bodyBytesLength);

                logger.LogHtmlInformation("1.2. Split", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    size,
                    windowWidthSize,
                    windowWidthStep,
                    totalCount
                }), guid.LoggingHtml());

                var items = new Item[totalCount];

                #region Channel

                using var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
                var channel = Channel.CreateBounded<Item>(new BoundedChannelOptions(totalCount) { SingleReader = true, SingleWriter = true, AllowSynchronousContinuations = true });

                #region Reader

                // ReSharper disable AccessToDisposedClosure

                var tasks = new Task[totalCount];
                var channelReaderTask = Task.Run(async () =>
                {
                    var index = 0;
                    await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                    {
                        try
                        {
                            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                            tasks[index] = ResolveAsync(item, true);

                            index++;
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }
                }, cancellationToken);

                // ReSharper restore AccessToDisposedClosure

                #endregion

                #region Writer

                foreach (var (index, pointer) in Enumerable
                             .Range(0, totalCount)
                             .Select(t => (long)t * windowStepAllPixelByteLength).Index())
                {
                    items[index] = GetItem(pointer, windowImageAllPixelByteLength);

                    await channel.Writer.WriteAsync(items[index], cancellationToken).ConfigureAwait(false);
                }

                channel.Writer.Complete();

                #endregion

                #endregion

                await channelReaderTask.ConfigureAwait(false);
                await Task.WhenAll(tasks).ConfigureAwait(false);

                var matchPoints = items.Where(t => t.IsOk).Select(t => t.MatchPoint).ToArray();

                var xDifferences = matchPoints
                    .Zip(matchPoints.Skip(1), (prev, next) => next.X - prev.X)
                    .ToArray();
                var average = xDifferences.Average();
                var xFilterDifferences = xDifferences.Where(t => t >= average).ToArray();
                Guard.IsEqualTo(xFilterDifferences.Length, ImageCount);
                RealUmPerPixel = DieWidthUm / xFilterDifferences.Average();

                logger.LogHtmlInformation("1.3. Split Result", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    AllScore = new HtmlPlot2DLinesChart([(string.Empty, [.. items.Select(t => new Point(t.MatchPoint.X, t.Score))])], string.Empty),
                    matchPoints = new HtmlPlot2DLinesChart([(string.Empty, matchPoints)], string.Empty),
                    xDifferences = new HtmlPlot2DLinesChart([(string.Empty, [.. xDifferences.Index().Select(t => new Point(t.Index, t.Item))])], string.Empty),
                    xFilterDifferences = new HtmlPlot2DLinesChart([(string.Empty, [.. xFilterDifferences.Index().Select(t => new Point(t.Index, t.Item))])], string.Empty),
                    RealUmPerPixel
                }), guid.LoggingHtml());

                #endregion

                #region Verify

                var imageAllPixelByteLength = ImageWidthPixel * heightPixelByteLength;
                var verifyStepAllPixelByteLength = (DieWidthUm / RealUmPerPixel) * heightPixelByteLength;
                Guard.IsEqualTo(MathHelper.SlideCountFull(imageAllPixelByteLength, verifyStepAllPixelByteLength, bodyBytesLength), ImageCount);

                logger.LogHtmlInformation("1.4. Verify", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    ImageWidthPixel,
                    ImageCount,
                }), guid.LoggingHtml());

                var verifyItems = new Item[ImageCount];
                foreach (var (index, pointer) in Enumerable
                             .Range(0, ImageCount)
                             .Select(t => t * verifyStepAllPixelByteLength)
                             .Select(Convert.ToInt64)
                             .Select(pointer => pointer - pointer % heightPixelByteLength) // verifyStepAllPixelByteLength是double, 不是整数倍, 需要对齐
                             .Index())
                {
                    verifyItems[index] = GetItem(pointer, imageAllPixelByteLength);
                    await ResolveAsync(verifyItems[index], false).ConfigureAwait(false);

                    if (verifyItems[index].IsOk == false) return false;
                }

                var verifyXDifferences = verifyItems
                    .Zip(verifyItems.Skip(1), (prev, next) => next.MatchPoint.X - prev.MatchPoint.X)
                    .ToArray();
                var verifyRealUmPerPixel = DieWidthUm / verifyXDifferences.Average();

                var errorPixel = Math.Abs(WaferDiameterUm / verifyRealUmPerPixel - WaferDiameterUm / RealUmPerPixel);
                var isOk = Math.Abs(verifyItems.Max(t => t.MatchPoint.X) - verifyItems.Min(t => t.MatchPoint.X)) <= VerifyThreasholdPixel
                           && errorPixel <= VerifyThreasholdPixel;

                logger.LogHtmlInformation("1.5. Verify Result", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    verifyItems = new HtmlPlot2DLinesChart([(string.Empty, [..verifyItems.Select(t => t.MatchPoint)])], string.Empty),
                    verifyXDifferences = new HtmlPlot2DLinesChart([(string.Empty, [.. verifyXDifferences.Index().Select(t => new Point(t.Index, t.Item))])], string.Empty),
                    verifyRealUmPerPixel,
                    errorPixel = $"{errorPixel:0.###}px/{WaferDiameterUm:0.###}um"
                }), guid.LoggingHtml());

                #endregion

                startNew.Stop();

                Console.WriteLine($"Split Image Success, Elapsed: {startNew.ElapsedMilliseconds} ms");

                return isOk;


                Item GetItem(long pointer, int allPixelByteLength)
                {
                    var isEnd = pointer + allPixelByteLength > bodyBytesLength;
                    var currentImageAllPixelByteLength = isEnd
                        ? Convert.ToInt32(bodyBytesLength - pointer)
                        : allPixelByteLength;
                    if (isEnd)
                    {
                        Guard.IsGreaterThan(currentImageAllPixelByteLength, 0);
                        Guard.IsEqualTo(currentImageAllPixelByteLength % heightPixelByteLength, 0);
                    }

                    var buffer = ArrayPool<byte>.Shared.Rent(currentImageAllPixelByteLength);

                    fileSteam.Seek(bodyBytesStartIndex + pointer, SeekOrigin.Begin);
                    Guard.IsEqualTo(binaryReader.Read(buffer, 0, currentImageAllPixelByteLength), currentImageAllPixelByteLength);

                    var item = new Item(pointer / heightPixelByteLength, buffer, new SizeI(currentImageAllPixelByteLength / heightPixelByteLength, heightPixel));

                    return item;
                }

                // ReSharper disable AccessToDisposedClosure

                async Task ResolveAsync(Item item, bool isOkLog)
                {
                    var (startPixel, buffer, sizeI) = item;

                    try
                    {
                        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                        using var image = TempRawImageFactory.CreateImage(buffer, sizeI);

                        var isMathOk = image.TryNccTemplateMathToOffset(templateId, out var matchPoint, out var score, out _);
                        item.IsOk = isMathOk && score >= ImageMatchThreshold;

                        item.MatchPoint = new Point(item.IsOk ? startPixel + matchPoint.X : startPixel, matchPoint.Y);
                        item.Score = score;

                        if (isOkLog == false || item.IsOk)
                        {
                            var title = item.IsOk ? $"{item.MatchPoint.X:0.###}px" : $"{startPixel:0.###}px";

                            var originImageFilePath = Path.Combine(detectImageDirectory, Path.GetFileNameWithoutExtension(TemplateImageFilePath), $"{title}.jpg");
                            image.Save(originImageFilePath);

                            var bullet = new HtmlBullet(new
                            {
                                item.StartPixel,
                                item.Size,
                                item.MatchPoint,
                                item.Score,
                                DeltaOfCenter = (item.StartPixel + item.Size.Width / 2d) - item.MatchPoint.X,
                                HtmlTab = new HtmlTab(new
                                {
                                    OriginImage = new HtmlImage(originImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(matchPoint, templateImageSize), new HtmlImageRectangleOverlay(matchPoint, templateImageSize)]),
                                    TemplateImage = new HtmlImage(TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                                })
                            });

                            if (item.IsOk)
                                logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header5, bullet, guid.LoggingHtml());
                            else
                                logger.LogHtmlError(title, HtmlHeaderLevelEnum.Header5, bullet, guid.LoggingHtml());
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                        semaphore.Release();
                    }
                }

                // ReSharper restore AccessToDisposedClosure
            }
            catch (Exception ex)
            {
                logger.LogHtmlError(ex, "SplitImage Error", HtmlHeaderLevelEnum.Header1, guid.LoggingHtml());

                return false;
            }
            finally
            {
                logger.LogHtmlInformation(guid.LoggedEndHtml(nameof(SplitImageWindowViewModel)));
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    private sealed record Item(long StartPixel, byte[] Buffer, SizeI Size)
    {
        public Point MatchPoint { get; set; }

        public double Score { get; set; }

        public bool IsOk { get; set; }
    }
}