using System.ComponentModel;

namespace Core.Models.Enums.Stage;

/// <summary>
/// chuck位置, 耳朵DSW位置, 耳朵未定义位置, 耳朵Haze位置, 耳朵ShinyWafer位置, 中心位置
/// </summary>
public enum CalChipSiteModelEnum
{
    [Description("Chuck")]
    ChuckModel = 0,

    [Description("DSW")]
    DswModel = 1,

    [Description("Undefined")]
    UndefinedModel = 2,

    [Description("Haze")]
    HazeModel = 3,

    [Description("Shiny Wafer")]
    ShinyWaferModel = 4
}