using FreeSql.DataAnnotations;
using Local.SQL.DB.Providers.Models.Entities.Base;
using Local.SQL.DB.Providers.Models.Enums;
using Net.Utilities.Constants;
using Net.Utilities.Helper.Json;
using Newtonsoft.Json;

namespace Local.SQL.DB.Providers.Models.Entities;

/// <summary>
/// 系统菜单表
/// </summary>
[Table(Name = nameof(SysMenu))]
[Index($"idx_{nameof(SysMenu)}_{nameof(ParentId)}_{nameof(Name)}_{nameof(Perms)}", $"{nameof(ParentId)},{nameof(Name)},{nameof(Perms)}", true)]
public sealed class SysMenu : EntityBase
{
    /// <summary>
    /// 菜单名称
    /// </summary>
    [Column(IsNullable = false, StringLength = 64)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 父菜单Id
    /// </summary>
    [JsonConverter(typeof(JsonConverterUtil.LongJsonConverter))]
    public long ParentId { get; set; } = ConstantHelper.NegValue;

    /// <summary>
    /// 显示顺序
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// 菜单图标
    /// </summary>
    [Column(IsNullable = false, StringLength = 64)]
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// 类型
    /// </summary>
    public MenuTypeEnum MenuTypeEnum { get; set; }

    /// <summary>
    /// 组件路径
    /// </summary>
    [Column(IsNullable = false, StringLength = 1024)]
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// 权限字符串
    /// </summary>
    [Column(StringLength = 128)]
    public string? Perms { get; set; }

    /// <summary>
    /// 显示状态
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 显示是否为勾选框
    /// </summary>
    public bool IsCheckable { get; set; } = false;


    #region 导航属性

    /// <summary>
    /// 父菜单
    /// </summary>
    [Navigate(nameof(ParentId))]
    public SysMenu? Parent { get; set; }

    /// <summary>
    /// 子菜单列表
    /// </summary>
    [Navigate(nameof(ParentId))]
    public List<SysMenu> ChildList { get; set; } = [];

    /// <summary>
    /// 角色列表
    /// </summary>
    [Navigate(ManyToMany = typeof(SysRoleMenu))]
    public List<SysRole> SysRoleList { get; set; } = [];

    #endregion 导航属性
}