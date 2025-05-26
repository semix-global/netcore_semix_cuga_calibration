using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Local.SQL.DB.Providers.Models.Enums;
using Net.Utilities.Mapper.Interfaces;

namespace Local.SQL.DB.Providers.Models.Entities.DTO;

public sealed partial class SysUserDto : SysBaseDto, ICloneable<SysUserDto>, IAdaptTo<SysUser>, IAdaptIn<SysUser, SysUserDto>
{
    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private long _deptId;

    [ObservableProperty]
    private string _nickName = string.Empty;

    [ObservableProperty]
    private string _avatar = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private SexEnum? _sexEnum;

    [ObservableProperty]
    private DateTime? _loginDate = DateTime.Now;

    [ObservableProperty]
    private bool? _isEnabled;

    public bool IsAdmin => Id == 1 || SysRoleList.Any(t => t.IsAdmin);

    #region 导航属性

    [ObservableProperty]
    private SysDeptDto _sysDept = new();

    [ObservableProperty]
    private List<SysPostDto> _sysPostList = [];

    [ObservableProperty]
    private List<SysRoleDto> _sysRoleList = [];

    #endregion 导航属性

    #region Mapper

    // 自定义的深度克隆方法
    public SysUserDto Clone()
    {
        return new SysUserDto
        {
            UserName = UserName,
            DeptId = DeptId,
            NickName = NickName,
            Avatar = Avatar,
            Email = Email,
            Password = Password,
            SexEnum = SexEnum,
            LoginDate = LoginDate,
            IsEnabled = IsEnabled,
            SysDept = SysDept.Clone(),
            SysPostList = [.. SysPostList.Select(t => t.Clone())],
            SysRoleList = [.. SysRoleList.Select(t => t.Clone())],
            Id = Id,
            CreatedUserId = CreatedUserId,
            CreatedUserName = CreatedUserName,
            CreatedTime = CreatedTime,
            ModifiedUserId = ModifiedUserId,
            ModifiedUserName = ModifiedUserName,
            ModifiedTime = ModifiedTime,
            Remark = Remark,
            IsDeleted = IsDeleted
        };
    }

    public void Update(SysUserDto obj)
    {
        Guard.IsNotNull(obj, nameof(obj));

        UserName = obj.UserName;
        DeptId = obj.DeptId;
        NickName = obj.NickName;
        Avatar = obj.Avatar;
        Email = obj.Email;
        Password = obj.Password;
        SexEnum = obj.SexEnum;
        LoginDate = obj.LoginDate;
        SysPostList = [.. obj.SysPostList.Select(t => t.Clone())];
        SysRoleList = [.. obj.SysRoleList.Select(t => t.Clone())];
        Id = obj.Id;
        CreatedUserId = obj.CreatedUserId;
        CreatedUserName = obj.CreatedUserName;
        CreatedTime = obj.CreatedTime;
        ModifiedUserId = obj.ModifiedUserId;
        ModifiedUserName = obj.ModifiedUserName;
        ModifiedTime = obj.ModifiedTime;
        Remark = obj.Remark;
        IsDeleted = obj.IsDeleted;
    }

    public SysUser AdaptTo() => new()
    {
        UserName = UserName,
        DeptId = DeptId,
        NickName = NickName,
        Avatar = Avatar,
        Email = Email,
        Password = Password,
        SexEnum = SexEnum is not null ? (SexEnum)SexEnum : Enums.SexEnum.Unknown,
        LoginDate = LoginDate,
        SysDept = SysDept.AdaptTo(),
        SysPostList = [.. SysPostList.Select(t => t.AdaptTo())],
        SysRoleList = [.. SysRoleList.Select(t => t.AdaptTo())],
        Id = Id,
        CreatedUserId = CreatedUserId,
        CreatedUserName = CreatedUserName,
        CreatedTime = CreatedTime,
        ModifiedUserId = ModifiedUserId,
        ModifiedUserName = ModifiedUserName,
        ModifiedTime = ModifiedTime,
        Remark = Remark,
        IsDeleted = IsDeleted
    };

    public SysUserDto AdaptIn(SysUser obj)
    {
        Guard.IsNotNull(obj, nameof(obj));

        UserName = obj.UserName;
        DeptId = obj.DeptId;
        NickName = obj.NickName;
        Avatar = obj.Avatar;
        Email = obj.Email;
        Password = obj.Password;
        SexEnum = obj.SexEnum;
        LoginDate = obj.LoginDate;
        SysPostList = [.. obj.SysPostList.Select(t => new SysPostDto().AdaptIn(t))];
        SysRoleList = [.. obj.SysRoleList.Select(t => new SysRoleDto().AdaptIn(t))];
        Id = obj.Id;
        CreatedUserId = obj.CreatedUserId;
        CreatedUserName = obj.CreatedUserName;
        CreatedTime = obj.CreatedTime;
        ModifiedUserId = obj.ModifiedUserId;
        ModifiedUserName = obj.ModifiedUserName;
        ModifiedTime = obj.ModifiedTime;
        Remark = obj.Remark;
        IsDeleted = obj.IsDeleted;

        return this;
    }

    #endregion Mapper
}