using System.Windows;

namespace Core.Models.Models.Common.AODWaveform.UI.Generates;

public abstract class AbstractGenerateAODWaveformParamUserControl : System.Windows.Controls.UserControl
{
    protected abstract GenerateAODWaveformParamUserControl InnerControl { get; }

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header),
        typeof(string),
        typeof(GenerateAODWaveformParamUserControl),
        new PropertyMetadata(string.Empty, OnHeaderChanged));

    public static readonly DependencyProperty HeaderContentProperty = DependencyProperty.Register(
        nameof(HeaderContent),
        typeof(object),
        typeof(GenerateAODWaveformParamUserControl),
        new PropertyMetadata(null, OnHeaderContentChanged));

    public static readonly DependencyProperty IsVisibleOpticsIlluminationModeEnumProperty = DependencyProperty.Register(
        nameof(IsVisibleOpticsIlluminationModeEnum),
        typeof(bool),
        typeof(AbstractGenerateAODWaveformParamUserControl),
        new PropertyMetadata(true, OnIsVisibleOpticsIlluminationModeEnumChanged));

    public static readonly DependencyProperty IsVisibleProductivityInformationProperty = DependencyProperty.Register(
        nameof(IsVisibleProductivityInformation),
        typeof(bool),
        typeof(AbstractGenerateAODWaveformParamUserControl),
        new PropertyMetadata(true, OnIsVisibleProductivityInformationChanged));

    public static readonly DependencyProperty IsVisibleFrequencyProperty = DependencyProperty.Register(
        nameof(IsVisibleFrequency),
        typeof(bool),
        typeof(AbstractGenerateAODWaveformParamUserControl),
        new PropertyMetadata(true, OnIsVisibleFrequencyChanged));

    public static readonly DependencyProperty IsVisibleAmplitudeProperty = DependencyProperty.Register(
        nameof(IsVisibleAmplitude),
        typeof(bool),
        typeof(AbstractGenerateAODWaveformParamUserControl),
        new PropertyMetadata(true, OnIsVisibleAmplitudeChanged));

    public static readonly DependencyProperty IsVisibleDirectoryPathProperty = DependencyProperty.Register(
        nameof(IsVisibleDirectoryPath),
        typeof(bool),
        typeof(AbstractGenerateAODWaveformParamUserControl),
        new PropertyMetadata(true, OnIsVisibleDirectoryPathChanged));

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

    private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AbstractGenerateAODWaveformParamUserControl control && e.NewValue is string value) control.InnerControl.Header = value;
    }

    private static void OnHeaderContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AbstractGenerateAODWaveformParamUserControl control) control.InnerControl.HeaderContent = e.NewValue;
    }

    private static void OnIsVisibleOpticsIlluminationModeEnumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AbstractGenerateAODWaveformParamUserControl control && e.NewValue is bool value) control.InnerControl.IsVisibleOpticsIlluminationModeEnum = value;
    }

    private static void OnIsVisibleProductivityInformationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AbstractGenerateAODWaveformParamUserControl control && e.NewValue is bool value) control.InnerControl.IsVisibleProductivityInformation = value;
    }

    private static void OnIsVisibleFrequencyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AbstractGenerateAODWaveformParamUserControl control && e.NewValue is bool value) control.InnerControl.IsVisibleFrequency = value;
    }

    private static void OnIsVisibleAmplitudeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AbstractGenerateAODWaveformParamUserControl control && e.NewValue is bool value) control.InnerControl.IsVisibleAmplitude = value;
    }

    private static void OnIsVisibleDirectoryPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AbstractGenerateAODWaveformParamUserControl control && e.NewValue is bool value) control.InnerControl.IsVisibleDirectoryPath = value;
    }

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

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public object? HeaderContent
    {
        get => GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    public bool IsVisibleOpticsIlluminationModeEnum
    {
        get => (bool)GetValue(IsVisibleOpticsIlluminationModeEnumProperty);
        set => SetValue(IsVisibleOpticsIlluminationModeEnumProperty, value);
    }

    public bool IsVisibleProductivityInformation
    {
        get => (bool)GetValue(IsVisibleProductivityInformationProperty);
        set => SetValue(IsVisibleProductivityInformationProperty, value);
    }

    public bool IsVisibleFrequency
    {
        get => (bool)GetValue(IsVisibleFrequencyProperty);
        set => SetValue(IsVisibleFrequencyProperty, value);
    }

    public bool IsVisibleDirectoryPath
    {
        get => (bool)GetValue(IsVisibleDirectoryPathProperty);
        set => SetValue(IsVisibleDirectoryPathProperty, value);
    }

    public bool IsVisibleAmplitude
    {
        get => (bool)GetValue(IsVisibleAmplitudeProperty);
        set => SetValue(IsVisibleAmplitudeProperty, value);
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
        InnerControl.IsVisibleProductivityInformation = IsVisibleProductivityInformation;
        InnerControl.IsVisibleFrequency = IsVisibleFrequency;
        InnerControl.IsVisibleAmplitude = IsVisibleAmplitude;
        InnerControl.IsVisibleDirectoryPath = IsVisibleDirectoryPath;
        InnerControl.IsVisibleElectrodeConfiguration = IsVisibleElectrodeConfiguration;
        InnerControl.IsVisibleUniformityConfiguration = IsVisibleUniformityConfiguration;
        InnerControl.IsVisibleSlopeDeltaKConfiguration = IsVisibleSlopeDeltaKConfiguration;
        InnerControl.IsVisibleCompensation = IsVisibleCompensation;
    }
}