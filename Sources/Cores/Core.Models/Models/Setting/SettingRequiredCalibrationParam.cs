using CommunityToolkit.Mvvm.ComponentModel;
using Core.Utilities.WPF.Assembly.Model;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Models.Enums;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Setting;

public sealed partial class SettingRequiredCalibrationParam :
    ObservableObject,
    ICloneable<SettingRequiredCalibrationParam>,
    IAdaptIn<SettingRequiredCalibrationParam, SettingRequiredCalibrationParam>
{
    [ObservableProperty]
    public partial SysMenuDTO SysMenu { get; set; } = new();

    [ObservableProperty]
    private SettingRequiredCalibrationCategoryItem _categoryItem = new();

    [ObservableProperty]
    public partial IReadOnlyList<SettingRequiredCalibrationParam> Children { get; set; } = [];

    public string Name => SysMenu.Name;

    public IReadOnlyList<SettingRequiredCalibrationParam> GetAllChildren()
    {
        var result = new List<SettingRequiredCalibrationParam>();

        RecursionFn(this);

        return result;

        void RecursionFn(SettingRequiredCalibrationParam item)
        {
            if (item.SysMenu.MenuTypeEnum == MenuTypeEnum.Menu) result.Add(item);

            foreach (var child in item.Children) RecursionFn(child);
        }
    }

    public SettingRequiredCalibrationParam Clone() => new()
    {
        SysMenu = SysMenu,
        CategoryItem = CategoryItem.Clone(),
        Children = Children.Select(t => t.Clone()).ToList().AsReadOnly()
    };

    public SettingRequiredCalibrationParam AdaptIn(SettingRequiredCalibrationParam obj)
    {
        SysMenu = obj.SysMenu;
        CategoryItem = obj.CategoryItem.AdaptIn(obj.CategoryItem);
        Children = obj.Children.Select(t => t.AdaptIn(t)).ToList().AsReadOnly();
        return obj;
    }
}

public partial class SettingRequiredCalibrationCategoryItem :
    TypeInfo,
    ICloneable<SettingRequiredCalibrationCategoryItem>,
    IAdaptIn<SettingRequiredCalibrationCategoryItem, SettingRequiredCalibrationCategoryItem>
{
    [ObservableProperty]
    public partial bool IsRequired { get; set; }

    public SettingRequiredCalibrationCategoryItem Clone() => new()
    {
        IsRequired = IsRequired,
        Description = Description,
        AssemblyQualifiedName = AssemblyQualifiedName
    };

    public SettingRequiredCalibrationCategoryItem AdaptIn(SettingRequiredCalibrationCategoryItem obj)
    {
        IsRequired = obj.IsRequired;
        Description = obj.Description;
        AssemblyQualifiedName = obj.AssemblyQualifiedName;
        return obj;
    }
}
