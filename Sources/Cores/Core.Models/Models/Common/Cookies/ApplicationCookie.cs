using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Collector;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Recipe;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Net.Utilities.Helpers.Helpers.Structs;

namespace Core.Models.Models.Common.Cookies;

public sealed partial class ApplicationCookie : ObservableObject
{
    /// <summary>
    /// 程序名称
    /// </summary>
    [ObservableProperty]
    private string _applicationName = string.Empty;

    /// <summary>
    /// 设备编码
    /// </summary>
    [ObservableProperty]
    private string _deviceCode = string.Empty;

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
    private IReadOnlyList<MicroscopeLensInformation> _microscopeLensInformations = [];

    /// <summary>
    /// 激光光强信息列表
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<LaserLightInformation> _laserLightInformations = [];

    /// <summary>
    /// 产率列表
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<ProductivityInformation> _productivityInformations = [];

    /// <summary>
    /// 按照MagType分类的产率列表
    /// </summary>
    public IReadOnlyList<ProductivityInformation> OpticsMagTypeProductivityInformations =>
    [
        ..ProductivityInformations
            .GroupBy(t => t.OpticsIlluminationModeEnum)
            .SelectMany(g => g
                .GroupBy(t => t.OpticsMagType)
                .Select(gg => gg.OrderByDescending(t => t).First()))
            .OrderBy(t => t)
    ];

    /// <summary>
    /// OI产率列表
    /// </summary>
    public IReadOnlyList<ProductivityInformation> OIProductivityInformations =>
    [
        ..ProductivityInformations
            .Where(t => t.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.OI)
    ];

    /// <summary>
    /// OI按照MagType分类的产率列表
    /// </summary>
    public IReadOnlyList<ProductivityInformation> OIOpticsMagTypeProductivityInformations =>
    [
        ..OpticsMagTypeProductivityInformations
            .Where(t => t.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.OI)
    ];

    /// <summary>
    /// OI最低产率
    /// </summary>
    public ProductivityInformation OILowProductivityInformation => OIProductivityInformations
        .OrderByDescending(t => t)
        .FirstOrDefault() ?? ProductivityInformation.Default;

    /// <summary>
    /// OI最高产率
    /// </summary>
    public ProductivityInformation OIHighProductivityInformation => OIProductivityInformations
        .OrderBy(t => t)
        .FirstOrDefault() ?? ProductivityInformation.Default;

    /// <summary>
    /// NI产率列表
    /// </summary>
    public IReadOnlyList<ProductivityInformation> NIProductivityInformations =>
    [
        ..ProductivityInformations
            .Where(t => t.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.NI)
    ];

    /// <summary>
    /// NI按照MagType分类的产率列表
    /// </summary>
    public IReadOnlyList<ProductivityInformation> NIOpticsMagTypeProductivityInformations =>
    [
        ..OpticsMagTypeProductivityInformations
            .Where(t => t.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.NI)
    ];

    /// <summary>
    /// NI最低产率
    /// </summary>
    public ProductivityInformation NILowProductivityInformation => NIProductivityInformations
        .OrderByDescending(t => t)
        .FirstOrDefault() ?? ProductivityInformation.Default;

    /// <summary>
    /// NI最高产率
    /// </summary>
    public ProductivityInformation NIHighProductivityInformation => NIProductivityInformations
        .OrderBy(t => t)
        .FirstOrDefault() ?? ProductivityInformation.Default;

    /// <summary>
    /// 照明方式列表
    /// </summary>
    public IReadOnlyList<OpticsIlluminationModeEnum> OpticsIlluminationModeEnums =>
    [
        ..ProductivityInformations
            .GroupBy(t => t.OpticsIlluminationModeEnum)
            .Select(t => t.Key)
    ];

    /// <summary>
    /// 光学切趾列表
    /// </summary>
    public IReadOnlyList<OpticsApodizationModeEnum> OpticsApodizationModeEnums => [OpticsApodizationModeEnum.None];

    /// <summary>
    /// 光学偏振列表
    /// </summary>
    public IReadOnlyList<OpticsPolarizationModeEnum> OpticsPolarizationModeEnums => EnumHelper.Enums<OpticsPolarizationModeEnum>();

    /// <summary>
    /// 采集偏振列表
    /// </summary>
    public IReadOnlyList<CollectorPolarizationModeEnum> CollectorPolarizationModeEnums => EnumHelper.Enums<CollectorPolarizationModeEnum>();

    /// <summary>
    /// CIB列表
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<CIBInformation> _cIBInformations = [];

    /// <summary>
    /// CIB的PMT列表
    /// </summary>
    public IReadOnlyList<int> CIBInformationPMTIds =>
    [
        .. CIBInformations
            .GroupBy(t => t.PMTId)
            .Select(t => t.Key)
    ];

    /// <summary>
    /// CIB的通道列表
    /// </summary>
    public IReadOnlyList<int> CIBInformationChannelIds =>
    [
        ..CIBInformations
            .GroupBy(t => t.PMTId)
            .First()
            .Select(c => c.ChannelId)
    ];

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

    public IReadOnlyList<ProductivityInformation> GetProductivityInformations(OpticsIlluminationModeEnum opticsIlluminationModeEnum) => opticsIlluminationModeEnum switch
    {
        OpticsIlluminationModeEnum.OI => OIProductivityInformations,
        OpticsIlluminationModeEnum.NI => NIProductivityInformations,
        _ => ThrowHelper.ThrowNotSupportedException<IReadOnlyList<ProductivityInformation>>(nameof(opticsIlluminationModeEnum))
    };

    public IReadOnlyList<ProductivityInformation> GetOpticsMagTypeProductivityInformations(OpticsIlluminationModeEnum opticsIlluminationModeEnum) => opticsIlluminationModeEnum switch
    {
        OpticsIlluminationModeEnum.OI => OIOpticsMagTypeProductivityInformations,
        OpticsIlluminationModeEnum.NI => NIOpticsMagTypeProductivityInformations,
        _ => ThrowHelper.ThrowNotSupportedException<IReadOnlyList<ProductivityInformation>>(nameof(opticsIlluminationModeEnum))
    };
}