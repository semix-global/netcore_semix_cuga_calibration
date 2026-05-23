using CommunityToolkit.Mvvm.ComponentModel;
using Core.Utilities.WPF.Assembly.Model;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Setting;

public sealed partial class SettingRequiredCalibrationParam : ObservableObject, ICloneable<SettingRequiredCalibrationParam>, IAdaptIn<SettingRequiredCalibrationParam, SettingRequiredCalibrationParam>
{
    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    public IReadOnlyList<SettingRequiredCalibrationCategoryItem> CategoryItems { get; set; } = [];

    public SettingRequiredCalibrationParam Clone() => new()
    {
        Description = Description,
        CategoryItems = CategoryItems.Select(t => t.Clone()).ToList().AsReadOnly()
    };

    public SettingRequiredCalibrationParam AdaptIn(SettingRequiredCalibrationParam obj)
    {
        Description = obj.Description;
        CategoryItems = obj.CategoryItems.Select(t => t.AdaptIn(t)).ToList().AsReadOnly();
        return obj;
    }
}

public partial class SettingRequiredCalibrationCategoryItem : TypeInfo, ICloneable<SettingRequiredCalibrationCategoryItem>, IAdaptIn<SettingRequiredCalibrationCategoryItem, SettingRequiredCalibrationCategoryItem>
{
    [ObservableProperty]
    public partial bool IsRequired { get; set; }

    public SettingRequiredCalibrationCategoryItem Clone() => new()
    {
        Description = Description,
        IsRequired = IsRequired,
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