using System.ComponentModel;

namespace Core.Models.Enums.EFEM;

// ReSharper disable once InconsistentNaming
public enum EFEMAngleEnum
{
    [Description("Up (0°)")]
    Up,

    [Description("Left (90°)")]
    Left,

    [Description("Down (180°)")]
    Down,

    [Description("Right (270°)")]
    Right
}