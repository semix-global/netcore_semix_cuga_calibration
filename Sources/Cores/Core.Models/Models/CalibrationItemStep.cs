using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Models.Models;

/// <summary>
/// 校准步骤
/// </summary>
[ObservableRecipient]
public sealed partial class CalibrationItemStep : ObservableObject
{
    /// <summary>
    /// 步骤名称
    /// </summary>
    [ObservableProperty]
    public partial string StepName { get; set; } = string.Empty;

    /// <summary>
    /// 步骤显示索引
    /// </summary>
    [ObservableProperty]
    public partial int StepIndex { get; set; } = 1;

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