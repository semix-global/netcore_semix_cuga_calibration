using FreeSql.DataAnnotations;
using Local.SQL.DB.Providers.Models.Entities.Base;

namespace Local.SQL.DB.Providers.Models.Entities;

/// <summary>
/// 系统岗位表
/// </summary>
[Table(Name = nameof(SysPost))]
[Index($"idx_{nameof(SysPost)}_{nameof(Name)}", nameof(Name), true)]
[Index($"idx_{nameof(SysPost)}_{nameof(Code)}", nameof(Code), true)]
public sealed class SysPost : EntityBase
{
    /// <summary>
    /// 岗位名称
    /// </summary>
    [Column(IsNullable = false, StringLength = 64)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 岗位编码
    /// </summary>
    [Column(IsNullable = false, StringLength = 64)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 显示顺序
    /// </summary>
    public int OrderNum { get; set; }

    #region 导航属性

    /// <summary>
    /// 用户列表
    /// </summary>
    [Navigate(ManyToMany = typeof(SysUserPost))]
    public List<SysUser> SysUserList { get; set; } = [];

    #endregion 导航属性
}