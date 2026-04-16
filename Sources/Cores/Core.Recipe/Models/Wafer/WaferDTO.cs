using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Models.Common.Alignment;
using Core.Recipe.Models.Wafer.WaferMap;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WaferMap.WPF.Documents;

namespace Core.Recipe.Models.Wafer;

public sealed partial class WaferDTO : ObservableObject, ICloneable<WaferDTO>, IAdaptIn<WaferDTO, WaferDTO>
{
    [ObservableProperty]
    private AlgorithmWaferTypeEnum _waferTypeEnum = AlgorithmWaferTypeEnum.D300;

    [ObservableProperty]
    private NotchDirectionTypeEnum _notchDirectionEnum = NotchDirectionTypeEnum.Down;

    /// <summary>
    /// Build Wafer对准结果，用于Review配方时计算WaferMap偏移量
    /// </summary>
    [ObservableProperty]
    private AlignmentResultDto? _alignmentResultDto;

    [ObservableProperty]
    private WaferMapDataDTO _waferMapDataDTO = new();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private WaferMapCanvasDocument _waferMapCanvasDocument = new();

    public bool IsAlignmentResultLegal()
    {
        return AlignmentResultDto != null
               && WaferMapDataDTO.WaferCircleCenter != Point.Origin;
    }

    /// <summary>
    /// WaferMapCanvasDocument未实现Clone方法，反序列化后，用WaferMapData映射到内存中
    /// </summary>
    public void WaferMapDataToWaferMapCanvasDocument()
    {
        WaferMapCanvasDocument.WaferBuilder.Circle = new Circle(WaferMapDataDTO.WaferCircleCenter, WaferMapDataDTO.WaferDiameter / 2d);
        WaferMapCanvasDocument.DieBuilder.OriginalDiePoint = WaferMapDataDTO.WaferOriginalDiePoint;
        WaferMapCanvasDocument.DieBuilder.DiePitchSize = new Size(WaferMapDataDTO.CellDieWidth, WaferMapDataDTO.CellDieHeight);
        WaferMapCanvasDocument.DieBuilder.DieScribeSize = new Size(WaferMapDataDTO.DieScribeWidth, WaferMapDataDTO.DieScribeHeight);

        WaferMapCanvasDocument.ReticleBuilder.OriginalDiePoint = WaferMapDataDTO.WaferReticleOriginalDiePoint;
        WaferMapCanvasDocument.ReticleBuilder.DiePitchSize = new Size(WaferMapDataDTO.ReticleWidth, WaferMapDataDTO.ReticleHeight);
        WaferMapCanvasDocument.ReticleBuilder.DieScribeSize = new Size(WaferMapDataDTO.ReticleScribeWidth, WaferMapDataDTO.ReticleScribeHeight);
        WaferMapCanvasDocument.ReticleBuilder.ReticleDieCount = WaferMapCanvasDocument.ReticleBuilder.ReticleDieCount with { XCount = WaferMapDataDTO.ReferenceDieRowNumber, YCount = WaferMapDataDTO.ReferenceDieColumnNumber };
    }

    public WaferDTO Clone() => new()
    {
        WaferTypeEnum = WaferTypeEnum,
        NotchDirectionEnum = NotchDirectionEnum,
        AlignmentResultDto = AlignmentResultDto is not null ? AlignmentResultDto!.Clone() : null,
        WaferMapDataDTO = WaferMapDataDTO.Clone()
        // todo: 实现document.clone()
    };

    public WaferDTO AdaptIn(WaferDTO obj)
    {
        WaferTypeEnum = obj.WaferTypeEnum;
        NotchDirectionEnum = obj.NotchDirectionEnum;
        AlignmentResultDto = obj.AlignmentResultDto is not null ? new AlignmentResultDto().AdaptIn(obj.AlignmentResultDto) : null;
        WaferMapDataDTO = new WaferMapDataDTO().AdaptIn(obj.WaferMapDataDTO);
        WaferMapCanvasDocument = obj.WaferMapCanvasDocument; //todo

        return this;
    }
}