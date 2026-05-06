using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Models.Models;

public partial class CalibrationItemStatus : ObservableObject
{
    public bool IsOk => Progress >= 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOk))]
    public partial double Progress { get; set; }

    [ObservableProperty]
    public partial string MarkdownMessage { get; set; } = string.Empty;
}