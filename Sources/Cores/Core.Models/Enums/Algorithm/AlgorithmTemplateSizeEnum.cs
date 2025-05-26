using System.ComponentModel;

namespace Core.Models.Enums.Algorithm;

public enum AlgorithmTemplateSizeEnum
{
    [Description("32")]
    Size32 = 32,

    [Description("64")]
    Size64 = 64,

    [Description("128")]
    Size128 = 128,

    [Description("256")]
    Size256 = 256,

    [Description("512")]
    Size512 = 512
}