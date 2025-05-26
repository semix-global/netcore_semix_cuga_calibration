using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;

namespace Local.SQL.DB.Providers.Models.Entities.DTO;

public partial class SysLoginInformationDto : ObservableObject, ICloneable<SysLoginInformationDto>, IAdaptTo<SysLoginInformation>, IAdaptIn<SysLoginInformation, SysLoginInformationDto>
{
    [ObservableProperty]
    private long _id;

    [ObservableProperty]
    private long _loginUserId;

    [ObservableProperty]
    private string _loginUserName = string.Empty;

    [ObservableProperty]
    private DateTime _loginTime;

    [ObservableProperty]
    private bool _isLoginOk;

    [ObservableProperty]
    private string? _message;

    #region Mapper

    // 自定义的深度克隆方法
    public SysLoginInformationDto Clone()
    {
        return new SysLoginInformationDto
        {
            Id = Id,
            LoginUserId = LoginUserId,
            LoginUserName = LoginUserName,
            LoginTime = LoginTime,
            IsLoginOk = IsLoginOk,
            Message = Message
        };
    }

    public SysLoginInformation AdaptTo() => new()
    {
        Id = Id,
        LoginUserId = LoginUserId,
        LoginUserName = LoginUserName,
        LoginTime = LoginTime,
        IsLoginOk = IsLoginOk,
        Message = Message
    };

    public SysLoginInformationDto AdaptIn(SysLoginInformation obj)
    {
        Guard.IsNotNull(obj, nameof(obj));

        Id = obj.Id;
        LoginUserId = obj.LoginUserId;
        LoginUserName = obj.LoginUserName;
        LoginTime = obj.LoginTime;
        IsLoginOk = obj.IsLoginOk;
        Message = obj.Message;

        return this;
    }

    #endregion Mapper
}