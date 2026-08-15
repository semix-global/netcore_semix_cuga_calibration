using System.Collections;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using MiniExcelLibs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeOffsetFrequencyPeriodParam : ObservableObject
{
    [ObservableProperty]
    public partial OpticsAODElectrodeEnum OpticsAODElectrodeEnum { get; set; }

    [ObservableProperty]
    public partial double BoardCardOffsetFrequencyPeriodCoefficient { get; set; }

    [ObservableProperty]
    public partial GenerateAODWaveformUniformityConfiguration[] UniformityConfigurations { get; set; } = [];

    partial void OnUniformityConfigurationsChanged(GenerateAODWaveformUniformityConfiguration[] oldValue, GenerateAODWaveformUniformityConfiguration[] newValue)
    {
        foreach (var item in oldValue) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        OnPropertyChanged(nameof(UniformityConfigurations));

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(UniformityConfigurations));
    }

    [RelayCommand]
    private void ImportUniformityConfiguration()
    {
        var dialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();

        try
        {
            var dialog = dialogWindowProvider.TryShowSelectFilePathDialog(".xlsx", out var filePath);
            if (dialog == false) return;

            UniformityConfigurations = [];

            var values = MiniExcel.Query<GenerateAODWaveformUniformityConfiguration>(filePath)
                .Where(t => t.Frequency > 0)
                .ToArray();
            if (values.Length <= 0)
            {
                values =
                [
                    .. MiniExcel.Query(filePath, useHeaderRow: true)
                        .Cast<IDictionary<string, object>>()
                        .Select(t => new GenerateAODWaveformUniformityConfiguration { Frequency = (double)t[nameof(Point.X)], Coefficient = (double)t[nameof(Point.Y)] })
                        .Where(t => t.Frequency > 0)
                ];
            }

            if (values.Length > 0)
            {
                UniformityConfigurations = values;
                dialogWindowProvider.ShowDialog("Import Uniformity Configuration OK!");
            }
            else dialogWindowProvider.ShowDialog("Import Uniformity Configuration Failed! No data found.", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"""
                                             Import Uniformity Configuration Failed!
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand]
    private void AddUniformityConfiguration() => UniformityConfigurations = [.. UniformityConfigurations, new GenerateAODWaveformUniformityConfiguration()];

    [RelayCommand]
    private void RemoveUniformityConfiguration(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var configurationList = UniformityConfigurations.ToList();

        foreach (GenerateAODWaveformUniformityConfiguration selectItem in selectItems) configurationList.Remove(selectItem);

        UniformityConfigurations = [.. configurationList];
    }

    public object ToHtmlAnonymous() => new
    {
        OpticsAODElectrodeEnum,
        BoardCardOffsetFrequencyPeriodCoefficient,
        UniformityConfigurations = new HtmlPlot2DLinesChart([(string.Empty, [.. UniformityConfigurations.Select(t => new Point(t.Frequency, t.Coefficient))])], string.Empty)
    };
}