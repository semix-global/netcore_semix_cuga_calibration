using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Models.Models.Common.Alignment;

/// <summary>
/// 对准步骤
/// </summary>
[ObservableRecipient]
public sealed partial class AlignmentItemStep : ObservableObject
{
    /// <summary>
    /// 步骤名称
    /// </summary>
    [ObservableProperty]
    public partial string StepName { get; set; } = string.Empty;

    /// <summary>
    /// 下一步是否可用
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool StepIsNextEnable { get; set; }

    /// <summary>
    /// 下一步是否可用默认值
    /// </summary>
    public bool DefaultIsNextEnable
    {
        get;
        init
        {
            SetProperty(ref field, value);
            StepIsNextEnable = value;
        }
    }
}