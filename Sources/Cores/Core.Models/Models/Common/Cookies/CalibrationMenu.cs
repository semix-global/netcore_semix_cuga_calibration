using CommunityToolkit.Mvvm.ComponentModel;
using Core.Utilities;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Models.Enums;

namespace Core.Models.Models.Common.Cookies;

public sealed partial class CalibrationMenu : ObservableObject
{
    [ObservableProperty]
    public partial SysMenuDTO SysMenu { get; set; } = new();

    [ObservableProperty]
    public partial CalibrationViewModelEntry Entry { get; set; } = CalibrationViewModelEntry.Default;

    [ObservableProperty]
    public partial IReadOnlyList<CalibrationMenu> Children { get; set; } = [];

    public IReadOnlyList<CalibrationMenu> GetAllChildren()
    {
        var result = new List<CalibrationMenu>();

        RecursionFn(this);

        return result;

        void RecursionFn(CalibrationMenu item)
        {
            if (item.SysMenu.MenuTypeEnum == MenuTypeEnum.Menu) result.Add(item);

            foreach (var child in item.Children) RecursionFn(child);
        }
    }

    public string GetFullName(ApplicationSetting applicationSetting)
    {
        var linkedList = new LinkedList<string>();
        linkedList.AddLast(SysMenu.Name);

        var current = SysMenu.Parent;
        while (current is not null)
        {
            if (current.Name == applicationSetting.CalibrationMenuName) break;

            linkedList.AddFirst(current.Name);

            current = current.Parent;
        }

        return string.Join("/", linkedList);
    }
}