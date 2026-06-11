using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Cookies;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Core.Models.Models.Common.AODWaveform.UI.Generates;

public sealed partial class GenerateAODWaveformParamUserControl
{
    private readonly ILogger<GenerateAODWaveformParamUserControl>? _logger;
    private readonly IDialogWindowProvider? _dialogWindowProvider;

    public ApplicationCookie ApplicationCookie => HostApplication.GetRequiredService<ApplicationCookie>();

    protected override GenerateAODWaveformParamUserControl InnerControl => this;

    public GenerateAODWaveformParamUserControl()
    {
        InitializeComponent();

        InitializeTransparentProperties();

        if (DesignerProperties.GetIsInDesignMode(this)) return;

        _logger = Guard.IsAssignableToTypeAndReturn<ILogger<GenerateAODWaveformParamUserControl>>(HostApplication.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType())));
        _dialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();
    }

    [RelayCommand]
    private void ChangeDirectoryPath() => Invoke(param =>
    {
        var dialog = Guard.IsNotNullAndReturn(_dialogWindowProvider).TryShowSelectDirectoryPathDialog(out var directoryPath);
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
    private void ImportUniformityConfiguration(GenerateAODWaveformElectrodeConfiguration generateAODWaveformElectrodeConfiguration) => Invoke(_ =>
    {
        try
        {
            var dialog = Guard.IsNotNullAndReturn(_dialogWindowProvider).TryShowSelectFilePathDialog(".xlsx", out var filePath);
            if (dialog == false) return;

            generateAODWaveformElectrodeConfiguration.UniformityConfigurations = [];

            var values = MiniExcel.Query<GenerateAODWaveformUniformityConfiguration>(filePath)
                .Where(t => t.Frequency > 0)
                .ToArray();
            if (values.Length <= 0)
            {
                values = MiniExcel.Query(filePath, useHeaderRow: true)
                    .Cast<IDictionary<string, object>>()
                    .Select(t => new GenerateAODWaveformUniformityConfiguration { Frequency = (double)t[nameof(Point.X)], Coefficient = (double)t[nameof(Point.Y)] })
                    .Where(t => t.Frequency > 0)
                    .ToArray();
            }

            if (values.Length > 0)
            {
                generateAODWaveformElectrodeConfiguration.UniformityConfigurations = values;
                Guard.IsNotNullAndReturn(_logger).LogInformation("Import Uniformity Configuration OK!");
                Guard.IsNotNullAndReturn(_dialogWindowProvider).ShowDialog("Import Uniformity Configuration OK!");
            }
            else
            {
                Guard.IsNotNullAndReturn(_logger).LogWarning("Import Uniformity Configuration Failed! No data found.");
                Guard.IsNotNullAndReturn(_dialogWindowProvider).ShowDialog("Import Uniformity Configuration Failed! No data found.", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        }
        catch (Exception ex)
        {
            Guard.IsNotNullAndReturn(_logger).LogError(ex, "{@Name}: Import Uniformity Configuration", nameof(GenerateAODWaveformParamUserControl));
            Guard.IsNotNullAndReturn(_dialogWindowProvider).ShowDialog($"""
                                                                        Import Uniformity Configuration Failed!
                                                                        {ex.Message}
                                                                        """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    });

    [RelayCommand]
    private void AddUniformityConfiguration(GenerateAODWaveformElectrodeConfiguration generateAODWaveformElectrodeConfiguration) => Invoke(_ =>
    {
        var configurationList = generateAODWaveformElectrodeConfiguration.UniformityConfigurations.ToList();
        configurationList.Add(new GenerateAODWaveformUniformityConfiguration());

        generateAODWaveformElectrodeConfiguration.UniformityConfigurations = configurationList;
    });

    [RelayCommand]
    private void RemoveUniformityConfiguration((GenerateAODWaveformElectrodeConfiguration GenerateAODWaveformElectrodeConfiguration, IEnumerable? SelectItems)? valueTuple) => Invoke(_ =>
    {
        if (valueTuple?.SelectItems is null) return;

        var configurationList = valueTuple.Value.GenerateAODWaveformElectrodeConfiguration.UniformityConfigurations.ToList();
        foreach (GenerateAODWaveformUniformityConfiguration selectItem in valueTuple.Value.SelectItems) configurationList.Remove(selectItem);

        valueTuple.Value.GenerateAODWaveformElectrodeConfiguration.UniformityConfigurations = configurationList;
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

        var param = Guard.IsAssignableToTypeAndReturn<AbstractGenerateAODWaveformParam>(DataContext);

        action.Invoke(param);
    }
}

public sealed class RemoveUniformityConfigurationConvert : MarkupExtension, IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values is [GenerateAODWaveformElectrodeConfiguration generateAODWaveformElectrodeConfiguration, IEnumerable selectItems]
            ? (generateAODWaveformElectrodeConfiguration, selectItems)
            : ThrowHelper.ThrowNotSupportedException<object>();
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => ThrowHelper.ThrowNotSupportedException<object[]>();

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}