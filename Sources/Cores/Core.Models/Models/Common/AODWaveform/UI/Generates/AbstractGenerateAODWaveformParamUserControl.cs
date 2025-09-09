using System.Windows;

namespace Core.Models.Models.Common.AODWaveform.UI.Generates;

public abstract class AbstractGenerateAODWaveformParamUserControl : System.Windows.Controls.UserControl
{
    protected abstract GenerateAODWaveformParamUserControl InnerControl { get; }

    public static readonly DependencyProperty IsVisibleElectrodeConfigurationProperty = DependencyProperty.Register(
        nameof(IsVisibleElectrodeConfiguration),
        typeof(bool),
        typeof(AbstractGenerateAODWaveformParamUserControl),
        new PropertyMetadata(true, OnIsVisibleElectrodeConfigurationChanged));

    public static readonly DependencyProperty IsVisibleUniformityConfigurationProperty = DependencyProperty.Register(
        nameof(IsVisibleUniformityConfiguration),
        typeof(bool),
        typeof(AbstractGenerateAODWaveformParamUserControl),
        new PropertyMetadata(true, OnIsVisibleUniformityConfigurationChanged));

    public static readonly DependencyProperty IsVisibleSlopeDeltaKConfigurationProperty = DependencyProperty.Register(
        nameof(IsVisibleSlopeDeltaKConfiguration),
        typeof(bool),
        typeof(AbstractGenerateAODWaveformParamUserControl),
        new PropertyMetadata(true, OnIsVisibleSlopeDeltaKConfigurationChanged));

    public static readonly DependencyProperty IsVisibleCompensationProperty = DependencyProperty.Register(
        nameof(IsVisibleCompensation),
        typeof(bool),
        typeof(AbstractGenerateAODWaveformParamUserControl),
        new PropertyMetadata(true, OnIsVisibleCompensationChanged));

    private static void OnIsVisibleElectrodeConfigurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AbstractGenerateAODWaveformParamUserControl control && e.NewValue is bool value) control.InnerControl.IsVisibleElectrodeConfiguration = value;
    }

    private static void OnIsVisibleUniformityConfigurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AbstractGenerateAODWaveformParamUserControl control && e.NewValue is bool value) control.InnerControl.IsVisibleUniformityConfiguration = value;
    }

    private static void OnIsVisibleSlopeDeltaKConfigurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AbstractGenerateAODWaveformParamUserControl control && e.NewValue is bool value) control.InnerControl.IsVisibleSlopeDeltaKConfiguration = value;
    }

    private static void OnIsVisibleCompensationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AbstractGenerateAODWaveformParamUserControl control && e.NewValue is bool value) control.InnerControl.IsVisibleCompensation = value;
    }

    public bool IsVisibleUniformityConfiguration
    {
        get => (bool)GetValue(IsVisibleUniformityConfigurationProperty);
        set => SetValue(IsVisibleUniformityConfigurationProperty, value);
    }

    public bool IsVisibleElectrodeConfiguration
    {
        get => (bool)GetValue(IsVisibleElectrodeConfigurationProperty);
        set => SetValue(IsVisibleElectrodeConfigurationProperty, value);
    }

    public bool IsVisibleSlopeDeltaKConfiguration
    {
        get => (bool)GetValue(IsVisibleSlopeDeltaKConfigurationProperty);
        set => SetValue(IsVisibleSlopeDeltaKConfigurationProperty, value);
    }

    public bool IsVisibleCompensation
    {
        get => (bool)GetValue(IsVisibleCompensationProperty);
        set => SetValue(IsVisibleCompensationProperty, value);
    }

    protected void InitializeTransparentProperties()
    {
        InnerControl.IsVisibleElectrodeConfiguration = IsVisibleElectrodeConfiguration;
        InnerControl.IsVisibleUniformityConfiguration = IsVisibleUniformityConfiguration;
        InnerControl.IsVisibleSlopeDeltaKConfiguration = IsVisibleSlopeDeltaKConfiguration;
        InnerControl.IsVisibleCompensation = IsVisibleCompensation;
    }
}