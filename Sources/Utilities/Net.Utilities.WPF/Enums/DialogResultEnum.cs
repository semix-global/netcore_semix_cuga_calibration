using Net.Utilities.Attributes;
using Net.Utilities.Properties;
using System.ComponentModel;

namespace Net.Utilities.WPF.Enums;

// ReSharper disable once InconsistentNaming
[Flags]
public enum DialogResultEnum
{
    [Localization(ResourceName = nameof(Lang.DialogResult_None), ResourceType = typeof(Lang))]
    [Description(nameof(None))]
    None = 0x0000_0001,

    [Localization(ResourceName = nameof(Lang.DialogResult_OK), ResourceType = typeof(Lang))]
    [Description(nameof(OK))]
    OK = 0x0000_0010,

    [Localization(ResourceName = nameof(Lang.DialogResult_Cancel), ResourceType = typeof(Lang))]
    [Description(nameof(Cancel))]
    Cancel = 0x0000_0100,

    [Localization(ResourceName = nameof(Lang.DialogResult_Abort), ResourceType = typeof(Lang))]
    [Description(nameof(Abort))]
    Abort = 0x0000_1000,

    [Localization(ResourceName = nameof(Lang.DialogResult_Retry), ResourceType = typeof(Lang))]
    [Description(nameof(Retry))]
    Retry = 0x0001_0000,

    [Localization(ResourceName = nameof(Lang.DialogResult_Ignore), ResourceType = typeof(Lang))]
    [Description(nameof(Ignore))]
    Ignore = 0x0010_0000,

    [Localization(ResourceName = nameof(Lang.DialogResult_Yes), ResourceType = typeof(Lang))]
    [Description(nameof(Yes))]
    Yes = 0x0100_0000,

    [Localization(ResourceName = nameof(Lang.DialogResult_No), ResourceType = typeof(Lang))]
    [Description(nameof(No))]
    No = 0x1000_0000
}