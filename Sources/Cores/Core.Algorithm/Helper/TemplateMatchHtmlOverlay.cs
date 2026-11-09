using Core.Algorithm.Models.TemplateMatch;
using Core.Models.Helper;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using System.IO;

namespace Core.Algorithm.Helper;

/// <summary>
/// 模板日志 HTML 叠加（模板图十字画自定义模板中心；匹配结果图画 MarkPoint 十字与模板尺寸 ROI）
/// </summary>
public static class TemplateMatchHtmlOverlay
{
    /// <summary>
    /// 模板图十字：几何中心加 CenterOffset
    /// </summary>
    /// <param name="templateImageSize">模板图实际像素尺寸</param>
    /// <param name="centerOffset">自定义模板中心相对 ROI 中心的偏移</param>
    public static HtmlImageCrossOverlay CreateTemplateCenterCross(Size templateImageSize, Point centerOffset) =>
        new((Point)(templateImageSize / 2d) + (Vector)centerOffset);

    /// <summary>
    /// 模板图十字（从模板持久化产物取尺寸与中心偏移，不依赖生成窗口会话状态）
    /// </summary>
    /// <param name="templateFilePath">模板文件路径（基路径，无扩展名）</param>
    public static HtmlImageCrossOverlay CreateTemplateCenterCross(string templateFilePath)
    {
        var imagePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath);
        if (File.Exists(imagePath) == false) return new HtmlImageCrossOverlay(true);

        var (_, templateImageSize) = ImageHelper.GetImageInfo(imagePath);

        return CreateTemplateCenterCross(templateImageSize, TemplateMatchMeta.Load(templateFilePath).CenterOffset);
    }

    /// <summary>
    /// 匹配结果图：MarkPoint 十字加模板尺寸 ROI（矩形中心 = MarkPoint - CenterOffset）
    /// </summary>
    /// <param name="markPoint">匹配结果点（模板中心落在结果图上的位置）</param>
    /// <param name="templateImageSize">模板图实际像素尺寸</param>
    /// <param name="centerOffset">自定义模板中心相对 ROI 中心的偏移</param>
    public static HtmlImageOverlay[] CreateMatchResultOverlays(Point markPoint, Size templateImageSize, Point centerOffset)
    {
        var templateRoiCenter = markPoint - (Vector)centerOffset;

        return
        [
            new HtmlImageCrossOverlay(markPoint, templateImageSize),
            new HtmlImageRectangleOverlay(templateRoiCenter, templateImageSize)
        ];
    }
}
