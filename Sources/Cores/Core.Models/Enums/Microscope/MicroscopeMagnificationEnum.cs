using System.ComponentModel;

namespace Core.Models.Enums.Microscope;

/// <summary>
/// 显微镜放大倍率
/// </summary>
public enum MicroscopeMagnificationEnum
{
    [Description("5X")]
    Magnification5X,

    [Description("10X")]
    Magnification10X,

    [Description("50X")]
    Magnification50X,

    [Description("100X")]
    Magnification100X,

    [Description("150X")]
    Magnification150X
}