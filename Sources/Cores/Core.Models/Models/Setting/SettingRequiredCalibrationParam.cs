using CommunityToolkit.Mvvm.ComponentModel;
using Core.Utilities.WPF.Assembly.Model;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Setting;

public sealed partial class SettingRequiredCalibrationParam : ObservableCacheBase, ICloneable<SettingRequiredCalibrationParam>, IAdaptIn<SettingRequiredCalibrationParam, SettingRequiredCalibrationParam>
{
    [ObservableProperty]
    private string _description = string.Empty;

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
    private bool _isRequired;

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