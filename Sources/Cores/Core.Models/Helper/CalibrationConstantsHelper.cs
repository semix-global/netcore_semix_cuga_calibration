namespace Core.Models.Helper;

public static class CalibrationConstantsHelper
{
    public const string Export = nameof(Export);

    public const string Import = nameof(Import);

    /// <summary>
    /// 主校准暗场采集PmtId
    /// </summary>
    public const int MainPmtId = 8;

    /// <summary>
    /// 配方数据库注入的key
    /// </summary>
    public const string RecipeDbKey = "Recipe";

    #region 规则

    /// <summary>
    /// 将模板文件路径转换为对应的模板图像文件路径
    /// </summary>
    /// <param name="templateFilePath">模板文件路径</param>
    /// <returns>追加 .bmp 扩展名后的模板图像文件路径</returns>
    public static string TemplatePathToTemplateImagePath(string templateFilePath) => $"{templateFilePath}.bmp";

    /// <summary>
    /// 将模板文件路径转换为对应的原始图像文件路径
    /// </summary>
    /// <param name="templateFilePath">模板文件路径</param>
    /// <returns>追加 _Origin.bmp 后缀后的原始图像文件路径</returns>
    public static string TemplatePathToOriginImagePath(string templateFilePath) => $"{templateFilePath}_Origin.bmp";

    /// <summary>
    /// 将模板文件路径转换为对应的模板匹配元数据文件路径
    /// </summary>
    /// <param name="templateFilePath">模板文件路径</param>
    /// <returns>追加 .json 扩展名后的模板匹配元数据文件路径</returns>
    public static string TemplatePathToTemplateMetaFilePath(string templateFilePath) => $"{templateFilePath}.json";

    #endregion 规则
}