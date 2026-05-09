using CommunityToolkit.Mvvm.ComponentModel;
using Local.SQL.DB.Providers.Models.Entities.DTO;

namespace Core.Models.Models.Common.Cookies;

public sealed partial class CalibrationMenu : ObservableObject
{
    [ObservableProperty]
    public partial SysMenuDTO SysMenu { get; set; } = new();

    [ObservableProperty]
    public partial CalibrationViewModelEntry Entry { get; set; } = CalibrationViewModelEntry.Default;

    [ObservableProperty]
    public partial IReadOnlyList<CalibrationMenu> Children { get; set; } = [];
}