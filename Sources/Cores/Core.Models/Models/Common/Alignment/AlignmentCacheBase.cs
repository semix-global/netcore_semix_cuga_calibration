using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Microscope;
using Local.NoSQL.DB.Providers.Bases;

namespace Core.Models.Models.Common.Alignment;

public partial class AlignmentCacheBase : ObservableCacheBase
{
    /// <summary>
    /// 算法匹配类型
    /// </summary>
    [ObservableProperty]
    private AlgorithmTemplateTypeEnum _algorithmTemplateTypeEnum;

    /// <summary>
    /// 晶圆类型
    /// </summary>
    [ObservableProperty]
    private AlgorithmWaferTypeEnum _algorithmWaferTypeEnum = AlgorithmWaferTypeEnum.D300;

    /// <summary>
    /// 对准低倍率
    /// </summary>
    [ObservableProperty]
    private MicroscopeMagnificationEnum _lowMag = MicroscopeMagnificationEnum.Magnification5X;

    /// <summary>
    /// 对准低倍率模板尺寸
    /// </summary>
    [ObservableProperty]
    private AlgorithmTemplateSizeEnum _lowSizeEnum = AlgorithmTemplateSizeEnum.Size256;

    /// <summary>
    /// 对准高倍率
    /// </summary>
    [ObservableProperty]
    private MicroscopeMagnificationEnum _highMag = MicroscopeMagnificationEnum.Magnification50X;

    /// <summary>
    /// 对准高倍率模板尺寸
    /// </summary>
    [ObservableProperty]
    private AlgorithmTemplateSizeEnum _highSizeEnum = AlgorithmTemplateSizeEnum.Size256;

    /// <summary>
    /// 低倍率mark点1位置(wafer中间掩模版芯粒左上角)
    /// </summary>
    [ObservableProperty]
    private AlignmentSiteDto _lowSite1 = new();

    /// <summary>
    /// 低倍率mark点2位置(mark点1的相邻掩模版芯粒左上角)[没有模板, 用低倍率mark点1模板匹配]
    /// </summary>
    [ObservableProperty]
    private AlignmentSiteDto _lowSite2 = new();

    /// <summary>
    /// 高倍率mark点1位置(低倍率mark点1的精细位置)
    /// </summary>
    [ObservableProperty]
    private AlignmentSiteDto _highSite1 = new();

    /// <summary>
    /// 高倍率mark点2位置(低倍率mark点2的精细位置)[没有模板, 用高倍率mark点1模板匹配]
    /// </summary>
    [ObservableProperty]
    private AlignmentSiteDto _highSite2 = new();

    /// <summary>
    /// 对准结果
    /// </summary>
    [ObservableProperty]
    private AlignmentResultDto _result = new();

    [ObservableProperty]
    private bool _isVerified;

    [ObservableProperty]
    private bool _isOk;
}