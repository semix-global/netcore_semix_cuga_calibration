using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;

namespace Core.Models.Models.Common.Alignment;

public sealed partial class AlignmentCacheDarkField : AlignmentCacheBase
{
    /// <summary>
    /// 对准高倍率
    /// </summary>
    [ObservableProperty]
    private OpticsMagTypeEnum _highDarkFieldOpticsMagTypeEnum = OpticsMagTypeEnum.High;

    /// <summary>
    /// 对准高倍率模板尺寸
    /// </summary>
    [ObservableProperty]
    private StageSpeedEnum _highDarkFieldStageSpeedEnum = StageSpeedEnum.Low;
}