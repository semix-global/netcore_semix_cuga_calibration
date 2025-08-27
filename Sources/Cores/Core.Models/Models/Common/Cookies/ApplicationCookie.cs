using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Recipe;
using Local.SQL.DB.Providers.Models.Entities.DTO;

namespace Core.Models.Models.Common.Cookies;

public sealed partial class ApplicationCookie : ObservableObject
{
    /// <summary>
    /// 程序名称
    /// </summary>
    [ObservableProperty]
    private string _applicationName = string.Empty;

    /// <summary>
    /// 用户
    /// </summary>
    [ObservableProperty]
    private SysUserDto _sysUser = new();

    /// <summary>
    /// 权限菜单
    /// </summary>
    [ObservableProperty]
    private List<SysMenuDto> _roleSysMenuList = [];

    /// <summary>
    /// 全部权限菜单权限
    /// </summary>
    [ObservableProperty]
    private List<SysMenuDto> _allRoleSysMenuList = [];

    /// <summary>
    /// 校准菜单
    /// </summary>
    [ObservableProperty]
    private CalibrationMenu _calibrationMenu = new();

    /// <summary>
    /// 标题栏菜单
    /// </summary>
    [ObservableProperty]
    private SysMenuDto _titleMenu = new();

    /// <summary>
    /// 系统管理菜单
    /// </summary>
    [ObservableProperty]
    private List<SystemManageMenu> _systemManageMenuList = [];

    /// <summary>
    /// 倍镜列表
    /// </summary>
    [ObservableProperty]
    private List<MicroscopeLensInformation> _microscopeLensInformationList = [];

    /// <summary>
    /// 激光光强信息列表
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<LaserLightInformation> _laserLightInformationList = [];

    /// <summary>
    /// 校准当前应用配方
    /// </summary>
    [ObservableProperty]
    private CalibrationRecipeDto? _calibrationRecipeDto;

    /// <summary>
    /// 校准根据对准差值修正wafermap坐标后的配方
    /// </summary>
    [ObservableProperty]
    private CalibrationRecipeDto? _calibrationReviseRecipeDto;

    public string Title => $"{ApplicationName} [{SysUser.NickName}] {(CalibrationRecipeDto is not null ? $"[{CalibrationRecipeDto.CalibrationRecipeInfoDto.RecipeName}]" : string.Empty)}";
}