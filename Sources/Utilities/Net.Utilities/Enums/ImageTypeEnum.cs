using System.ComponentModel;

namespace Net.Utilities.Enums;

public enum ImageTypeEnum
{
    [Description("image/bmp")]
    Bmp,

    [Description("image/jpeg")]
    Jpeg,

    [Description("image/png")]
    Png,

    [Description("image/gif")]
    Gif,

    [Description("image/x-icon")]
    Ico
}