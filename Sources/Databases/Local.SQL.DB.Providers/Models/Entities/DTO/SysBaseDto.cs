using CommunityToolkit.Mvvm.ComponentModel;

namespace Local.SQL.DB.Providers.Models.Entities.DTO;

public partial class SysBaseDto : ObservableObject
{
    [ObservableProperty]
    private long _id;

    [ObservableProperty]
    private long _createdUserId;

    [ObservableProperty]
    private string _createdUserName = string.Empty;

    [ObservableProperty]
    private DateTime _createdTime;

    [ObservableProperty]
    private long? _modifiedUserId;

    [ObservableProperty]
    private string? _modifiedUserName;

    [ObservableProperty]
    private DateTime? _modifiedTime;

    [ObservableProperty]
    private string? _remark;

    [ObservableProperty]
    private bool _isDeleted;
}