using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Config;
using Core.Models.Models.Common.Pattern;
using Cuga.Data.DataStruct.Stage;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Net.Utilities.Helpers.Helpers.Structs;

namespace Core.Models.Models.Common.Cookies;

public sealed partial class ApplicationCookie : ObservableObject
{
    public static readonly Dictionary<Type, CalibrationViewModelEntry> CalibrationViewModelEntries = new();
    public static readonly string ApplicationCUGAVersion = (typeof(CgPoint).Assembly.GetName().Version ?? new System.Version()).ToString();

    /// <summary>
    /// 设备编码
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial string DeviceCode { get; set; } = string.Empty;

    /// <summary>
    /// 设备CUGA版本
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial string DeviceCUGAVersion { get; set; } = string.Empty;

    /// <summary>
    /// 程序名称
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// 用户
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial SysUserDTO SysUser { get; set; } = new();

    /// <summary>
    /// 标题
    /// </summary>
    public string Title => $"{ApplicationName} - {DeviceCode} V{DeviceCUGAVersion} [{SysUser.NickName}]";

    /// <summary>
    /// 当前权限菜单集合
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<SysMenuDTO> CurrentRoleSysMenus { get; set; } = [];

    /// <summary>
    /// 全部权限菜单集合
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<SysMenuDTO> AllRoleSysMenus { get; set; } = [];

    /// <summary>
    /// 校准菜单
    /// </summary>
    [ObservableProperty]
    public partial CalibrationMenu CalibrationMenu { get; set; } = new();

    /// <summary>
    /// 标题栏菜单
    /// </summary>
    [ObservableProperty]
    public partial SysMenuDTO TitleMenu { get; set; } = new();

    /// <summary>
    /// 系统管理菜单
    /// </summary>
    [ObservableProperty]
    public partial List<SystemManageMenu> SystemManageMenuList { get; set; } = [];

    /// <summary>
    /// 倍镜列表
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<MicroscopeLensInformation> MicroscopeLensInformations { get; set; } = [];

    /// <summary>
    /// 激光光强信息列表
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<LaserLightInformation> LaserLightInformations { get; set; } = [];

    /// <summary>
    /// 产率列表
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<ProductivityInformation> ProductivityInformations { get; set; } = [];

    /// <summary>
    /// 按照MagType分类的产率列表
    /// </summary>
    public IReadOnlyList<ProductivityInformation> OpticsMagTypeProductivityInformations =>
    [
        .. ProductivityInformations
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
        .. ProductivityInformations
            .Where(t => t.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.OI)
    ];

    /// <summary>
    /// OI按照MagType分类的产率列表
    /// </summary>
    public IReadOnlyList<ProductivityInformation> OIOpticsMagTypeProductivityInformations =>
    [
        .. OpticsMagTypeProductivityInformations
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
        .. ProductivityInformations
            .Where(t => t.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.NI)
    ];

    /// <summary>
    /// NI按照MagType分类的产率列表
    /// </summary>
    public IReadOnlyList<ProductivityInformation> NIOpticsMagTypeProductivityInformations =>
    [
        .. OpticsMagTypeProductivityInformations
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
        .. ProductivityInformations
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
    /// 光学采集偏振列表
    /// </summary>
    public IReadOnlyList<OpticsCollectorPolarizationModeEnum> OpticsCollectorPolarizationModeEnums => EnumHelper.Enums<OpticsCollectorPolarizationModeEnum>();

    /// <summary>
    /// CIB列表
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<CIBInformation> CIBInformations { get; set; } = [];

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
        .. CIBInformations
            .GroupBy(t => t.PMTId)
            .First()
            .Select(c => c.ChannelId)
    ];

    /// <summary>
    /// 硬件状态配置
    /// </summary>
    [ObservableProperty]
    public partial HardwareStateConfig? HardwareStateConfig { get; set; }

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