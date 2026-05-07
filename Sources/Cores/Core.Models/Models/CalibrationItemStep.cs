using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Models.Models;

[ObservableRecipient]
public sealed partial class CalibrationItemStep : ObservableObject
{
    [ObservableProperty]
    public partial string StepName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int StepIndex { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool StepIsNextEnable { get; set; }

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