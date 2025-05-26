using System.ComponentModel;

namespace Core.Models.Enums.Recipe.Wafer;

public enum WaferMaskTypeEnum
{
    [Description("5um")]
    Grid_5um,

    [Description("8um")]
    Grid_8um,

    [Description("10um")]
    Grid_10um,

    [Description("25um")]
    Grid_25um,

    [Description("50um")]
    Grid_50um,

    [Description("100um")]
    Grid_100um,

    [Description("50umGridCorner")]
    GridConrner_50um,

    [Description("Caliper")]
    Caliper,

    [Description("Butterfly")]
    Butterfly,

    [Description("DieCorner")]
    DieCorner,
}