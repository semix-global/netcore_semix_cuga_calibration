using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;

namespace Core.Models.Models.Common.Alignment;

public partial class AlignmentCacheBase : ObservableCacheBase
{
    /// <summary>
    /// 算法匹配类型
    /// </summary>
    [ObservableProperty]
    public partial AlgorithmTemplateTypeEnum AlgorithmTemplateTypeEnum { get; set; } = AlgorithmTemplateTypeEnum.Ncc;

    /// <summary>
    /// 晶圆类型
    /// </summary>
    [ObservableProperty]
    public partial AlgorithmWaferTypeEnum AlgorithmWaferTypeEnum { get; set; } = AlgorithmWaferTypeEnum.D300;

    /// <summary>
    /// 对准低倍率
    /// </summary>
    [ObservableProperty]
    public partial MicroscopeLensInformation LowMag { get; set; } = MicroscopeLensInformation.Default;

    /// <summary>
    /// 对准低倍率模板尺寸
    /// </summary>
    [ObservableProperty]
    public partial AlgorithmTemplateSizeEnum LowSizeEnum { get; set; } = AlgorithmTemplateSizeEnum.Size256;

    /// <summary>
    /// 对准高倍率
    /// </summary>
    [ObservableProperty]
    public partial MicroscopeLensInformation HighMag { get; set; } = MicroscopeLensInformation.Default;

    /// <summary>
    /// 对准高倍率模板尺寸
    /// </summary>
    [ObservableProperty]
    public partial AlgorithmTemplateSizeEnum HighSizeEnum { get; set; } = AlgorithmTemplateSizeEnum.Size256;

    /// <summary>
    /// 低倍率mark点1位置(wafer中间掩模版芯粒左上角)
    /// </summary>
    [ObservableProperty]
    public partial AlignmentSiteDto LowSite1 { get; set; } = new();

    /// <summary>
    /// 低倍率mark点2位置(mark点1的相邻掩模版芯粒左上角)[没有模板, 用低倍率mark点1模板匹配]
    /// </summary>
    [ObservableProperty]
    public partial AlignmentSiteDto LowSite2 { get; set; } = new();

    /// <summary>
    /// 高倍率mark点1位置(低倍率mark点1的精细位置)
    /// </summary>
    [ObservableProperty]
    public partial AlignmentSiteDto HighSite1 { get; set; } = new();

    /// <summary>
    /// 高倍率mark点2位置(低倍率mark点2的精细位置)[没有模板, 用高倍率mark点1模板匹配]
    /// </summary>
    [ObservableProperty]
    public partial AlignmentSiteDto HighSite2 { get; set; } = new();

    /// <summary>
    /// 对准结果
    /// </summary>
    [ObservableProperty]
    public partial AlignmentResultDto Result { get; set; } = new();

    [ObservableProperty]
    public partial bool IsVerified { get; set; }

    [ObservableProperty]
    public partial bool IsOk { get; set; }
}