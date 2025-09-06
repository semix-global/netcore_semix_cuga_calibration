using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using System.ComponentModel;
using System.Windows;

namespace Core.Models.Models.Common.AODWaveform.UI.Generates;

public sealed partial class GenerateAODWaveformParamUserControl
{
    private readonly ILogger<GenerateAODWaveformParamUserControl>? _logger;
    private readonly IDialogWindowProvider? _dialogWindowProvider;

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header),
        typeof(string),
        typeof(GenerateAODWaveformParamUserControl),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty HeaderContentProperty = DependencyProperty.Register(
        nameof(HeaderContent),
        typeof(object),
        typeof(GenerateAODWaveformParamUserControl),
        new PropertyMetadata(null));

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public object HeaderContent
    {
        get => GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    public GenerateAODWaveformParamUserControl()
    {
        InitializeComponent();

        if (DesignerProperties.GetIsInDesignMode(this)) return;

        _logger = GuardUtils.IsAssignableToType<ILogger<GenerateAODWaveformParamUserControl>>(HostApplication.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType())));
        _dialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();
    }

    [RelayCommand]
    private void ChangeDirectoryPath()
    {
        var param = GuardUtils.IsAssignableToType<AbstractGenerateAODWaveformParam>(DataContext);

        var dialog = GuardUtils.IsNotNullAndReturn(_dialogWindowProvider).TryShowSelectDirectoryPathDialog(out var directoryPath);
        if (dialog == false) return;

        param.DirectoryPath = directoryPath;
    }

    [RelayCommand]
    private void AddElectrodeConfiguration()
    {
        var param = GuardUtils.IsAssignableToType<AbstractGenerateAODWaveformParam>(DataContext);
        var electrodeEnums = EnumHelper.Enums<OpticsAODElectrodeEnum>();

        if (param.ElectrodeConfigurations.Count > electrodeEnums.Length) return;

        var configurationList = param.ElectrodeConfigurations.ToList();
        configurationList.Add(new GenerateAODWaveformElectrodeConfiguration());

        foreach (var (index, item) in configurationList
                     .Select((item, index) => (index, t: item)))
        {
            item.OpticsAODElectrodeEnum = electrodeEnums[index];
        }

        param.ElectrodeConfigurations = configurationList;
    }

    [RelayCommand]
    private void RemoveElectrodeConfiguration(GenerateAODWaveformElectrodeConfiguration? selectItem)
    {
        if (selectItem is null) return;

        var param = GuardUtils.IsAssignableToType<AbstractGenerateAODWaveformParam>(DataContext);
        var electrodeEnums = EnumHelper.Enums<OpticsAODElectrodeEnum>();

        var configurationList = param.ElectrodeConfigurations.ToList();
        configurationList.Remove(selectItem);

        foreach (var (index, item) in configurationList
                     .Select((item, index) => (index, t: item)))
        {
            item.OpticsAODElectrodeEnum = electrodeEnums[index];
        }

        param.ElectrodeConfigurations = configurationList;
    }

    [RelayCommand]
    private void ImportUniformityConfiguration()
    {
        try
        {
            var dialog = GuardUtils.IsNotNullAndReturn(_dialogWindowProvider).TryShowSelectFilePathDialog(".xlsx", out var filePath);
            if (dialog == false) return;

            var param = GuardUtils.IsAssignableToType<AbstractGenerateAODWaveformParam>(DataContext);

            var values = MiniExcel.Query<GenerateAODWaveformUniformityConfiguration>(filePath).ToArray();
            if (values.Length > 0) param.UniformityConfigurations = values;
        }
        catch (Exception ex)
        {
            GuardUtils.IsNotNullAndReturn(_logger).LogError(ex, "{@Name}: Import Uniformity Configuration", nameof(GenerateAODWaveformParamUserControl));
            GuardUtils.IsNotNullAndReturn(_dialogWindowProvider).ShowDialog($"""
                                                                             Import Uniformity Configuration Failed!
                                                                             {ex.Message}
                                                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand]
    private void AddUniformityConfiguration()
    {
        var param = GuardUtils.IsAssignableToType<AbstractGenerateAODWaveformParam>(DataContext);

        var configurationList = param.UniformityConfigurations.ToList();
        configurationList.Add(new GenerateAODWaveformUniformityConfiguration());

        param.UniformityConfigurations = configurationList;
    }

    [RelayCommand]
    private void RemoveUniformityConfiguration(GenerateAODWaveformUniformityConfiguration? selectItem)
    {
        if (selectItem is null) return;

        var param = GuardUtils.IsAssignableToType<AbstractGenerateAODWaveformParam>(DataContext);

        var configurationList = param.UniformityConfigurations.ToList();
        configurationList.Remove(selectItem);

        param.UniformityConfigurations = configurationList;
    }


    [RelayCommand]
    private void AddSlopeDeltaKConfiguration()
    {
        var param = GuardUtils.IsAssignableToType<AbstractGenerateAODWaveformParam>(DataContext);

        var configurationList = param.SlopeDeltaKConfigurations.ToList();
        configurationList.Add(new GenerateAODWaveformSlopeDeltaKConfiguration());

        param.SlopeDeltaKConfigurations = configurationList;
    }

    [RelayCommand]
    private void RemoveSlopeDeltaKConfiguration(GenerateAODWaveformSlopeDeltaKConfiguration? selectItem)
    {
        if (selectItem is null) return;

        var param = GuardUtils.IsAssignableToType<AbstractGenerateAODWaveformParam>(DataContext);

        var configurationList = param.SlopeDeltaKConfigurations.ToList();
        configurationList.Remove(selectItem);

        param.SlopeDeltaKConfigurations = configurationList;
    }
}