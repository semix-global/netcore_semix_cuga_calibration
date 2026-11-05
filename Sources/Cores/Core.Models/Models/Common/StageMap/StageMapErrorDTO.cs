using Cuga.Data.DataStruct.Stage;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Common.StageMap;

/// <summary>
/// Cuga ACS ErrorMap 的传输对象.
/// </summary>
/// <param name="Zone">区域编码.</param>
/// <param name="BaseX">标准 X 坐标.</param>
/// <param name="BaseY">标准 Y 坐标.</param>
/// <param name="XStep">X 轴步进.</param>
/// <param name="YStep">Y 轴步进.</param>
/// <param name="Rows">矩阵数据.</param>
public sealed record StageMapErrorDTO(
    int? Zone,
    double BaseX,
    double BaseY,
    double XStep,
    double YStep,
    StageMapErrorRowDTO[] Rows) : IAdaptTo<CgErrorMap>
{
    public CgErrorMap AdaptTo() => new()
    {
        Zone = Zone,
        BaseX = BaseX,
        BaseY = BaseY,
        XStep = XStep,
        YStep = YStep,
        Rows = [.. Rows.Select(row => row.AdaptTo())]
    };
}

/// <summary>
/// Cuga ACS ErrorMap 行的传输对象.
/// </summary>
/// <param name="Id">行编号.</param>
/// <param name="Cols">列数据.</param>
public sealed record StageMapErrorRowDTO(int Id, StageMapErrorColumnDTO[] Cols) : IAdaptTo<CgErrorMapRow>
{
    public CgErrorMapRow AdaptTo() => new()
    {
        Id = Id,
        Cols = [.. Cols.Select(column => column.AdaptTo())]
    };
}

/// <summary>
/// Cuga ACS ErrorMap 列的传输对象.
/// </summary>
/// <param name="Id">列编号.</param>
/// <param name="Error">误差向量.</param>
public sealed record StageMapErrorColumnDTO(int Id, Vector Error) : IAdaptTo<CgErrorMapCol>
{
    public CgErrorMapCol AdaptTo() => new()
    {
        Id = Id,
        Location = new CgPoint(Error.X, Error.Y)
    };
}