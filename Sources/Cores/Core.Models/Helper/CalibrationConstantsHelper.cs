using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Net.Utilities.Helpers.Helpers.Files;

namespace Core.Models.Helper;

public static class CalibrationConstantsHelper
{
    /// <summary>
    /// 校准明场StageY轴长度
    /// </summary>
    public const double CalibrationGantryHLength = 1130000;

    /// <summary>
    /// 校准暗场采集PmtId集合
    /// </summary>
    public static readonly int[] PmtIds = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15];

    /// <summary>
    /// 主校准暗场采集PmtId
    /// </summary>
    public const int MainPmtId = 8;

    /// <summary>
    /// 校准暗场采集通道Id集合
    /// </summary>
    public static readonly int[] ChannelIds = [1, 2, 3];

    /// <summary>
    /// 主校准暗场采集通道Id
    /// </summary>
    public const int MainChannelId = 3;

    /// <summary>
    /// 主校准暗场采集宽度
    /// </summary>
    public const int MainXWidthPixel = 800;

    /// <summary>
    /// 主校准暗场采集倍率
    /// </summary>
    public const OpticsMagTypeEnum MainOpticsMagTypeEnum = OpticsMagTypeEnum.High;

    /// <summary>
    /// 主校准暗场采集速度
    /// </summary>
    public const StageSpeedEnum MainStageSpeedEnum = StageSpeedEnum.Low;

    /// <summary>
    /// 主校准暗场入射方式
    /// </summary>
    public const OpticsIlluminationModeEnum MainOpticsIlluminationModeEnum = OpticsIlluminationModeEnum.OI;

    /// <summary>
    /// 主校准暗场采集坐标系系统
    /// </summary>
    public const StageCoordinateSystemEnum MainStageCoordinateSystemEnum = StageCoordinateSystemEnum.Bright;

    /// <summary>
    /// 校准步长
    /// </summary>
    public static readonly double[] StageSteps = [1, 2, 5, 10, 20, 50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000, 50000];

    /// <summary>
    /// 校准步长
    /// </summary>
    public static readonly double[] StageThetaSteps = [-1, -0.5, -0.2, -0.1, -0.05, 0.05, 0.1, 0.2, 0.5, 1];

    /// <summary>
    /// 平移台监控间隔
    /// </summary>
    public const int MonitorStageMilliseconds = 1000;

    /// <summary>
    /// 显微镜监控间隔
    /// </summary>
    public const int MonitorMicroscopeMilliseconds = 1000;

    /// <summary>
    /// Fps监控间隔
    /// </summary>
    public const int FpsMonitorMilliseconds = 1000;

    /// <summary>
    /// 配方数据库注入的key
    /// </summary>
    public const string RecipeDbKey = "Recipe";

    #region 方法

    #region 规则

    /// <summary>
    /// 根据模板文件路径获取模板图片路径
    /// </summary>
    /// <param name="templateFilePath">模板文件</param>
    /// <returns>模板图片路径</returns>
    public static string TemplatePathToTemplateImagePath(string templateFilePath)
    {
        return $"{templateFilePath}.jpg";
    }

    /// <summary>
    /// 根据图片文件路径获取raw图片路径
    /// </summary>
    /// <param name="templateFilePath">模板文件</param>
    /// <returns>raw图片路径</returns>
    public static string ImagePathToRawImagePath(string templateFilePath)
    {
        return $"{FileHelper.GetFileFullName(templateFilePath)}.raw";
    }

    #endregion 规则

    #endregion 方法
}