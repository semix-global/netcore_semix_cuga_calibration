using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Cookies;
using Core.Utilities.WPF.Assembly.Model;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Net.Utilities.Mapper.Interfaces;
using System.Collections;

namespace Core.Models.Models.Setting.CalibrationRelationConfig;

public sealed partial class SettingCalibrationRelationConfig :
    ObservableObject,
    ICalibrationTreeNode,
    ICloneable<SettingCalibrationRelationConfig>,
    IAdaptIn<SettingCalibrationRelationConfig, SettingCalibrationRelationConfig>
{
    [ObservableProperty]
    public partial SysMenuDTO SysMenu { get; set; } = new();

    [ObservableProperty]
    public partial SettingCalibrationRelationConfigItem Item { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<SettingCalibrationRelationConfig> Children { get; set; } = [];

    public ICalibrationCategoryItem CategoryItem => Item;

    IEnumerable ICalibrationTreeNode.Children => Children;

    public SettingCalibrationRelationConfig Clone() => new()
    {
        SysMenu = SysMenu,
        Item = Item.Clone(),
        Children = Children.Select(t => t.Clone()).ToList().AsReadOnly()
    };

    public SettingCalibrationRelationConfig AdaptIn(SettingCalibrationRelationConfig obj)
    {
        SysMenu = obj.SysMenu;
        Item = obj.Item.AdaptIn(obj.Item);
        Children = obj.Children.Select(t => t.AdaptIn(t)).ToList().AsReadOnly();
        return obj;
    }
}

public sealed partial class SettingCalibrationRelationConfigItem :
    TypeInfo,
    ICalibrationCategoryItem,
    ICloneable<SettingCalibrationRelationConfigItem>,
    IAdaptIn<SettingCalibrationRelationConfigItem, SettingCalibrationRelationConfigItem>
{
    [ObservableProperty]
    public partial bool IsUsed { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial bool IsArray { get; set; }

    public bool IsChecked
    {
        get => IsUsed;
        set => IsUsed = value;
    }

    public bool IsEnabled => true;

    partial void OnIsUsedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsChecked));
    }

    public SettingCalibrationRelationConfigItem Clone() => new()
    {
        IsUsed = IsUsed,
        IsArray = IsArray,
        Description = Description,
        AssemblyQualifiedName = AssemblyQualifiedName
    };

    public SettingCalibrationRelationConfigItem AdaptIn(SettingCalibrationRelationConfigItem obj)
    {
        IsUsed = obj.IsUsed;
        IsArray = obj.IsArray;
        Description = obj.Description;
        AssemblyQualifiedName = obj.AssemblyQualifiedName;
        return obj;
    }
}