namespace Net.Utilities.WPF.Enums;

// ReSharper disable InconsistentNaming
public enum DialogButtonsEnum
{
    OK = DialogResultEnum.OK,
    OKCancel = DialogResultEnum.OK | DialogResultEnum.Cancel,
    AbortRetryIgnore = DialogResultEnum.Abort | DialogResultEnum.Retry | DialogResultEnum.Ignore,
    YesNoCancel = DialogResultEnum.Yes | DialogResultEnum.No | DialogResultEnum.Cancel,
    YesNo = DialogResultEnum.Yes | DialogResultEnum.No,
    RetryCancel = DialogResultEnum.Retry | DialogResultEnum.Cancel
}