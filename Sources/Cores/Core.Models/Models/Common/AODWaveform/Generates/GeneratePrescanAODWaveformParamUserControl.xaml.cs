using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.Enums;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public partial class GeneratePrescanAODWaveformParamUserControl
{
    private readonly ILogger<GeneratePrescanAODWaveformParamUserControl> _logger;
    private readonly IDialogWindowProvider _dialogWindowProvider;

    public GeneratePrescanAODWaveformParamUserControl()
    {
        InitializeComponent();

        _logger = HostApplication.GetRequiredService<ILogger<GeneratePrescanAODWaveformParamUserControl>>();
        _dialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();
    }

    [RelayCommand]
    private void ChangeDirectoryPath()
    {
        var param = GuardUtils.IsAssignableToType<GeneratePrescanAODWaveformParam>(DataContext);

        var dialog = _dialogWindowProvider.TryShowSelectDirectoryPathDialog(out var directoryPath);
        if (dialog == false) return;

        param.DirectoryPath = directoryPath;
    }

    [RelayCommand]
    private void AddElectrodeConfiguration()
    {
        var param = GuardUtils.IsAssignableToType<GeneratePrescanAODWaveformParam>(DataContext);
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
    private void RemoveElectrodeConfiguration(GenerateAODWaveformElectrodeConfiguration selectItem)
    {
        var param = GuardUtils.IsAssignableToType<GeneratePrescanAODWaveformParam>(DataContext);
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
            var dialog = _dialogWindowProvider.TryShowSelectFilePathDialog(".xlsx", out var filePath);
            if (dialog == false) return;

            var param = GuardUtils.IsAssignableToType<GeneratePrescanAODWaveformParam>(DataContext);

            var values = MiniExcel.Query<GenerateAODWaveformUniformityConfiguration>(filePath).ToArray();
            if (values.Length > 0) param.UniformityConfigurations = values;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{@Name}: Import Uniformity Configuration", nameof(GeneratePrescanAODWaveformParamUserControl));
            _dialogWindowProvider.ShowDialog($"""
                                              Import Uniformity Configuration Failed!
                                              {ex}
                                              """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand]
    private void AddUniformityConfiguration()
    {
        var param = GuardUtils.IsAssignableToType<GeneratePrescanAODWaveformParam>(DataContext);

        var configurationList = param.UniformityConfigurations.ToList();
        configurationList.Add(new GenerateAODWaveformUniformityConfiguration());

        param.UniformityConfigurations = configurationList;
    }

    [RelayCommand]
    private void RemoveUniformityConfiguration(GenerateAODWaveformUniformityConfiguration selectItem)
    {
        var param = GuardUtils.IsAssignableToType<GeneratePrescanAODWaveformParam>(DataContext);

        var configurationList = param.UniformityConfigurations.ToList();
        configurationList.Remove(selectItem);

        param.UniformityConfigurations = configurationList;
    }


    [RelayCommand]
    private void AddSlopeDeltaKConfiguration()
    {
        var param = GuardUtils.IsAssignableToType<GeneratePrescanAODWaveformParam>(DataContext);

        var configurationList = param.SlopeDeltaKConfigurations.ToList();
        configurationList.Add(0d);

        param.SlopeDeltaKConfigurations = configurationList;
    }

    [RelayCommand]
    private void RemoveSlopeDeltaKConfiguration(int selectIndex)
    {
        var param = GuardUtils.IsAssignableToType<GeneratePrescanAODWaveformParam>(DataContext);
        if (selectIndex < 0 || param.SlopeDeltaKConfigurations.Count <= selectIndex) return;

        var configurationList = param.UniformityConfigurations.ToList();
        configurationList.RemoveAt(selectIndex);

        param.UniformityConfigurations = configurationList;
    }
}