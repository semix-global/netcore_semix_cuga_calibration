using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Services.Interfaces;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;

namespace CugaCalibrationTest.ViewModels;

[IOCAppService(ServiceType = typeof(SplitImageWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class SplitImageWindowViewModel(
    ILogger<SplitImageWindowViewModel> logger,
    ICalibrationAlgorithmService calibrationAlgorithmService) : ViewModelBase
{
    [ObservableProperty]
    private string _templateFilePath = @"C:\Users\DELL\Documents\1_Magnification5X_56f9f947-fbcd-4e38-891e-24e368cf63c7.ncc";

    [ObservableProperty]
    private string _templateImageFilePath = @"C:\Users\DELL\Documents\1_Magnification5X_56f9f947-fbcd-4e38-891e-24e368cf63c7.jpg";

    [ObservableProperty]
    private double _dieWidthUm = 5100;

    [ObservableProperty]
    private double _idealUmPerPixel = 0.334;

    [ObservableProperty]
    private string _calUmPerPixelRawImageFilePath = @"C:\Users\DELL\Documents\20250430_42_0_0_1_short_519624_PMT08-CH3_8.raw";

    [ObservableProperty]
    private int _calUmPerPixelImageCount = 15;

    [ObservableProperty]
    private double _realUmPerPixel;

    [ObservableProperty]
    private string _splitRawImageFilePath = @"C:\Users\DELL\Documents\20250430_42_0_0_1_short_519624_PMT08-CH3_8.raw";

    [ObservableProperty]
    private int _splitWidthPixel = 1000;

    [ObservableProperty]
    private int _splitImageCount = 15;

    [RelayCommand]
    private async Task SplitImageAsync()
    {
        await Task.Run(() =>
        {
            var guid = Guid.NewGuid();
            var detectImageDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", nameof(SplitImageWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));
            try
            {
                using var templateId = HalconHelper.ReadNccTemplate(TemplateFilePath);
                var (_, templateImageSize) = ImageHelper.GetImageInfo(TemplateImageFilePath);

                #region Get Um Per Pixel

                logger.LogHtmlInformation("1. Get Um Per Pixel", HtmlHeaderLevelEnum.Header1, guid.LoggingHtml());
                logger.LogHtmlInformation("1.1 Param", HtmlHeaderLevelEnum.Header2, new HtmlQuote(new
                {
                    detectImageDirectory,
                    DieWidthUm,
                    IdealUmPerPixel,
                    CalUmPerPixelRawImageFilePath,
                    CalUmPerPixelCount = CalUmPerPixelImageCount,
                    TemplateImage = new HtmlImage(TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                }), guid.LoggingHtml());

                var calUmPerPixelRawBytes = File.ReadAllBytes(CalUmPerPixelRawImageFilePath);
                var (calUmPerPixelBodyBytesSize, calUmPerPixelBodyBytesStartIndex, calUmPerPixelBodyBytesLength) = calibrationAlgorithmService.GetSize(calUmPerPixelRawBytes);
                var (_, calUmPerPixelHeightPixel) = calUmPerPixelBodyBytesSize.DeconstructToInt32();

                var calUmPerPixelDieWidthPixel = DieWidthUm / IdealUmPerPixel;
                var calUmPerPixelSplitImageWidthPixel = Convert.ToInt32(calUmPerPixelDieWidthPixel) / 10;
                var calUmPerPixelHeightPixelByteLength = calUmPerPixelHeightPixel * 2;
                var calUmPerPixelSplitImageAllPixelByteLength = calUmPerPixelSplitImageWidthPixel * calUmPerPixelHeightPixelByteLength;

                ReadOnlySpan<byte> calUmPerPixelSpan = calUmPerPixelRawBytes.AsSpan().Slice(calUmPerPixelBodyBytesStartIndex, calUmPerPixelBodyBytesLength);

                var calUmPerPixelMatchPoint = new List<Point>();

                var calUmPerPixelPointerList = Enumerable
                    .Range(0, CalUmPerPixelImageCount)
                    .Select(t => 0 + t * calUmPerPixelDieWidthPixel * calUmPerPixelHeightPixelByteLength)
                    .Select(Convert.ToInt32)
                    .ToList(); // 分割指针集合
                foreach (var (index, pointer) in calUmPerPixelPointerList.Select((t, i) => (Index: i, Pointer: t)))
                {
                    var pointerTemp = pointer - pointer % calUmPerPixelHeightPixelByteLength; // dieWidthPixel不是整数倍, 需要对齐
                    if (index > 0)
                    {
                        pointerTemp -= calUmPerPixelSplitImageAllPixelByteLength / 2;
                    }

                    byte[] array;
                    if (pointerTemp + calUmPerPixelSplitImageAllPixelByteLength > calUmPerPixelSpan.Length)
                    {
                        if (index != calUmPerPixelPointerList.Count - 1) ThrowHelper.ThrowArgumentException("Data length is not a multiple of width.");

                        var offset = calUmPerPixelSplitImageWidthPixel - (calUmPerPixelSpan.Length - pointerTemp) / calUmPerPixelHeightPixelByteLength; // 算出右边差多少像素

                        pointerTemp += offset * calUmPerPixelHeightPixelByteLength; // 那么左边也去掉这么多像素
                        array = calUmPerPixelSpan[pointerTemp..].ToArray();
                    }
                    else
                    {
                        array = calUmPerPixelSpan.Slice(pointerTemp, calUmPerPixelSplitImageAllPixelByteLength).ToArray();
                    }

                    var currentWidthPixel = array.Length / calUmPerPixelHeightPixelByteLength;
                    var calUmPerPixelSplitImageRawBytes = calibrationAlgorithmService.ToRawBytes(array, new Size(currentWidthPixel, calUmPerPixelHeightPixel));
                    var (image, _, horizontalFlipRawBytes) = calibrationAlgorithmService.ToHorizontalFlipImageInfo(calUmPerPixelSplitImageRawBytes);

                    var calUmPerPixelImageLeftPixel = pointerTemp / calUmPerPixelHeightPixelByteLength;
                    using var _ = image;
                    var originImageFilePath = Path.Combine(detectImageDirectory, Path.GetFileNameWithoutExtension(TemplateImageFilePath), $"calUmPerPixelImage_{index + 1}.jpg");
                    HalconHelper.Save(image, originImageFilePath);
                    HalconHelper.TryNccTemplateMathToOffset(image, templateId, out var result, out var score, out var _);
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

                    calUmPerPixelMatchPoint.Add(point);
                }

                var calUmPerPixelDifferences = calUmPerPixelMatchPoint
                    .Zip(calUmPerPixelMatchPoint.Skip(1), (prev, curr) => curr.X - prev.X)
                    .ToList();
                RealUmPerPixel = (DieWidthUm / Vector<double>.Build.DenseOfEnumerable(calUmPerPixelDifferences)).Average();
                var error1 = calUmPerPixelDifferences.Max() - calUmPerPixelDifferences.Min();
                logger.LogHtmlInformation("1.2. End Split", HtmlHeaderLevelEnum.Header2, new HtmlQuote(new
                {
                    RealUmPerPixel = $"{RealUmPerPixel:f20}",
                    Um = new HtmlPlot2DLinesChart([(nameof(calUmPerPixelDifferences), calUmPerPixelDifferences.ToPoints())], nameof(calUmPerPixelDifferences))
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
                    TemplateImage = new HtmlImage(TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                }), guid.LoggingHtml());

                var rawBytes = File.ReadAllBytes(SplitRawImageFilePath);
                var (bodyBytesSize, bodyBytesStartIndex, bodyBytesLength) = calibrationAlgorithmService.GetSize(rawBytes);
                var (_, heightPixel) = bodyBytesSize.DeconstructToInt32();

                ReadOnlySpan<byte> span = rawBytes.AsSpan().Slice(bodyBytesStartIndex, bodyBytesLength);

                var dieWidthPixel = DieWidthUm / RealUmPerPixel;
                var heightPixelByteLength = heightPixel * 2;
                var splitImageAllPixelByteLength = SplitWidthPixel * heightPixelByteLength;

                var matchPoint = new List<Point>();
                var matchOffsetPoint = new List<Point>();
                var pointerList = Enumerable
                    .Range(0, SplitImageCount)
                    .Select((count, index) => index == 0 ? 0 : count * dieWidthPixel * heightPixelByteLength)
                    .Select(Convert.ToInt32)
                    .ToList();
                foreach (var (index, pointer) in pointerList.Select((t, i) => (Index: i, Pointer: t)))
                {
                    var pointerTemp = pointer - pointer % heightPixelByteLength; // dieWidthPixel不是整数倍, 需要对齐
                    byte[] array;
                    if (pointerTemp + splitImageAllPixelByteLength > rawBytes.Length)
                    {
                        if (index != pointerList.Count - 1) ThrowHelper.ThrowArgumentException("Data length is not a multiple of width.");

                        var offset = SplitWidthPixel - (rawBytes.Length - pointerTemp) / heightPixelByteLength;

                        pointerTemp += offset * heightPixelByteLength;
                        array = span[pointerTemp..].ToArray();
                    }
                    else
                    {
                        array = span.Slice(pointerTemp, splitImageAllPixelByteLength).ToArray();
                    }

                    var currentWidthPixel = array.Length / calUmPerPixelHeightPixelByteLength;
                    var size = new Size(currentWidthPixel, heightPixel);
                    var splitImageRawBytes = calibrationAlgorithmService.ToRawBytes(array, size);
                    var (image, _, horizontalFlipRawBytes) = calibrationAlgorithmService.ToHorizontalFlipImageInfo(splitImageRawBytes);

                    var calUmPerPixelImageLeftPixel = pointerTemp / calUmPerPixelHeightPixelByteLength;
                    using var _ = image;
                    var originImageFilePath = Path.Combine(detectImageDirectory, Path.GetFileNameWithoutExtension(TemplateImageFilePath), $"{index + 1}.jpg");
                    HalconHelper.Save(image, originImageFilePath);
                    HalconHelper.TryNccTemplateMathToOffset(image, templateId, out var result, out var score, out var _);
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
                logger.LogHtmlInformation("1.2. End Split", HtmlHeaderLevelEnum.Header2, new HtmlQuote(new
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
}