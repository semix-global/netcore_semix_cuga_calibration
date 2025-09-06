using System.Windows;
using Core.Models.Models.Common.AODWaveform.UI.Generates;

namespace Core.Models.Models.Common.AODWaveform.UI;

public sealed partial class AODWaveformProfilesUserControl
{
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header),
        typeof(string),
        typeof(AODWaveformProfilesUserControl),
        new PropertyMetadata(string.Empty));

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public AODWaveformProfilesUserControl()
    {
        InitializeComponent();
    }
}