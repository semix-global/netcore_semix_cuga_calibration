using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Services.Interfaces;
using HalconDotNet;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Channels;

namespace CugaCalibrationTest.ViewModels;

[IOCAppService(ServiceType = typeof(SplitImageWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class SplitImageWindowViewModel(
    ILogger<SplitImageWindowViewModel> logger,
    ICalibrationAlgorithmService calibrationAlgorithmService) : ViewModelBase
{
    [ObservableProperty]
    private string _templateFilePath = @"F:\TestRawPicture\1_5X_213f5129-3d2b-4baa-9a5c-26c9c7f899b4.ncc";

    [ObservableProperty]
    private string _templateImageFilePath = @"F:\TestRawPicture\1_5X_213f5129-3d2b-4baa-9a5c-26c9c7f899b4.jpg";

    [ObservableProperty]
    private double _dieWidthUm = 15300;

    [ObservableProperty]
    private int _slideWindowValue = 1000;

    [ObservableProperty]
    private int _slideStepValue = 100;

    [ObservableProperty]
    private double _imageMatchThreshold = 0.8;

    [ObservableProperty]
    private string _calUmPerPixelRawImageFilePath = @"F:\TestRawPicture\20250928_11470_0_0_1_short_841006_PMT08-CH3_8.raw";

    //[ObservableProperty]
    //private int _calUmPerPixelImageCount = 16;

    [ObservableProperty]
    private double _realUmPerPixel;

    [ObservableProperty]
    private string _splitRawImageFilePath = @"F:\TestRawPicture\20250928_11470_0_0_1_short_841006_PMT08-CH3_8.raw";

    [ObservableProperty]
    private int _splitWidthPixel = 1000;

    [ObservableProperty]
    private int _splitImageCount = 16;

    [RelayCommand]
    private async Task SplitImageAsync()
    {
        await Task.Run(async () =>
        {
            var guid = Guid.NewGuid();
            var detectImageDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", nameof(SplitImageWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));
            try
            {
                using var templateId = TemplateFilePath.ReadNccTemplate();
                var (_, templateImageSize) = ImageHelper.GetImageInfo(TemplateImageFilePath);

                #region Get Um Per Pixel

                logger.LogHtmlInformation("1. Get Um Per Pixel", HtmlHeaderLevelEnum.Header1, guid.LoggingHtml());
                logger.LogHtmlInformation("1.1 Param", HtmlHeaderLevelEnum.Header2, new HtmlQuote(new
                {
                    detectImageDirectory,
                    DieWidthUm,
                    SlideWindowValue,
                    SlideStepValue,
                    CalUmPerPixelRawImageFilePath,
                    //CalUmPerPixelCount = CalUmPerPixelImageCount,
                    TemplateImage = new HtmlImage(TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                }), guid.LoggingHtml());

                using var fileSteam = File.OpenRead(CalUmPerPixelRawImageFilePath);
                using var binaryReader = new BinaryReader(fileSteam, Encoding.UTF8, true);

                var (calUmPerPixelBodyBytesSize, calUmPerPixelBodyBytesStartIndex, calUmPerPixelBodyBytesLength) = RawImageFactory.GetSize(binaryReader);
                var (_, calUmPerPixelHeightPixel) = (SizeI)calUmPerPixelBodyBytesSize;

                //var calUmPerPixelDieWidthPixel = DieWidthUm / IdealUmPerPixel;
                //var calUmPerPixelSplitImageWidthPixel = Convert.ToInt32(calUmPerPixelDieWidthPixel) / 10;
                var calUmPerPixelSplitImageWidthPixel = SlideWindowValue;
                var calUmPerPixelHeightPixelByteLength = calUmPerPixelHeightPixel * 2;
                var calUmPerPixelSplitImageAllPixelByteLength = calUmPerPixelSplitImageWidthPixel * calUmPerPixelHeightPixelByteLength;

                // 预计算所有需要处理的块信息
                var blockInfos = new List<(int k, long pointerTemp, int byteLength, int currentWidthPixel, long calUmPerPixelImageLeftPixel)>();

                for (int k = 0; ; k++)
                {
                    long pointerTemp;
                    if (k == 0)
                        pointerTemp = 0;
                    else
                        pointerTemp = k * (SlideWindowValue * calUmPerPixelHeightPixelByteLength - (SlideWindowValue - SlideStepValue) * calUmPerPixelHeightPixelByteLength);

                    int byteLength;
                    bool isLastBlock = false;

                    if (pointerTemp + calUmPerPixelSplitImageAllPixelByteLength > calUmPerPixelBodyBytesLength)
                    {
                        var offset = SlideWindowValue - (calUmPerPixelBodyBytesLength - pointerTemp) / calUmPerPixelHeightPixelByteLength;
                        pointerTemp -= offset * calUmPerPixelHeightPixelByteLength;
                        pointerTemp -= pointerTemp % calUmPerPixelHeightPixelByteLength;

                        fileSteam.Seek(calUmPerPixelBodyBytesStartIndex + pointerTemp, SeekOrigin.Begin);
                        var tempArray = binaryReader.ReadRemainingBytes();
                        var offsetlast = tempArray.Length % calUmPerPixelHeightPixelByteLength;
                        fileSteam.Seek(calUmPerPixelBodyBytesStartIndex + pointerTemp + (tempArray.Length % calUmPerPixelHeightPixelByteLength), SeekOrigin.Begin);
                        byteLength = binaryReader.ReadRemainingBytes().Length;
                        isLastBlock = true;
                    }
                    else
                    {
                        byteLength = calUmPerPixelSplitImageAllPixelByteLength;
                    }

                    var currentWidthPixel = byteLength / calUmPerPixelHeightPixelByteLength;
                    var calUmPerPixelImageLeftPixel = pointerTemp / calUmPerPixelHeightPixelByteLength;

                    blockInfos.Add((k, pointerTemp, byteLength, currentWidthPixel, calUmPerPixelImageLeftPixel));

                    if (isLastBlock) break;
                }

                // 使用锁保护模板访问
                //var templateLock = new object();
                //var results = new ConcurrentBag<(int k, Point matchPoint, double score, long pointerTemp, int currentWidthPixel, long calUmPerPixelImageLeftPixel)>();
                //var scoreList = new ConcurrentBag<Point>();
                //var scoreCalibrateList = new ConcurrentBag<Point>();

                // 定义处理结果的数据结构
                var results = new ConcurrentBag<(int k, Point matchPoint, double score, long pointerTemp, int currentWidthPixel, long calUmPerPixelImageLeftPixel)>();
                var scoreList = new ConcurrentBag<Point>();
                var scoreCalibrateList = new ConcurrentBag<Point>();

                // 使用Channel实现生产者和消费者模式，带取消功能
                await ProcessWithChannelAndCancellationAsync(
                blockInfos,
                fileSteam,
                binaryReader,
                templateId,
                calUmPerPixelHeightPixel,
                calUmPerPixelBodyBytesStartIndex,
                results,
                scoreList,
                scoreCalibrateList);

                // 按原始顺序排序结果
                //var orderedResults = results.OrderBy(r => r.k).ToList();
                //var calUmPerPixelMatchPoint = orderedResults.Select(r => r.matchPoint).ToList();
                //var orderedScores = scoreList.OrderBy(s => s.X).ToList();
                //var orderedCalibrateScores = scoreCalibrateList.OrderBy(s => s.X).ToList();

                // 按原始顺序排序结果
                var orderedResults = results.OrderBy(r => r.k).ToList();
                var calUmPerPixelMatchPoint = orderedResults.Select(r => r.matchPoint).ToList();
                var orderedScores = scoreList.OrderBy(s => s.X).ToList();
                var orderedCalibrateScores = scoreCalibrateList.OrderBy(s => s.X).ToList();

                var calUmPerPixelMatchPointListValid = new List<Point>();
                var scoreListValid = new List<Point>();
                for (int j = 0; j < scoreList.Count; j++)
                {
                    if (orderedScores[j].Y > ImageMatchThreshold)
                    {
                        //scoreListValid.Add(orderedScores[j]);
                        scoreListValid.Add(orderedCalibrateScores[j]);
                        calUmPerPixelMatchPointListValid.Add(calUmPerPixelMatchPoint[j]);
                    }
                }

                //var differencesScoreList = scoreListValid
                //    .Zip(scoreListValid.Skip(1), (prev, curr) => curr.X - prev.X)
                //    .ToList();
                // 过滤掉Y值大于0.6的点
                scoreListValid = scoreListValid
                    .Where(point => point.Y > ImageMatchThreshold)
                    .ToList();

                /*{
                    double maxDelta = 1;
                    double minDelta = 0.001; // 最小差值阈值，避免完全相同的点
                                             // 检测连续相似点并标记要删除的第一个点
                    var indicesToRemove = scoreListValid
                        .Select((point, index) => new { Point = point, Index = index })
                        .Where((item, index) => index < scoreListValid.Count - 1) // 确保有下一个点
                        .Where(item =>
                        {
                            var nextPoint = scoreListValid[item.Index + 1];
                            double deltaX = Math.Abs(nextPoint.X - item.Point.X);
                            double deltaY = Math.Abs(nextPoint.Y - item.Point.Y);

                            // 差值既要小于容差，又要大于最小差值阈值
                            return deltaX < maxDelta && deltaX > minDelta &&
                                   deltaY < maxDelta && deltaY > minDelta;
                        })
                        .Select(item => item.Index) // 选择要删除的点的索引（连续相似点中的第一个）
                        .ToList();
                    // 从后往前删除标记的点，避免索引变化
                    foreach (int index in indicesToRemove.OrderByDescending(i => i))
                    {
                        scoreListValid.RemoveAt(index);
                    }
                    // 检测连续相似点并标记要删除的最后一个点
                    indicesToRemove = scoreListValid
                        .Select((point, index) => new { Point = point, Index = index })
                        .Where((item, index) => index > 0) // 确保有前一个点
                        .Where(item =>
                        {
                            var prevPoint = scoreListValid[item.Index - 1];
                            double deltaX = Math.Abs(item.Point.X - prevPoint.X);
                            double deltaY = Math.Abs(item.Point.Y - prevPoint.Y);

                            // 差值既要小于容差，又要大于最小差值阈值
                            return deltaX < maxDelta && deltaX > minDelta &&
                                   deltaY < maxDelta && deltaY > minDelta;
                        })
                        .Select(item => item.Index) // 选择要删除的点的索引（连续相似点中的最后一个）
                        .ToList();
                    // 从后往前删除标记的点，避免索引变化
                    foreach (int index in indicesToRemove.OrderByDescending(i => i))
                    {
                        scoreListValid.RemoveAt(index);
                    }
                }
                // 定义容差值和最小差值阈值
                {
                    double maxDelta = 1;
                    double minDelta = 0.001; // 最小差值阈值，避免完全相同的点
                                             // 检测连续相似点并标记要删除的第一个点
                    var indicesToRemove = calUmPerPixelMatchPointListValid
                        .Select((point, index) => new { Point = point, Index = index })
                        .Where((item, index) => index < calUmPerPixelMatchPointListValid.Count - 1) // 确保有下一个点
                        .Where(item =>
                        {
                            var nextPoint = calUmPerPixelMatchPointListValid[item.Index + 1];
                            double deltaX = Math.Abs(nextPoint.X - item.Point.X);
                            double deltaY = Math.Abs(nextPoint.Y - item.Point.Y);

                            // 差值既要小于容差，又要大于最小差值阈值
                            return deltaX < maxDelta && deltaX > minDelta &&
                                   deltaY < maxDelta && deltaY > minDelta;
                        })
                        .Select(item => item.Index) // 选择要删除的点的索引（连续相似点中的第一个）
                        .ToList();
                    // 从后往前删除标记的点，避免索引变化
                    foreach (int index in indicesToRemove.OrderByDescending(i => i))
                    {
                        calUmPerPixelMatchPointListValid.RemoveAt(index);
                    }
                    // 检测连续相似点并标记要删除的最后一个点
                    indicesToRemove = calUmPerPixelMatchPointListValid
                        .Select((point, index) => new { Point = point, Index = index })
                        .Where((item, index) => index > 0) // 确保有前一个点
                        .Where(item =>
                        {
                            var prevPoint = calUmPerPixelMatchPointListValid[item.Index - 1];
                            double deltaX = Math.Abs(item.Point.X - prevPoint.X);
                            double deltaY = Math.Abs(item.Point.Y - prevPoint.Y);

                            // 差值既要小于容差，又要大于最小差值阈值
                            return deltaX < maxDelta && deltaX > minDelta &&
                                   deltaY < maxDelta && deltaY > minDelta;
                        })
                        .Select(item => item.Index) // 选择要删除的点的索引（连续相似点中的最后一个）
                        .ToList();
                    // 从后往前删除标记的点，避免索引变化
                    foreach (int index in indicesToRemove.OrderByDescending(i => i))
                    {
                        calUmPerPixelMatchPointListValid.RemoveAt(index);
                    }
                }*/

                //List<Point> differencesMatchPointList = calUmPerPixelMatchPointListValid
                // .Zip(calUmPerPixelMatchPointListValid.Skip(1), (prev, curr) => new Point(curr.X - prev.X,curr.Y-prev.Y))
                // .Where(point => point.X >= 20 && point.X <= 100000)  // 过滤掉小于10和大于100000的差值
                // .ToList();
                List<Point> differencesMatchPointList = calUmPerPixelMatchPointListValid
                .Zip(calUmPerPixelMatchPointListValid.Skip(1), (prev, curr) => new Point(curr.X - prev.X, curr.Y - prev.Y))
                //.Where(point => point.X >= 20 && point.X <= 100000)  // 过滤掉小于10和大于100000的差值
                .ToList();
                List<double> differencesMatchPointXList = differencesMatchPointList.Select(p => p.X).Where(x => x > 1).ToList();
                // 先计算平均值（避免重复计算）
                var averageXList = differencesMatchPointXList.Average();
                differencesMatchPointXList = differencesMatchPointXList.Where(x => x >= averageXList).ToList();

                // 循环结束后，可以对scoreList进行分析
                // 例如：输出所有score值
                //logger.LogHtmlInformation("Match Point X Value Difference ", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                //{
                //    TraceBufferList = new HtmlPlot2DLinesChart([("differencesMatchPointList", differencesMatchPointList.Select(p => p.X).ToPoints())], string.Empty)
                //}), guid.LoggingHtml());
                logger.LogHtmlInformation("All Match Point Initial Scores", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    TraceBufferList = new HtmlPlot2DLinesChart([("orderedScoresInitialList", orderedCalibrateScores.ToArray())], string.Empty)
                }), guid.LoggingHtml());
                logger.LogHtmlInformation("All Match Point Process Scores", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    TraceBufferList = new HtmlPlot2DLinesChart([("orderedScoresProcessList", scoreListValid.ToArray())], string.Empty)
                }), guid.LoggingHtml());
                logger.LogHtmlInformation("Origin Match Point Analysis Point", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    TraceBufferList = new HtmlPlot2DLinesChart([("MatchPointList", calUmPerPixelMatchPointListValid.ToArray())], string.Empty)
                }), guid.LoggingHtml());
                logger.LogHtmlInformation("Origin Match Point Analysis X", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    TraceBufferList = new HtmlPlot2DLinesChart([("MatchPointList X", calUmPerPixelMatchPointListValid.Select(p => p.X).ToPoints())], string.Empty)
                }), guid.LoggingHtml());
                logger.LogHtmlInformation("Origin Match Point Analysis Y", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    TraceBufferList = new HtmlPlot2DLinesChart([("MatchPointList Y", calUmPerPixelMatchPointListValid.Select(p => p.Y).ToPoints())], string.Empty)
                }), guid.LoggingHtml());

                //logger.LogHtmlInformation("Process Match Point Analysis", HtmlHeaderLevelEnum.Header2, new HtmlQuote(new
                //{
                //    TraceBufferList = new HtmlPlot2DLinesChart([("MatchPointList", differencesMatchPointList.ToArray())], string.Empty)
                //}), guid.LoggingHtml());

                /*foreach (var (index, pointer) in calUmPerPixelPointerList.Select((t, i) => (Index: i, Pointer: t)))
                {
                    var pointerTemp = pointer - pointer % calUmPerPixelHeightPixelByteLength; // dieWidthPixel不是整数倍, 需要对齐

                    if (index > 0)
                    {
                        pointerTemp -= calUmPerPixelSplitImageAllPixelByteLength / 2;
                        pointerTemp -= pointerTemp % calUmPerPixelHeightPixelByteLength; // dieWidthPixel不是整数倍, 需要对齐
                    }

                    byte[] array;
                    if (pointerTemp + calUmPerPixelSplitImageAllPixelByteLength > calUmPerPixelBodyBytesLength)
                    {
                        if (index != calUmPerPixelPointerList.Count - 1) ThrowHelper.ThrowArgumentException("Data length is not a multiple of width.");

                        var offset = calUmPerPixelSplitImageWidthPixel - (calUmPerPixelBodyBytesLength - pointerTemp) / calUmPerPixelHeightPixelByteLength; // 算出右边差多少像素

                        pointerTemp += offset * calUmPerPixelHeightPixelByteLength; // 那么左边也去掉这么多像素
                        pointerTemp -= pointerTemp % calUmPerPixelHeightPixelByteLength; // dieWidthPixel不是整数倍, 需要对齐

                        fileSteam.Seek(calUmPerPixelBodyBytesStartIndex + pointerTemp, SeekOrigin.Begin);
                        array = binaryReader.ReadRemainingBytes();
                    }
                    else
                    {
                        fileSteam.Seek(calUmPerPixelBodyBytesStartIndex + pointerTemp, SeekOrigin.Begin);
                        array = binaryReader.ReadBytes(calUmPerPixelSplitImageAllPixelByteLength);
                    }

                    var currentWidthPixel = array.Length / calUmPerPixelHeightPixelByteLength;
                    var calUmPerPixelSplitImageRawBytes = calibrationAlgorithmService.ToRawBytes(array, new Size(currentWidthPixel, calUmPerPixelHeightPixel));
                    var (image, _, horizontalFlipRawBytes) = calibrationAlgorithmService.ToHorizontalFlipImageInfo(calUmPerPixelSplitImageRawBytes);

                    var calUmPerPixelImageLeftPixel = pointerTemp / calUmPerPixelHeightPixelByteLength;
                    using var _ = image;
                    var originImageFilePath = Path.Combine(detectImageDirectory, Path.GetFileNameWithoutExtension(TemplateImageFilePath), $"calUmPerPixelImage_{index + 1}.jpg");
                    image.Save(originImageFilePath);
                    image.TryNccTemplateMathToOffset(templateId, out var result, out var score, out var _);
                    var point = new Point(currentWidthPixel - result.X + calUmPerPixelImageLeftPixel, result.Y); // 水平翻转后的坐标
                    logger.LogHtmlInformation($"{index + 1}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        score,
                        LeftPixel = calUmPerPixelImageLeftPixel,
                        Point = point,
                        RawImageFile = new HtmlDownload(calUmPerPixelSplitImageRawBytes, $"calUmPerPixelImage_{index + 1}.raw"),
                        HtmlTab = new HtmlTab(new
                        {
                            OriginImage = new HtmlImage(originImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(result, templateImageSize), new HtmlImageRectangleOverlay(result, templateImageSize)]),
                            TemplateImage = new HtmlImage(TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    }), guid.LoggingHtml());

                    var pointScore = new Point(calUmPerPixelImageLeftPixel * 1.0, score);
                    scoreList = [.. scoreList, pointScore];
                    // 循环结束后，可以对scoreList进行分析
                    // 例如：输出所有score值
                    logger.LogHtmlInformation("Score Analysis", HtmlHeaderLevelEnum.Header2, new HtmlQuote(new
                    {
                        //Scores = scoreList.Select(s => new { Iteration = s.iteration, Score = s.score }).ToArray(),
                        //AverageScore = scoreList.Average(s => s.score),
                        //MinScore = scoreList.Min(s => s.score),
                        //MaxScore = scoreList.Max(s => s.score)
                        TraceBufferList = new HtmlPlot2DLinesChart([("scoreList", scoreList)], string.Empty)
                    }), guid.LoggingHtml());

                    calUmPerPixelMatchPoint.Add(point);
                }
                */
                //var calUmPerPixelDifferences = calUmPerPixelMatchPoint
                //    .Zip(calUmPerPixelMatchPoint.Skip(1), (prev, curr) => curr.X - prev.X)
                //    .ToList();
                //RealUmPerPixel = (DieWidthUm / Vector<double>.Build.DenseOfEnumerable(calUmPerPixelDifferences)).Average();
                //var error1 = calUmPerPixelDifferences.Max() - calUmPerPixelDifferences.Min();

                RealUmPerPixel = DieWidthUm / Vector<double>.Build.DenseOfEnumerable(differencesMatchPointXList).Average();
                //var errorX = differencesMatchPointList.Select(p => p.X).Max() - differencesMatchPointList.Select(p => p.X).Min();
                //var errorY = differencesMatchPointList.Select(p => p.Y).Max() - differencesMatchPointList.Select(p => p.Y).Min();
                var (calUmPerPixelWidthPixel, _) = (SizeI)calUmPerPixelBodyBytesSize;
                SplitImageCount = (int)(calUmPerPixelWidthPixel * 1.0 / (DieWidthUm / RealUmPerPixel));

                logger.LogHtmlInformation("1.2. End Split", HtmlHeaderLevelEnum.Header2, new HtmlQuote(new
                {
                    RealUmPerPixel = $"{RealUmPerPixel:f20}",
                    //Um = new HtmlPlot2DLinesChart([(nameof(differencesMatchPointList), differencesMatchPointList.ToArray())], nameof(differencesMatchPointList))
                    UmX = new HtmlPlot2DLinesChart([("differencesMatchPointListX", differencesMatchPointList.Select(p => p.X).ToPoints())], "differencesMatchPointListX"),
                    UmY = new HtmlPlot2DLinesChart([("differencesMatchPointListY", differencesMatchPointList.Select(p => p.Y).ToPoints())], "differencesMatchPointListY")
                }), guid.LoggingHtml());

                #endregion Get Um Per Pixel

                logger.LogHtmlInformation("2. Split Image", HtmlHeaderLevelEnum.Header1, guid.LoggingHtml());
                logger.LogHtmlInformation("2.1 Param", HtmlHeaderLevelEnum.Header2, new HtmlQuote(new
                {
                    detectImageDirectory,
                    DieWidthUm,
                    RealUmPerPixel = $"{RealUmPerPixel:f20}",
                    SplitRawImageFilePath,
                    SplitWidthPixel,
                    SplitCount = SplitImageCount,
                    TemplateImage = new HtmlImage(TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                }), guid.LoggingHtml());

                var (bodyBytesSize, bodyBytesStartIndex, bodyBytesLength) = RawImageFactory.GetSize(binaryReader);
                var (_, heightPixel) = (SizeI)bodyBytesSize;

                var dieWidthPixel = DieWidthUm / RealUmPerPixel;
                var heightPixelByteLength = heightPixel * 2;
                var splitImageAllPixelByteLength = SplitWidthPixel * heightPixelByteLength;

                var matchPoint = new List<Point>();
                var matchOffsetPoint = new List<Point>();
                var pointerList = Enumerable
                    .Range(0, SplitImageCount + 1)
                    .Select((count, index) => index == 0 ? 0 : count * dieWidthPixel * heightPixelByteLength)
                    .Select(Convert.ToInt64)
                    .ToList();
                foreach (var (index, pointer) in pointerList.Select((t, i) => (Index: i, Pointer: t)))
                {
                    var pointerTemp = pointer - pointer % heightPixelByteLength; // dieWidthPixel不是整数倍, 需要对齐
                    byte[] array;
                    if (pointerTemp + splitImageAllPixelByteLength > bodyBytesLength)
                    {
                        if (index != pointerList.Count - 1) ThrowHelper.ThrowArgumentException("Data length is not a multiple of width.");

                        var offset = SplitWidthPixel - (bodyBytesLength - pointerTemp) / heightPixelByteLength;

                        pointerTemp += offset * heightPixelByteLength;
                        pointerTemp -= pointerTemp % heightPixelByteLength; // dieWidthPixel不是整数倍, 需要对齐

                        fileSteam.Seek(bodyBytesStartIndex + pointerTemp, SeekOrigin.Begin);
                        array = binaryReader.ReadRemainingBytes();
                    }
                    else
                    {
                        fileSteam.Seek(bodyBytesStartIndex + pointerTemp, SeekOrigin.Begin);
                        array = binaryReader.ReadBytes(splitImageAllPixelByteLength);
                    }

                    var currentWidthPixel = array.Length / calUmPerPixelHeightPixelByteLength;
                    var size = new Size(currentWidthPixel, heightPixel);
                    var splitImageRawBytes = calibrationAlgorithmService.ToRawBytes(array, size);
                    var (image, _, horizontalFlipRawBytes) = calibrationAlgorithmService.ToHorizontalFlipImageInfo(splitImageRawBytes);

                    var calUmPerPixelImageLeftPixel = pointerTemp / calUmPerPixelHeightPixelByteLength;
                    using var _ = image;
                    var originImageFilePath = Path.Combine(detectImageDirectory, Path.GetFileNameWithoutExtension(TemplateImageFilePath), $"{index + 1}.jpg");
                    image.Save(originImageFilePath);
                    image.TryNccTemplateMathToOffset(templateId, out var result, out var score, out var _);
                    var point = new Point(currentWidthPixel - result.X + calUmPerPixelImageLeftPixel, result.Y); // 水平翻转后的坐标
                    matchPoint.Add(point);
                    matchOffsetPoint.Add(new Point(result.X, result.Y) - (Vector)size / 2);
                    logger.LogHtmlInformation($"{index + 1}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        score,
                        LeftPixel = calUmPerPixelImageLeftPixel,
                        Point = point,
                        RawImageFile = new HtmlDownload(splitImageRawBytes, $"{index + 1}.raw"),
                        HorizontalFlipRawImageFile = new HtmlDownload(horizontalFlipRawBytes, $"HorizontalFlip_{index + 1}.raw"),
                        HtmlTab = new HtmlTab(new
                        {
                            OriginImage = new HtmlImage(originImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(result, templateImageSize), new HtmlImageRectangleOverlay(result, templateImageSize)]),
                            TemplateImage = new HtmlImage(TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    }), guid.LoggingHtml());
                }

                var differences = matchPoint
                    .Zip(matchPoint.Skip(1), (prev, curr) => curr.X - prev.X)
                    .ToList();
                var error = differences.Max() - differences.Min();
                logger.LogHtmlInformation("2.2. End Split", HtmlHeaderLevelEnum.Header2, new HtmlQuote(new
                {
                    Point = new HtmlPlot2DLinesChart([(nameof(differences), differences.ToPoints())], nameof(differences)),
                    OffsetPointX = new HtmlPlot2DLinesChart([(nameof(matchOffsetPoint), matchOffsetPoint.Select(t => t.X).ToPoints())], nameof(differences)),
                    OffsetPointY = new HtmlPlot2DLinesChart([(nameof(matchOffsetPoint), matchOffsetPoint.Select(t => t.Y).ToPoints())], nameof(differences))
                }), guid.LoggingHtml());
            }
            catch (Exception ex)
            {
                logger.LogHtmlError(ex, "SplitImage Error", HtmlHeaderLevelEnum.Header1, guid.LoggingHtml());
            }
            finally
            {
                logger.LogHtmlInformation(guid.LoggedEndHtml(nameof(SplitImageWindowViewModel)));
            }
        }).ConfigureAwait(false);
    }

    // 新增的Channel处理方法
    private async Task ProcessWithChannelAndCancellationAsync(
        List<(int k, long pointerTemp, int byteLength, int currentWidthPixel, long calUmPerPixelImageLeftPixel)> blockInfos,
        FileStream fileSteam,
        BinaryReader binaryReader,
        HNCCModel templateId,
        int calUmPerPixelHeightPixel,
        long calUmPerPixelBodyBytesStartIndex,
        ConcurrentBag<(int k, Point matchPoint, double score, long pointerTemp, int currentWidthPixel, long calUmPerPixelImageLeftPixel)> results,
        ConcurrentBag<Point> scoreList,
        ConcurrentBag<Point> scoreCalibrateList,
        CancellationToken cancellationToken = default)
    {
        // 定义Channel中传递的数据结构
        var channel = Channel.CreateBounded<(int k, long pointerTemp, int byteLength, int currentWidthPixel, long calUmPerPixelImageLeftPixel, byte[] imageData)>(
            new BoundedChannelOptions(Environment.ProcessorCount * 2)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });

        // 创建模板匹配锁
        var templateLock = new object();

        // 生产者任务 - 读取图像数据
        var producerTask = Task.Run(async () =>
        {
            try
            {
                foreach (var blockInfo in blockInfos)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var (k, pointerTemp, byteLength, currentWidthPixel, calUmPerPixelImageLeftPixel) = blockInfo;

                    byte[] array;
                    lock (fileSteam) // 文件读取需要加锁
                    {
                        fileSteam.Seek(calUmPerPixelBodyBytesStartIndex + pointerTemp, SeekOrigin.Begin);
                        array = binaryReader.ReadBytes(byteLength);
                    }

                    // 将数据发送到Channel
                    await channel.Writer.WriteAsync((k, pointerTemp, byteLength, currentWidthPixel, calUmPerPixelImageLeftPixel, array), cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("生产者任务被取消");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "生产者任务发生错误");
            }
            finally
            {
                channel.Writer.Complete();
            }
        });

        // 消费者任务 - 处理图像匹配
        var consumerTasks = new List<Task>();
        var consumerCount = Math.Max(1, Environment.ProcessorCount - 1); // 保留一个核心给其他任务

        for (int i = 0; i < consumerCount; i++)
        {
            var consumerTask = Task.Run(async () =>
            {
                try
                {
                    await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
                    {
                        try
                        {
                            await ProcessBlockDataAsync(
                                item.k,
                                item.pointerTemp,
                                item.byteLength,
                                item.currentWidthPixel,
                                item.calUmPerPixelImageLeftPixel,
                                item.imageData,
                                templateId,
                                calUmPerPixelHeightPixel,
                                results,
                                scoreList,
                                scoreCalibrateList,
                                templateLock,
                                cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            throw; // 重新抛出取消异常
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "处理块 {Index} 时发生错误", item.k);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("消费者任务被取消");
                }
                catch (ChannelClosedException)
                {
                    // Channel正常关闭，不是错误
                }
            });

            consumerTasks.Add(consumerTask);
        }

        // 等待所有任务完成
        try
        {
            await Task.WhenAll(consumerTasks);
            await producerTask;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("图像处理流程被取消");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "图像处理流程发生错误");
            throw;
        }
    }

    // 处理单个图像块的异步方法   
    private async Task ProcessBlockDataAsync(
        int k,
        long pointerTemp,
        int byteLength,
        int currentWidthPixel,
        long calUmPerPixelImageLeftPixel,
        byte[] imageData,
        HNCCModel templateId,
        int calUmPerPixelHeightPixel,
        ConcurrentBag<(int k, Point matchPoint, double score, long pointerTemp, int currentWidthPixel, long calUmPerPixelImageLeftPixel)> results,
        ConcurrentBag<Point> scoreList,
        ConcurrentBag<Point> scoreCalibrateList,
        object templateLock,
        CancellationToken cancellationToken)
    {
        // 立即让出控制权，避免阻塞消费者线程
        await Task.Yield();

        cancellationToken.ThrowIfCancellationRequested();
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum.Ncc;

        // 处理图像数据
        var calUmPerPixelSplitImageRawBytes = calibrationAlgorithmService.ToRawBytes(
            imageData,
            new Size(currentWidthPixel, calUmPerPixelHeightPixel));

        var (image, _, horizontalFlipRawBytes) = calibrationAlgorithmService.ToHorizontalFlipImageInfo(calUmPerPixelSplitImageRawBytes);

        using (image)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 使用锁保护模板匹配操作
            Point result;
            double score;
            lock (templateLock)
            {
                cancellationToken.ThrowIfCancellationRequested();
                image.TryNccTemplateMathToOffset(templateId, out result, out score, out var _);
                //HalconHelper.TryNccTemplateMathToOffset(image, templateId, out result, out score, out var _);
                //calibrationAlgorithmService.TryTemplateMatchToOffset(algorithmTemplateTypeEnum, image, templateId, out result, out var _, out score, out _);

            }

            var point = new Point(currentWidthPixel - result.X + calUmPerPixelImageLeftPixel, result.Y);
            var pointScore = new Point(calUmPerPixelImageLeftPixel * 1.0, score);

            results.Add((k, point, score, pointerTemp, currentWidthPixel, calUmPerPixelImageLeftPixel));
            scoreList.Add(pointScore);
            scoreCalibrateList.Add(new Point(currentWidthPixel - result.X + calUmPerPixelImageLeftPixel * 1.0, score));

            // 可选：记录处理日志
            if (k % 10 == 0) // 每10个块记录一次，避免日志过多
            {
                logger.LogDebug("已处理块 {BlockIndex}, 分数: {Score}", k, score);
            }
        }
    }

}


