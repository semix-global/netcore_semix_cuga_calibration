using CommunityToolkit.Mvvm.ComponentModel;
using Core.Utilities.WPF.Assembly.Model;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Models.Enums;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Setting;

public sealed partial class SettingDisableCalibrationConfig :
    CalibrationDTOBase<SettingDisableCalibrationConfig>,
    ICloneable<SettingDisableCalibrationConfig>,
    IAdaptIn<SettingDisableCalibrationConfig, SettingDisableCalibrationConfig>
{
    [ObservableProperty]
    public partial string ConfigDescription { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<SettingDisableCalibrationCategory> CalibrationItems { get; set; } = [];

    public SettingDisableCalibrationConfig AdaptIn(SettingDisableCalibrationConfig obj)
    {
        ConfigDescription = obj.ConfigDescription;
        CalibrationItems = obj.CalibrationItems.Select(t => t.AdaptIn(t)).ToList().AsReadOnly();
        Id = obj.Id;
        Expiration = obj.Expiration;
        return obj;
    }

    public override SettingDisableCalibrationConfig Clone() => new()
    {
        ConfigDescription = ConfigDescription,
        CalibrationItems = CalibrationItems.Select(t => t.Clone()).ToList().AsReadOnly(),
        Id = Id,
        Expiration = Expiration
    };

    public bool Equals(SettingDisableCalibrationConfig? other) => this == other;

    public override bool Equals(object? obj) => obj is SettingDisableCalibrationConfig other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Id, ConfigDescription, CalibrationItems.Count);

    public static bool operator ==(SettingDisableCalibrationConfig? left, SettingDisableCalibrationConfig? right) => (left, right) switch
    {
        (null, null) => true,
        (null, _) => false,
        (_, null) => false,
        (_, _) => ReferenceEquals(left, right) || (Equals(left.Id, right.Id) &&
                                                   Equals(left.ConfigDescription, right.ConfigDescription) &&
                                                   left.CalibrationItems.SequenceEqual(right.CalibrationItems))
    };

    public static bool operator !=(SettingDisableCalibrationConfig? left, SettingDisableCalibrationConfig? right) => !(left == right);
}

public sealed partial class SettingDisableCalibrationCategory :
    ObservableObject,
    ICloneable<SettingDisableCalibrationCategory>,
    IAdaptIn<SettingDisableCalibrationCategory, SettingDisableCalibrationCategory>,
    IEquatable<SettingDisableCalibrationCategory>
{
    [ObservableProperty]
    public partial SysMenuDTO SysMenu { get; set; } = new();

    [ObservableProperty]
    private SettingDisableCalibrationCategoryItem _categoryItem = new();

    [ObservableProperty]
    public partial IReadOnlyList<SettingDisableCalibrationCategory> Children { get; set; } = [];

    public string Name => SysMenu.Name;

    public IReadOnlyList<SettingDisableCalibrationCategory> GetAllChildren()
    {
        var result = new List<SettingDisableCalibrationCategory>();

        RecursionFn(this);

        return result;

        void RecursionFn(SettingDisableCalibrationCategory item)
        {
            if (item.SysMenu.MenuTypeEnum == MenuTypeEnum.Menu) result.Add(item);

            foreach (var child in item.Children) RecursionFn(child);
        }
    }

    public SettingDisableCalibrationCategory Clone() => new()
    {
        SysMenu = SysMenu,
        CategoryItem = CategoryItem.Clone(),
        Children = Children.Select(t => t.Clone()).ToList().AsReadOnly()
    };

    public SettingDisableCalibrationCategory AdaptIn(SettingDisableCalibrationCategory obj)
    {
        SysMenu = obj.SysMenu;
        CategoryItem = obj.CategoryItem.AdaptIn(obj.CategoryItem);
        Children = obj.Children.Select(t => t.AdaptIn(t)).ToList().AsReadOnly();
        return obj;
    }

    public bool Equals(SettingDisableCalibrationCategory? other) => this == other;

    public override bool Equals(object? obj) => obj is SettingDisableCalibrationCategory other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(SysMenu.Id, Children.Count);

    public static bool operator ==(SettingDisableCalibrationCategory? left, SettingDisableCalibrationCategory? right) => (left, right) switch
    {
        (null, null) => true,
        (null, _) => false,
        (_, null) => false,
        (_, _) => ReferenceEquals(left, right) || (Equals(left.SysMenu.Id, right.SysMenu.Id) &&
                                                   Equals(left.CategoryItem, right.CategoryItem) &&
                                                   left.Children.SequenceEqual(right.Children))
    };

    public static bool operator !=(SettingDisableCalibrationCategory? left, SettingDisableCalibrationCategory? right) => !(left == right);
}

public partial class SettingDisableCalibrationCategoryItem :
    TypeInfo,
    ICloneable<SettingDisableCalibrationCategoryItem>,
    IAdaptIn<SettingDisableCalibrationCategoryItem, SettingDisableCalibrationCategoryItem>,
    IEquatable<SettingDisableCalibrationCategoryItem>
{
    [ObservableProperty]
    private bool _isDisable;

    public SettingDisableCalibrationCategoryItem Clone() => new()
    {
        IsDisable = IsDisable,
        Description = Description,
        AssemblyQualifiedName = AssemblyQualifiedName
    };

    public SettingDisableCalibrationCategoryItem AdaptIn(SettingDisableCalibrationCategoryItem obj)
    {
        IsDisable = obj.IsDisable;
        Description = obj.Description;
        AssemblyQualifiedName = obj.AssemblyQualifiedName;
        return obj;
    }

    public bool Equals(SettingDisableCalibrationCategoryItem? other) => this == other;

    public override bool Equals(object? obj) => obj is SettingDisableCalibrationCategoryItem other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(IsDisable, Description, AssemblyQualifiedName);

    public static bool operator ==(SettingDisableCalibrationCategoryItem? left, SettingDisableCalibrationCategoryItem? right) => (left, right) switch
    {
        (null, null) => true,
        (null, _) => false,
        (_, null) => false,
        (_, _) => ReferenceEquals(left, right) || (Equals(left.IsDisable, right.IsDisable) &&
                                                   Equals(left.Description, right.Description) &&
                                                   Equals(left.AssemblyQualifiedName, right.AssemblyQualifiedName))
    };

    public static bool operator !=(SettingDisableCalibrationCategoryItem? left, SettingDisableCalibrationCategoryItem? right) => !(left == right);
}