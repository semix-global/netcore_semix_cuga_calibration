using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;

namespace Local.SQL.DB.Providers.Models.Entities.DTO;

public sealed partial class SysPostDto : SysBaseDto, ICloneable<SysPostDto>, IAdaptTo<SysPost>, IAdaptIn<SysPost, SysPostDto>
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private int _orderNum;

    [ObservableProperty]
    private bool _isEnabled;

    #region 导航属性

    [ObservableProperty]
    private List<SysUserDto> _sysUserList = [];

    #endregion 导航属性

    #region Mapper

    // 自定义的深度克隆方法
    public SysPostDto Clone()
    {
        return new SysPostDto
        {
            Name = Name,
            Code = Code,
            OrderNum = OrderNum,
            IsEnabled = IsEnabled,
            SysUserList = [.. SysUserList.Select(t => t.Clone())],
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

    public SysPost AdaptTo() => new()
    {
        Name = Name,
        Code = Code,
        OrderNum = OrderNum,
        IsEnabled = IsEnabled,
        SysUserList = [.. SysUserList.Select(t => t.AdaptTo())],
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

    public SysPostDto AdaptIn(SysPost obj)
    {
        Guard.IsNotNull(obj, nameof(obj));

        Name = obj.Name;
        Code = obj.Code;
        OrderNum = obj.OrderNum;
        IsEnabled = obj.IsEnabled;
        SysUserList = [.. obj.SysUserList.Select(t => new SysUserDto().AdaptIn(t))];
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