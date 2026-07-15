using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Cookies;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Models.Enums;
using Net.Utilities.Mapper.Interfaces;
using System.Collections;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Setting.CalibrationRelationConfig;

public sealed partial class SettingCalibrationRelationParam :
    ObservableObject,
    ICalibrationTreeNode,
    ICloneable<SettingCalibrationRelationParam>,
    IAdaptIn<SettingCalibrationRelationParam, SettingCalibrationRelationParam>
{
    [ObservableProperty]
    public partial SysMenuDTO SysMenu { get; set; } = new();

    [ObservableProperty]
    public partial SettingCalibrationRelationCategoryItem Item { get; set; } = new();

    public ICalibrationCategoryItem CategoryItem => CalibrationCategoryItemEmpty.Instance;

    [ObservableProperty]
    public partial IReadOnlyList<SettingCalibrationRelationParam> Children { get; set; } = [];

    IEnumerable ICalibrationTreeNode.Children => Children;

    public IReadOnlyList<SettingCalibrationRelationParam> GetAllChildren()
    {
        var result = new List<SettingCalibrationRelationParam>();

        RecursionFn(this);

        return result;

        void RecursionFn(SettingCalibrationRelationParam item)
        {
            if (item.SysMenu.MenuTypeEnum == MenuTypeEnum.Menu) result.Add(item);

            foreach (var child in item.Children) RecursionFn(child);
        }
    }

    public SettingCalibrationRelationParam Clone() => new()
    {
        SysMenu = SysMenu,
        Item = Item.Clone(),
        Children = Children.Select(t => t.Clone()).ToList().AsReadOnly()
    };

    public SettingCalibrationRelationParam AdaptIn(SettingCalibrationRelationParam obj)
    {
        SysMenu = obj.SysMenu;
        Item = obj.Item.AdaptIn(obj.Item);
        Children = obj.Children.Select(t => t.AdaptIn(t)).ToList().AsReadOnly();
        return obj;
    }
}

public sealed partial class SettingCalibrationRelationCategoryItem :
    ObservableObject,
    ICloneable<SettingCalibrationRelationCategoryItem>,
    IAdaptIn<SettingCalibrationRelationCategoryItem, SettingCalibrationRelationCategoryItem>
{
    [ObservableProperty]
    public partial ObservableCollection<SettingCalibrationRelationConfig> DependencyRelationConfigs { get; set; } = [];

    public SettingCalibrationRelationCategoryItem Clone() => new()
    {
        DependencyRelationConfigs = new ObservableCollection<SettingCalibrationRelationConfig>(DependencyRelationConfigs.Select(t => t.Clone())),
    };

    public SettingCalibrationRelationCategoryItem AdaptIn(SettingCalibrationRelationCategoryItem obj)
    {
        DependencyRelationConfigs = new ObservableCollection<SettingCalibrationRelationConfig>(obj.DependencyRelationConfigs.Select(t => t.AdaptIn(t)));
        return obj;
    }
}