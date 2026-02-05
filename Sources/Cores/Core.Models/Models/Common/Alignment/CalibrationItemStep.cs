using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Models.Models.Common.Alignment;

/// <summary>
/// 对准步骤
/// </summary>
[ObservableRecipient]
public sealed partial class AlignmentItemStep : ObservableObject
{
    private readonly bool _defaultIsNextEnable;

    /// <summary>
    /// 步骤名称
    /// </summary>
    [ObservableProperty]
    private string _stepName = string.Empty;

    /// <summary>
    /// 下一步是否可用
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private bool _stepIsNextEnable;

    /// <summary>
    /// 下一步是否可用默认值
    /// </summary>
    public bool DefaultIsNextEnable
    {
        get => _defaultIsNextEnable;
        init
        {
            SetProperty(ref _defaultIsNextEnable, value);
            StepIsNextEnable = value;
        }
    }
}