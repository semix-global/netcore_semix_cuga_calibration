using Microsoft.Extensions.Logging;
using Net.Utilities.Calibration;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using System.IO;

namespace Core.Algorithm.Helper;

/// <summary>
/// 算法错误日志辅助类：统一提供错误图片保存与错误日志记录
/// </summary>
public static class AlgorithmLogHelper
{
    /// <summary>
    /// 错误图片目录
    /// </summary>
    public static readonly string ErrorImageDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ErrorImages");

    /// <summary>
    /// 保存错误图片到错误图片目录
    /// </summary>
    /// <param name="image">错误图片</param>
    /// <param name="guid">校准流程 Guid</param>
    /// <param name="methodName">算法方法名</param>
    /// <returns>错误图片完整路径</returns>
    public static string SaveErrorImage(BitmapImage image, Guid guid, string methodName)
    {
        var errorImagePath = Path.Combine(ErrorImageDirectory, $"{methodName}_{guid}.jpg");
        DirectoryHelper.CreateFileDirectoryIfNotExists(errorImagePath);
        image.SaveImage(errorImagePath);
        return errorImagePath;
    }

    /// <summary>
    /// 记录算法错误日志（可附带错误图片）
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="ex">异常</param>
    /// <param name="methodName">算法方法名</param>
    /// <param name="guid">校准流程 Guid</param>
    /// <param name="errorImagePath">错误图片路径（为空时仅记录错误消息）</param>
    public static void LogAlgorithmError(ILogger logger, Exception ex, string methodName, Guid guid, string? errorImagePath = null)
    {
        if (string.IsNullOrEmpty(errorImagePath))
        {
            logger.LogHtmlError(ex, $"{methodName} Error", HtmlHeaderLevelEnum.Header6,
                new HtmlBullet(new
                {
                    ErrorMessage = ex.Message
                }), guid.LoggingHtml());
        }
        else
        {
            logger.LogHtmlError(ex, $"{methodName} Error", HtmlHeaderLevelEnum.Header6,
                new HtmlBullet(new
                {
                    ErrorImage = new HtmlImage(errorImagePath),
                    ImagePath = errorImagePath,
                    ErrorMessage = ex.Message
                }), guid.LoggingHtml());
        }
    }
}
