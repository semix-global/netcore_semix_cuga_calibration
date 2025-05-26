using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Recipe.Wafer.ReticleMask;
using Core.Models.Models.Common.Recipe.Wafer.WaferMap;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;

namespace Core.Models.Models.Common.Recipe.Wafer;

public sealed partial class WaferDto : ObservableCacheBase, ICloneable<WaferDto>
{
    /// <summary>
    /// 晶圆中心实际明场坐标值
    /// </summary>
    [ObservableProperty]
    private Point? _waferCenterBrightFieldPosition;

    /// <summary>
    /// 减掉P8偏移量后的对准结果
    /// </summary>
    [ObservableProperty]
    private AlignmentResultDto? _alignmentResultDto;

    [ObservableProperty]
    private WaferMapDto _waferMapDto = new();

    [ObservableProperty]
    private ReticleMarkDto _reticleMarkDto = new();

    public bool RequireActionIsOk()
    {
        return AlignmentResultDto != null
               && WaferCenterBrightFieldPosition != null;
    }

    public WaferDto Clone() => new()
    {
        WaferCenterBrightFieldPosition = WaferCenterBrightFieldPosition,
        AlignmentResultDto = AlignmentResultDto is not null ? AlignmentResultDto!.Clone() : null,
        WaferMapDto = WaferMapDto.Clone(),
        ReticleMarkDto = ReticleMarkDto.Clone(),
    };
}