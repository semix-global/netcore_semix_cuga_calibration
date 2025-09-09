using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.AODWaveform.Generates;
using MiniExcelLibs;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models;
using Net.Utilities.WPF.Enums;

namespace Core.Models.Models.Common.AODWaveform.UI.Generates;

public sealed partial class GenerateChirpAODWaveformParamUserControl
{
    public GenerateChirpAODWaveformParamUserControl()
    {
        InitializeComponent();
    }

    [RelayCommand]
    private void ImportUniformityConfiguration() => Invoke(param =>
    {
        try
        {
            var dialog = GuardUtils.IsNotNullAndReturn(_dialogWindowProvider).TryShowSelectFilePathDialog(Extenstion, out var filePath);
            if (dialog == false) return;

            FileHelper.SerializeOperate(filePath);

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
}