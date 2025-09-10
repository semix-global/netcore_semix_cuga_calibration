using System.Collections;
using CommunityToolkit.Diagnostics;
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

namespace Core.Models.Models.Common.AODWaveform.UI.Generates;

public sealed partial class GenerateAODWaveformParamUserControl
{
    private readonly ILogger<GenerateAODWaveformParamUserControl>? _logger;
    private readonly IDialogWindowProvider? _dialogWindowProvider;

    protected override GenerateAODWaveformParamUserControl InnerControl => this;

    public GenerateAODWaveformParamUserControl()
    {
        InitializeComponent();

        InitializeTransparentProperties();

        if (DesignerProperties.GetIsInDesignMode(this)) return;

        _logger = GuardUtils.IsAssignableToType<ILogger<GenerateAODWaveformParamUserControl>>(HostApplication.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType())));
        _dialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();
    }

    [RelayCommand]
    private void ChangeDirectoryPath() => Invoke(param =>
    {
        var dialog = GuardUtils.IsNotNullAndReturn(_dialogWindowProvider).TryShowSelectDirectoryPathDialog(out var directoryPath);
        if (dialog == false) return;

        param.DirectoryPath = directoryPath;
    });

    [RelayCommand]
    private void AddElectrodeConfiguration() => Invoke(param =>
    {
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
    });

    [RelayCommand]
    private void RemoveElectrodeConfiguration(IEnumerable? selectItems) => Invoke(param =>
    {
        if (selectItems is null) return;

        var electrodeEnums = EnumHelper.Enums<OpticsAODElectrodeEnum>();

        var configurationList = param.ElectrodeConfigurations.ToList();
        foreach (GenerateAODWaveformElectrodeConfiguration selectItem in selectItems) configurationList.Remove(selectItem);

        foreach (var (index, item) in configurationList
                     .Select((item, index) => (index, t: item)))
        {
            item.OpticsAODElectrodeEnum = electrodeEnums[index];
        }

        param.ElectrodeConfigurations = configurationList;
    });

    [RelayCommand]
    private void ImportUniformityConfiguration() => Invoke(param =>
    {
        try
        {
            var dialog = GuardUtils.IsNotNullAndReturn(_dialogWindowProvider).TryShowSelectFilePathDialog(".xlsx", out var filePath);
            if (dialog == false) return;

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
    });

    [RelayCommand]
    private void AddUniformityConfiguration() => Invoke(param =>
    {
        var configurationList = param.UniformityConfigurations.ToList();
        configurationList.Add(new GenerateAODWaveformUniformityConfiguration());

        param.UniformityConfigurations = configurationList;
    });

    [RelayCommand]
    private void RemoveUniformityConfiguration(IEnumerable? selectItems) => Invoke(param =>
    {
        if (selectItems is null) return;

        var configurationList = param.UniformityConfigurations.ToList();
        foreach (GenerateAODWaveformUniformityConfiguration selectItem in selectItems) configurationList.Remove(selectItem);

        param.UniformityConfigurations = configurationList;
    });


    [RelayCommand]
    private void AddSlopeDeltaKConfiguration() => Invoke(param =>
    {
        var configurationList = param.SlopeDeltaKConfigurations.ToList();
        configurationList.Add(new GenerateAODWaveformSlopeDeltaKConfiguration());

        param.SlopeDeltaKConfigurations = configurationList;
    });

    [RelayCommand]
    private void RemoveSlopeDeltaKConfiguration(IEnumerable? selectItems) => Invoke(param =>
    {
        if (selectItems is null) return;

        var configurationList = param.SlopeDeltaKConfigurations.ToList();
        foreach (GenerateAODWaveformSlopeDeltaKConfiguration selectItem in selectItems) configurationList.Remove(selectItem);

        param.SlopeDeltaKConfigurations = configurationList;
    });

    private void Invoke(Action<AbstractGenerateAODWaveformParam> action)
    {
        Guard.IsNotNull(_logger);
        Guard.IsNotNull(_dialogWindowProvider);

        var param = GuardUtils.IsAssignableToType<AbstractGenerateAODWaveformParam>(DataContext);

        action.Invoke(param);
    }
}