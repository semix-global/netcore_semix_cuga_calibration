using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Microscope.PixelSize;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using HalconDotNet;
using Local.SQL.Cache.Providers.Extensions;
using Local.SQL.Cache.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;
using R3;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(ReviewViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ReviewViewModel(
    ICalibrationReviewService calibrationReviewService,
    StageViewModel stageViewModel,
    MicroscopeViewModel microscopeViewModel,
    ILogger<ReviewViewModel> logger,
    IDialogWindowProvider dialogWindowProvider,
    ICacheProvider cacheProvider,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    CalibrationSetting calibrationSetting)
    : ViewModelBase
{
    private long _frameCount;

    [ObservableProperty]
    private byte[] _bitmapMemoryByteArray = [];

    [ObservableProperty]
    private double _quality;

    [ObservableProperty]
    private double _fps;

    #region 服务

    public bool Connect()
    {
        var ret = calibrationReviewService.Connect();

        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public byte[] GetBrightFieldImageMemoryByteArray()
    {
        var ret = calibrationReviewService.GetBrightFieldImageMemoryByteArray();

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        _frameCount++;
        BitmapMemoryByteArray = ret.Anything;
        GC.Collect();
        return ret.Anything;
    }

    public HImage GetBrightFieldImage()
    {
        var ret = calibrationReviewService.GetBrightFieldImage();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SaveCurrentBrightFieldImage(string filePath)
    {
        using var imageMatchResult = GetBrightFieldImage();

        imageMatchResult.Save(filePath);
    }

    public Size GetBrightFieldImagePixelSize()
    {
        var ret = calibrationReviewService.GetBrightFieldImagePixelSize();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public double GetQuality(HImage image)
    {
        var quality = calibrationAlgorithmService.GetQuality(image);
        return quality;
    }

    #region 模板匹配

    /// <summary>
    /// 生成模板
    /// </summary>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="templateFilePath">模板路径</param>
    /// <param name="algorithmTemplateSizeEnum">模板尺寸</param>
    /// <returns>是否成功</returns>
    public bool TryGenerateTemplate(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, string templateFilePath, AlgorithmTemplateSizeEnum algorithmTemplateSizeEnum)
    {
        using var image = GetBrightFieldImage();

        var size = image.GetSize();
        var rect = new Rect(size.Width / 2d - Convert.ToInt32(algorithmTemplateSizeEnum) / 2d, size.Height / 2d - Convert.ToInt32(algorithmTemplateSizeEnum) / 2d, Convert.ToInt32(algorithmTemplateSizeEnum), Convert.ToInt32(algorithmTemplateSizeEnum));

        return TryGenerateTemplate(image, algorithmTemplateTypeEnum, templateFilePath, rect);
    }

    /// <summary>
    /// 生成模板
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="templateFilePath">模板路径</param>
    /// <param name="rect">ROI尺寸</param>
    /// <returns>是否成功</returns>
    public bool TryGenerateTemplate(HImage image, AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, string templateFilePath, Rect rect)
    {
        var size = image.GetSize();
        if (new Rect(Point.Origin, size).Contains(rect) == false)
        {
            logger.LogError("{@Name}: Out of Image Area", nameof(ReviewViewModel));
            return false;
        }

        var isSuccess = calibrationAlgorithmService.TryGenerateTemplate(algorithmTemplateTypeEnum, image, templateFilePath, rect, out var roiImage);
        using var _ = roiImage;
        if (isSuccess == false) throw new AlgorithmException("Generate Template Error");

        roiImage.Save(CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath));
        return true;
    }

    /// <summary>
    /// 生成模板
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="templateFilePath">模板路径</param>
    /// <returns>是否成功</returns>
    public bool TryGenerateProjectionTemplate(HImage image, string templateFilePath)
    {
        var isSuccess = calibrationAlgorithmService.TryGenerateProjectionTemplate(image, templateFilePath, out var roiImage);
        using var _ = roiImage;
        if (isSuccess == false) throw new AlgorithmException("Generate Projection Template Error");

        roiImage.Save(CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath));
        return true;
    }

    /// <summary>
    /// 匹配模板: 从旧位置到匹配后位置
    /// </summary>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="microscopePixelSizeHistoryList">显微镜尺寸列表</param>
    /// <param name="position">匹配旧的位置</param>
    /// <param name="microscopeLensInformation">匹配的镜头</param>
    /// <param name="templateFilePath">匹配的模板</param>
    /// <param name="saveResultImageFileDirectory">匹配后成功的[保存的匹配图片的文件目录]</param>
    /// <param name="logGuid">记录日志: id</param>
    /// <param name="logName">记录日志: 名称</param>
    /// <param name="logResultTitle">记录日志: 匹配后成功的[标题]</param>
    /// <param name="resultPosition">匹配后成功的[位置]</param>
    /// <param name="resultScore">匹配后成功的[得分]</param>
    /// <param name="resultAngle">匹配后成功的[角度]</param>
    /// <param name="resultImageFilePath">匹配后成功的[保存的匹配图片的路径]</param>
    /// <param name="originImageFilePath">匹配前原图[保存的原图图片的路径]</param>
    /// <param name="calChipSiteModelEnum"></param>
    /// <returns>是否成功</returns>
    public bool TryGetMatchPosition(
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum,
        IEnumerable<MicroscopePixelSizeItemDto> microscopePixelSizeHistoryList,
        Point position,
        MicroscopeLensInformation microscopeLensInformation,
        string templateFilePath,
        string? saveResultImageFileDirectory,
        Guid? logGuid,
        string? logName,
        string? logResultTitle,
        out Point resultPosition,
        out double resultScore,
        out double resultAngle,
        out string resultImageFilePath,
        out string originImageFilePath,
        CalChipSiteModelEnum calChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel)
    {
        resultPosition = Point.Origin;
        resultScore = 0;
        resultAngle = 0;
        resultImageFilePath = string.Empty;
        originImageFilePath = string.Empty;

        var size = microscopePixelSizeHistoryList.SingleOrDefault(t => t.LensInformation == microscopeLensInformation);

        Size pixelSize;
        if (size is null || size.IsOk == false)
        {
            if (logGuid is not null && logName is not null)
                logger.LogHtmlWarning("Warning", HtmlHeaderLevelEnum.Header5, new HtmlComment($"{logName} Error: Microscope Pixel Size is Empty or not verify."), logGuid.Value.LoggingHtml());
            else logger.LogWarning("{@Name}: Microscope Pixel Size is Empty or not verify", nameof(ReviewViewModel));

            pixelSize = new Size(3.45 / microscopeLensInformation.ObjectiveMagnification, 3.45 / microscopeLensInformation.ObjectiveMagnification);
        }
        else pixelSize = size.PixelSize;

        var isSuccess = calibrationAlgorithmService.TryReadTemplate(algorithmTemplateTypeEnum, templateFilePath, out var templateId);
        using var _1 = templateId;
        if (isSuccess == false)
        {
            if (logGuid is not null && logName is not null)
                logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"{logName} Error: Read Template Failed!"), logGuid.Value.LoggingHtml());
            else logger.LogError("{@Name}: Read Template Failed!", nameof(ReviewViewModel));

            return false;
        }

        try
        {
            var currentMag = microscopeViewModel.GetCurrentMicroscopeLensInformation();
            if (microscopeLensInformation != currentMag)
                microscopeViewModel.SwitchMicroscopeLensInformation(microscopeLensInformation);
            stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(position, calChipSiteModelEnum);
            Thread.Sleep(500);

            using var image = GetBrightFieldImage();

            isSuccess = calibrationAlgorithmService.TryTemplateMatchToOffset(algorithmTemplateTypeEnum, image, templateId, out var resultPoint, out var offset, out resultScore, out resultAngle);
            if (isSuccess == false)
            {
                var templateMatchScoreThreshold = algorithmTemplateTypeEnum.ToTemplateMatchScoreThreshold(calibrationSetting);
                originImageFilePath = $"{FileHelper.GetFileFullName(templateFilePath)}_Error\\Score({resultScore:f3},{templateMatchScoreThreshold})_Angle{resultAngle:f3}_Origin_Guid({logGuid ?? Guid.NewGuid()}).jpg";
                image.Save(originImageFilePath);
                if (logGuid is not null && logName is not null)
                    logger.LogHtmlError($"{logName} Error: Try Math Template To Offset Failed.{logResultTitle}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                    {
                        microscopeLensInformation.LensName,
                        OriginPosition = position,
                        Score = resultScore,
                        TemplateMatchScoreThreshold = templateMatchScoreThreshold,
                        HtmlTab = new HtmlTab(new
                        {
                            TemplateImage = new HtmlImage(CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath), htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                            OriginImage = new HtmlImage(originImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    }), logGuid.Value.LoggingHtml());
                else logger.LogError("{@Name}: Error: Try Math Template To Offset Failed", nameof(ReviewViewModel));

                return false;
            }

            var actualOffset = new Point(offset.X * pixelSize.Width, offset.Y * pixelSize.Height);
            stageViewModel.MoveRelativeStageXy(actualOffset);
            Thread.Sleep(500);
            var tempResultPosition = stageViewModel.GetBrightFieldStagePosition();

            if (saveResultImageFileDirectory is not null)
            {
                originImageFilePath = $"{saveResultImageFileDirectory}_Score({resultScore:f3})_Angle{resultAngle:f3}_Origin_Guid({logGuid ?? Guid.NewGuid()})_{DateTime.Now.ToString(Constants.LongFileDateTimeFormat)}.jpg";
                image.Save(originImageFilePath);

                resultImageFilePath = $"{saveResultImageFileDirectory}_Score({resultScore:f3})_Angle{resultAngle:f3}_Result_Guid({logGuid ?? Guid.NewGuid()})_{DateTime.Now.ToString(Constants.LongFileDateTimeFormat)}.jpg";
                SaveCurrentBrightFieldImage(resultImageFilePath);
            }

            resultPosition = tempResultPosition;

            if (logGuid is not null && logName is not null && logResultTitle is not null)
                logger.LogHtmlInformation($"Match Template {logResultTitle}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                {
                    microscopeLensInformation.LensName,
                    OriginPosition = position,
                    ResultPosition = resultPosition,
                    ResultOffset = actualOffset,
                    ResultPoint = resultPoint,
                    ResultScore = resultScore,
                    ResultAngle = resultAngle,
                    HtmlTab = new HtmlTab(new
                    {
                        OriginImage = new HtmlImage(originImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(resultPoint)]),
                        TemplateImage = new HtmlImage(CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath), htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                }), logGuid.Value.LoggingHtml());
            return true;
        }
        finally
        {
            var tryCleanTemplate = calibrationAlgorithmService.TryCleanTemplate(algorithmTemplateTypeEnum, templateId);
            if (tryCleanTemplate == false)
            {
                if (logGuid is not null && logName is not null)
                    logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"{logName} Error: Clean Template Failed."), logGuid.Value.LoggingHtml());
                else logger.LogError("{@Name}: Clean Template Failed", nameof(ReviewViewModel));
            }
        }
    }

    /// <summary>
    /// 记录日志的匹配模板
    /// </summary>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="microscopePixelSizeHistoryList">显微镜尺寸列表</param>
    /// <param name="position">匹配旧的位置</param>
    /// <param name="microscopeLensInformation">匹配的镜头</param>
    /// <param name="templateFilePath">匹配的模板</param>
    /// <param name="resultPosition">匹配后成功的[位置]</param>
    /// <returns>是否成功</returns>
    public bool TryGetMatchPosition(
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum,
        IEnumerable<MicroscopePixelSizeItemDto> microscopePixelSizeHistoryList,
        Point position,
        MicroscopeLensInformation microscopeLensInformation,
        string templateFilePath,
        out Point resultPosition)
    {
        return TryGetMatchPosition(algorithmTemplateTypeEnum, microscopePixelSizeHistoryList, position, microscopeLensInformation, templateFilePath, null, null, null, null,
            out resultPosition, out _, out _, out _, out _);
    }

    /// <summary>
    /// 记录日志的匹配模板
    /// </summary>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="microscopePixelSizeHistoryList">显微镜尺寸列表</param>
    /// <param name="position">匹配的位置</param>
    /// <param name="microscopeLensInformation">镜头</param>
    /// <param name="templateFilePath">模板</param>
    /// <param name="resultPosition">匹配后成功的[位置]</param>
    /// <param name="resultScore">匹配后成功的[得分]</param>
    /// <param name="resultAngle">匹配后成功的[角度]</param>
    /// <param name="resultImageFilePath">匹配后成功的[保存的匹配图片的路径]</param>
    /// <param name="originImageFilePath">匹配前原图[保存的原图图片的路径]</param>
    /// <returns>是否成功</returns>
    public bool TryGetMatchPosition(
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum,
        IEnumerable<MicroscopePixelSizeItemDto> microscopePixelSizeHistoryList,
        Point position,
        MicroscopeLensInformation microscopeLensInformation,
        string templateFilePath,
        out Point resultPosition,
        out double resultScore,
        out double resultAngle,
        out string resultImageFilePath,
        out string originImageFilePath)
    {
        return TryGetMatchPosition(algorithmTemplateTypeEnum, microscopePixelSizeHistoryList, position, microscopeLensInformation, templateFilePath, null, null, null, null,
            out resultPosition, out resultScore, out resultAngle, out resultImageFilePath, out originImageFilePath);
    }

    #endregion 模板匹配

    public void Monitor(CancellationToken cancellationToken)
    {
#pragma warning disable IDE0079
#pragma warning disable IDISP001
        var fpsMonitor = Observable.Interval(TimeSpan.FromMilliseconds(CalibrationConstantsHelper.FpsMonitorMilliseconds), cancellationToken).Subscribe(_ =>
        {
            Fps = _frameCount / (CalibrationConstantsHelper.FpsMonitorMilliseconds / 1000d);
            _frameCount = 0;
        });
        cancellationToken.Register(fpsMonitor.Dispose);
#pragma warning restore IDISP001
#pragma warning restore IDE0079
    }

    #endregion 服务

    #region Command

    [RelayCommand]
    private async Task GetQualityAsync()
    {
        await Task.Run(() =>
        {
            using var hImage = GetBrightFieldImage();
            Quality = GetQuality(hImage);
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private void Review(string filePath)
    {
        try
        {
            if (File.Exists(filePath) == false)
            {
                dialogWindowProvider.ShowDialog("The file does not exist.", DialogButtonsEnum.OK, DialogIconEnum.Error);
                return;
            }

            dialogWindowProvider.ShowImage(filePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name}: Review failed", nameof(ReviewViewModel));
        }
    }

    [RelayCommand]
    private async Task MousePilotAsync(Point? point)
    {
        await Task.Run(() =>
        {
            if (point is null) return;

            var microscopePixelSizes = cacheProvider.GetOrDefaultArray<MicroscopePixelSizeItemDto>();
            var currentMicroscopeLensInformation = microscopeViewModel.GetCurrentMicroscopeLensInformation();
            var size = microscopePixelSizes.SingleOrDefault(t => t.LensInformation == currentMicroscopeLensInformation);

            Size pixelSize;
            if (size is null || size.IsOk == false)
            {
                logger.LogWarning("{@Name}: Microscope Pixel Size is Empty or not verify", nameof(ReviewViewModel));
                pixelSize = new Size(3.45 / currentMicroscopeLensInformation.ObjectiveMagnification, 3.45 / currentMicroscopeLensInformation.ObjectiveMagnification);
            }
            else pixelSize = size.PixelSize;

            var tmp = new Point(point.Value.X, point.Value.Y);
            var pointEnd = new Point(tmp.X * pixelSize.Width, tmp.Y * pixelSize.Height);
            stageViewModel.MoveRelativeStageXy(pointEnd);

            stageViewModel.GetDarkFieldStagePosition();
            stageViewModel.GetBrightFieldStagePosition();
        }).ConfigureAwait(false);
    }

    #endregion Command
}