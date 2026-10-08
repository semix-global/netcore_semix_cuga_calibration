using Core.Models.Enums.Algorithm;

namespace Core.Models.Models.Setting;

/// <summary>
/// 模板匹配设置聚合。
/// 算法库由全局 Setting（<see cref="AlgorithmEngineTypeEnum"/>）决定，此处不冗余存储；
/// 公共部分（模板尺寸）平铺，各算法库专属参数下沉到各自子对象，互不污染、便于扩展新库。
/// </summary>
public sealed class TemplateMatchSetting
{
    /// <summary>模板尺寸（决定生成模板时的 ROI 大小），两算法库通用</summary>
    public AlgorithmTemplateSizeEnum TemplateSize { get; set; } = AlgorithmTemplateSizeEnum.Size256;

    /// <summary>Halcon 算法库专属参数</summary>
    public HalconTemplateMatchSetting Halcon { get; set; } = new();

    /// <summary>AlgoCSharp 算法库专属参数</summary>
    public AlgoCSharpTemplateMatchSetting AlgoCSharp { get; set; } = new();

    public TemplateMatchSetting Clone() => new()
    {
        TemplateSize = TemplateSize,
        Halcon = Halcon.Clone(),
        AlgoCSharp = AlgoCSharp.Clone()
    };
}

/// <summary>Halcon 算法库专属模板匹配参数。</summary>
public sealed class HalconTemplateMatchSetting
{
    /// <summary>Halcon 模板类型</summary>
    public AlgorithmTemplateTypeEnum TemplateType { get; set; } = AlgorithmTemplateTypeEnum.Ncc;

    public HalconTemplateMatchSetting Clone() => new() { TemplateType = TemplateType };
}

/// <summary>AlgoCSharp 算法库专属模板匹配参数。</summary>
public sealed class AlgoCSharpTemplateMatchSetting
{
    /// <summary>AlgoCSharp 匹配方法</summary>
    public TemplateMatchMethodEnum Method { get; set; } = TemplateMatchMethodEnum.TmCcoeffNormed;

    /// <summary>AlgoCSharp 金字塔层级</summary>
    public int PyramidLevels { get; set; }

    public AlgoCSharpTemplateMatchSetting Clone() => new()
    {
        Method = Method,
        PyramidLevels = PyramidLevels
    };
}