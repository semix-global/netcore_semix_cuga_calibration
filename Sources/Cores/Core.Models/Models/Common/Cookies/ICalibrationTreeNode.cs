using Local.SQL.DB.Providers.Models.Entities.DTO;
using System.Collections;

namespace Core.Models.Models.Common.Cookies;

public interface ICalibrationTreeNode
{
    SysMenuDTO SysMenu { get; set; }

    ICalibrationCategoryItem CategoryItem { get; }

    IEnumerable Children { get; }
}

internal sealed class CalibrationCategoryItemEmpty : ICalibrationCategoryItem
{
    public static readonly CalibrationCategoryItemEmpty Instance = new();

    public bool IsChecked { get => false; set { } }

    public bool IsEnabled => false;
}
