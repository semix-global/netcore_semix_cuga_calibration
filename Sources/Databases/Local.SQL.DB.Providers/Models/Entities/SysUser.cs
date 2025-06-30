using FreeSql.DataAnnotations;
using Local.SQL.DB.Providers.Models.Entities.Base;
using Local.SQL.DB.Providers.Models.Enums;
using Net.Utilities.Helpers;
using Newtonsoft.Json;

namespace Local.SQL.DB.Providers.Models.Entities;

/// <summary>
/// 系统用户表
/// </summary>
[Table(Name = nameof(SysUser))]
[Index($"idx_{nameof(SysUser)}_{nameof(UserName)}", nameof(UserName), true)]
public sealed class SysUser : EntityBase
{
    /// <summary>
    /// 登录用户名
    /// </summary>
    [Column(IsNullable = false, StringLength = 64)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 部门Id
    /// </summary>
    [JsonConverter(typeof(JsonConverterUtils.LongJsonConverter))]
    public long DeptId { get; set; }

    /// <summary>
    /// 用户昵称
    /// </summary>
    [Column(IsNullable = false, StringLength = 64)]
    public string NickName { get; set; } = string.Empty;

    /// <summary>
    /// 头像
    /// </summary>
    [Column(IsNullable = false, StringLength = 12400)]
    public string Avatar { get; set; } = string.Empty;

    /// <summary>
    /// 用户邮箱
    /// </summary>
    [Column(IsNullable = false, StringLength = 64)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Column(IsNullable = false, StringLength = 200)]
    [JsonIgnore]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 用户性别
    /// </summary>
    public SexEnum SexEnum { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    [Column(CanInsert = false, ServerTime = DateTimeKind.Local)]
    [JsonConverter(typeof(JsonConverterUtils.DateTimeJsonConverter))]
    public DateTime? LoginDate { get; set; }

    #region 导航属性

    /// <summary>
    /// 部门
    /// </summary>
    [Navigate(nameof(DeptId))]
    public SysDept SysDept { get; set; } = null!;

    /// <summary>
    /// 岗位列表
    /// </summary>
    [Navigate(ManyToMany = typeof(SysUserPost))]
    public List<SysPost> SysPostList { get; set; } = [];

    /// <summary>
    /// 角色列表
    /// </summary>
    [Navigate(ManyToMany = typeof(SysUserRole))]
    public List<SysRole> SysRoleList { get; set; } = [];

    #endregion 导航属性
}