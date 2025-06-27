using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;

namespace Core.Models.Models;

/// <summary>
/// 校准步骤
/// </summary>
[ObservableRecipient]
public sealed partial class CalibrationItemStep : ObservableCacheBase
{
    private readonly bool _defaultIsNextEnable;

    /// <summary>
    /// 步骤名称
    /// </summary>
    [ObservableProperty]
    private string _stepName = string.Empty;

    /// <summary>
    /// 步骤显示索引
    /// </summary>
    [ObservableProperty]
    private int _stepIndex = 1;

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