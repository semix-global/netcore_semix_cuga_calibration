using Cuga.Data.DataStruct.Stage;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Common.StageMap;

/// <summary>
/// Cuga ACS ErrorMap 的传输对象
/// </summary>
public sealed class StageMapErrorDTO : ICloneable<StageMapErrorDTO>, IAdaptTo<CgErrorMap>
{
    /// <summary>
    /// 区域编码
    /// </summary>
    public int? Zone { get; set; } = 0;

    /// <summary>
    /// 标准 X 坐标
    /// </summary>
    public double BaseX { get; set; }

    /// <summary>
    /// X 轴步进
    /// </summary>
    public double XStep { get; set; }

    /// <summary>
    /// 标准 Y 坐标
    /// </summary>
    public double BaseY { get; set; }

    /// <summary>
    /// Y 轴步进
    /// </summary>
    public double YStep { get; set; }

    /// <summary>
    /// 矩阵数据
    /// </summary>
    public StageMapErrorRowDTO[] Rows { get; set; } = [];

    public StageMapErrorDTO Clone() => new()
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
/// Cuga ACS ErrorMap 行的传输对象
/// </summary>
public sealed class StageMapErrorRowDTO : ICloneable<StageMapErrorRowDTO>, IAdaptTo<CgErrorMapRow>
{
    public int Id { get; set; }

    public StageMapErrorColumnDTO[] Cols { get; set; } = [];

    public StageMapErrorRowDTO Clone() => new()
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

public sealed class StageMapErrorColumnDTO : ICloneable<StageMapErrorColumnDTO>, IAdaptTo<CgErrorMapCol>
{
    public int Id { get; set; }

    public Vector Error { get; set; } = Vector.Zero;

    public StageMapErrorColumnDTO Clone() => new()
    {
        Id = Id,
        Error = Error
    };

    public CgErrorMapCol AdaptTo() => new()
    {
        Id = Id,
        Location = new CgPoint(Error.X, Error.Y)
    };
}