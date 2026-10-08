namespace Core.Models.Enums.Algorithm;

/// <summary>
/// AlgoCSharp 模板匹配方法
/// </summary>
public enum TemplateMatchMethodEnum
{
    /// <summary>平方差，越小越好</summary>
    TmSqdiff = 0,

    /// <summary>归一化平方差，越小越好</summary>
    TmSqdiffNormed = 1,

    /// <summary>相关，越大越好</summary>
    TmCcorr = 2,

    /// <summary>归一化相关，越大越好</summary>
    TmCcorrNormed = 3,

    /// <summary>相关系数，越大越好</summary>
    TmCcoeff = 4,

    /// <summary>归一化相关系数，越大越好</summary>
    TmCcoeffNormed = 5
}