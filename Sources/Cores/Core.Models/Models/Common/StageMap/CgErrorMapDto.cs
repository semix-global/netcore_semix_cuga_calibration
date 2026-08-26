using Cuga.Data.DataStruct.Stage;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.StageMap;

/// <summary>
/// Cuga ACS ErrorMap 的传输对象。
/// </summary>
public sealed class CgErrorMapDto : ICloneable<CgErrorMapDto>, IAdaptTo<CgErrorMap>
{
    /// <summary>
    /// 区域编码。
    /// </summary>
    public int? Zone { get; set; } = 0;

    /// <summary>
    /// 标准 X 坐标。
    /// </summary>
    public double BaseX { get; set; }

    /// <summary>
    /// X 轴步进。
    /// </summary>
    public double XStep { get; set; }

    /// <summary>
    /// 标准 Y 坐标。
    /// </summary>
    public double BaseY { get; set; }

    /// <summary>
    /// Y 轴步进。
    /// </summary>
    public double YStep { get; set; }

    /// <summary>
    /// 矩阵数据。
    /// </summary>
    public List<CgErrorMapRowDto> Rows { get; set; } = [];

    public CgErrorMapDto Clone() => new()
    {
        Zone = Zone,
        BaseX = BaseX,
        XStep = XStep,
        BaseY = BaseY,
        YStep = YStep,
        Rows = [.. Rows.Select(t => t.Clone())]
    };

    public CgErrorMap AdaptTo() => new()
    {
        Zone = Zone,
        BaseX = BaseX,
        XStep = XStep,
        BaseY = BaseY,
        YStep = YStep,
        Rows = [.. Rows.Select(t => t.AdaptTo())]
    };
}

/// <summary>
/// Cuga ACS ErrorMap 行的传输对象。
/// </summary>
public sealed class CgErrorMapRowDto : ICloneable<CgErrorMapRowDto>, IAdaptTo<CgErrorMapRow>
{
    public int Id { get; set; }

    public List<CgErrorMapColDto> Cols { get; set; } = [];

    public CgErrorMapRowDto Clone() => new()
    {
        Id = Id,
        Cols = [.. Cols.Select(t => t.Clone())]
    };

    public CgErrorMapRow AdaptTo() => new()
    {
        Id = Id,
        Cols = [.. Cols.Select(t => t.AdaptTo())]
    };
}

/// <summary>
/// Cuga ACS ErrorMap 列的传输对象。
/// </summary>
public sealed class CgErrorMapColDto : ICloneable<CgErrorMapColDto>, IAdaptTo<CgErrorMapCol>
{
    public int Id { get; set; }

    public CgPoint Location { get; set; } = CgPoint.Empty;

    public CgErrorMapColDto Clone() => new()
    {
        Id = Id,
        Location = Location.Clone()
    };

    public CgErrorMapCol AdaptTo() => new()
    {
        Id = Id,
        Location = Location.Clone()
    };
}