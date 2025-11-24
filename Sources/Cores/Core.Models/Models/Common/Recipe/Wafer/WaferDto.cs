using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Recipe.Wafer.ReticleMask;
using Core.Models.Models.Common.Recipe.Wafer.WaferMap;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WaferMap.WPF.Documents;

namespace Core.Models.Models.Common.Recipe.Wafer;

public sealed partial class WaferDto : ObservableCacheBase, ICloneable<WaferDto>
{
    /// <summary>
    /// 晶圆中心晶圆坐标
    /// </summary>
    [ObservableProperty]
    private Point? _waferCenterWaferPosition;

    /// <summary>
    /// 减掉P8偏移量后的对准结果
    /// </summary>
    [ObservableProperty]
    private AlignmentResultDto? _alignmentResultDto;

    [ObservableProperty]
    private WaferMapDataDto _waferMapData = new();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private WaferMapCanvasDocument _waferMapCanvasDocument = new();

    [ObservableProperty]
    private ReticleMarkDto _reticleMarkDto = new();

    public bool RequireActionIsOk()
    {
        return AlignmentResultDto != null
               && WaferCenterWaferPosition != null;
    }

    public WaferDto Clone() => new()
    {
        WaferCenterWaferPosition = WaferCenterWaferPosition,
        AlignmentResultDto = AlignmentResultDto is not null ? AlignmentResultDto!.Clone() : null,
        ReticleMarkDto = ReticleMarkDto.Clone(),
        WaferMapData = WaferMapData.Clone()
        // todo: 实现document.clone()
    };

    public void WaferMapCanvasDocumentToWaferMapData()
    {
        WaferMapData.WaferDiameter = WaferMapCanvasDocument.WaferBuilder.Circle.Diameter;

        WaferMapData.WaferOriginalDiePoint = WaferMapCanvasDocument.DieBuilder.OriginalDiePoint;
        WaferMapData.CellDieWidth = WaferMapCanvasDocument.DieBuilder.DiePitchSize.Width;
        WaferMapData.CellDieHeight = WaferMapCanvasDocument.DieBuilder.DiePitchSize.Height;
        WaferMapData.DieScribeWidth = WaferMapCanvasDocument.DieBuilder.DieScribeSize.Width;
        WaferMapData.DieScribeHeight = WaferMapCanvasDocument.DieBuilder.DieScribeSize.Height;

        WaferMapData.WaferReticleOriginalDiePoint = WaferMapCanvasDocument.ReticleBuilder.OriginalDiePoint;
        WaferMapData.ReticleWidth = WaferMapCanvasDocument.ReticleBuilder.DiePitchSize.Width;
        WaferMapData.ReticleHeight = WaferMapCanvasDocument.ReticleBuilder.DiePitchSize.Height;
        WaferMapData.ReticleScribeWidth = WaferMapCanvasDocument.ReticleBuilder.DieScribeSize.Width;
        WaferMapData.ReticleScribeHeight = WaferMapCanvasDocument.ReticleBuilder.DieScribeSize.Height;
        WaferMapData.ReferenceDieRowNumber = WaferMapCanvasDocument.ReticleBuilder.ReticleDieCount.XCount;
        WaferMapData.ReferenceDieColumnNumber = WaferMapCanvasDocument.ReticleBuilder.ReticleDieCount.YCount;
    }

    public void WaferMapDataToWaferMapCanvasDocument()
    {
        WaferMapCanvasDocument.WaferBuilder.Circle = new Circle(Point.Origin, WaferMapData.WaferDiameter / 2d);
        WaferMapCanvasDocument.DieBuilder.OriginalDiePoint = WaferMapData.WaferOriginalDiePoint;
        WaferMapCanvasDocument.DieBuilder.DiePitchSize = new Size(WaferMapData.CellDieWidth, WaferMapData.CellDieHeight);
        WaferMapCanvasDocument.DieBuilder.DieScribeSize = new Size(WaferMapData.DieScribeWidth, WaferMapData.DieScribeHeight);

        WaferMapCanvasDocument.ReticleBuilder.OriginalDiePoint = WaferMapData.WaferReticleOriginalDiePoint;
        WaferMapCanvasDocument.ReticleBuilder.DiePitchSize = new Size(WaferMapData.ReticleWidth, WaferMapData.ReticleHeight);
        WaferMapCanvasDocument.ReticleBuilder.DieScribeSize = new Size(WaferMapData.ReticleScribeWidth, WaferMapData.ReticleScribeHeight);
        WaferMapCanvasDocument.ReticleBuilder.ReticleDieCount = WaferMapCanvasDocument.ReticleBuilder.ReticleDieCount with { XCount = WaferMapData.ReferenceDieRowNumber, YCount = WaferMapData.ReferenceDieColumnNumber };
    }
}