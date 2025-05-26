namespace Local.SQL.DB.Providers.Models.Enums;

public enum DataScopeEnum
{
    /// <summary>
    /// 全部数据权限
    /// </summary>
    All = 0,

    /// <summary>
    /// 本人数据
    /// </summary>
    Self = 1,

    /// <summary>
    /// 本部门数据权限
    /// </summary>
    Dept = 2,

    /// <summary>
    /// 本部门及以下数据权限
    /// </summary>
    DeptWithChild = 3,

    /// <summary>
    /// 自定数据权限
    /// </summary>
    Custom = 4
}