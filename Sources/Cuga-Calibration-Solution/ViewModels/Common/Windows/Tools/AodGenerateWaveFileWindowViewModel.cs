using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.DarkField;
using MiniExcelLibs;
using Net.Utilities.Algorithm.MathNet.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using Point = Net.Utilities.Models.Point;

namespace CugaCalibration.ViewModels.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(AodGenerateWaveFileWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AodGenerateWaveFileWindowViewModel(
    IDialogWindowProvider dialogWindowProvider,
    LaserViewModel laserViewModel) : ViewModelBase
{
    [ObservableProperty]
    private GenerateChirpAodWaveParamDto _generateChirpAodWaveParamDto = new();

    [ObservableProperty]
    private GeneratePrescanAodWaveParamDto _generatePrescanAodWaveParamDto = new();

    [ObservableProperty]
    private string _chirpAodWaveFilePath = string.Empty;

    [ObservableProperty]
    private string _prescanAodWaveFilePath = string.Empty;

    [ObservableProperty]
    private Point[] _aodWaveFlatnessLinearFrequencySignals = [];

    [ObservableProperty]
    private Point[] _aodWaveFlatnessNonLinearFrequencySignals = [];

    [ObservableProperty]
    private Point[] _aodWaveFlatnessTotalFrequencySignals = [];

    [ObservableProperty]
    private Point[] _aodWaveFlatnessLinearCompensationSignals = [];

    [ObservableProperty]
    private Point[] _aodWaveFlatnessAstigmatismCompensationSignals = [];

    [ObservableProperty]
    private Point[] _aodWaveFlatnessSphericalAberrationCompensationSignals = [];

    [ObservableProperty]
    private Point[] _aodWaveFlatnessSecondaryAstigmatismCompensationSignals = [];

    [ObservableProperty]
    private Point[] _aodWaveFlatnessComaCompensationSignals = [];

    [ObservableProperty]
    private Point[] _aodWaveFlatnessTrefoilCompensationSignals = [];

    [ObservableProperty]
    private Point[] _aodWaveFlatnessQuadrafoilCompensationSignals = [];

    [ObservableProperty]
    private Point[] _aodWaveFlatnessAlphaOrderCompensationSignals = [];

    [ObservableProperty]
    private Point[] _aodWaveFlatnessNonlinearCompensationSignals = [];

    [ObservableProperty]
    private Point[] _aodWaveFlatnessTotalCompensationSignals = [];

    [ObservableProperty]
    private Point[] _aodWaveSignals = [];

    [ObservableProperty]
    private Point[] _aodWaveSignalsFourier = [];

    [ObservableProperty]
    private Point[] _aodWaveSignalsSinc = [];

    #region Chirp Aod

    [RelayCommand]
    private void ChangeChirpAodWaveDirectory()
    {
        var dialog = dialogWindowProvider.TryShowSelectDirectoryPathDialog(out var directoryPath);
        if (dialog == false) return;

        GenerateChirpAodWaveParamDto.AodWaveDirectory = directoryPath;
    }

    [RelayCommand]
    private void ChangeFrequencyCompensationsFilePath()
    {
        var dialog = dialogWindowProvider.TryShowSelectFilePathDialog(".xlsx", out var filePath);
        if (dialog == false) return;

        GenerateChirpAodWaveParamDto.FrequencyCompensationsFilePath = filePath;
    }

    [RelayCommand]
    private async Task GenerateChirpAodWaveFileAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                ChirpAodWaveFilePath = string.Empty;
                Clear();

                Point[]? frequencyCompensations = null;
                if (string.IsNullOrWhiteSpace(GenerateChirpAodWaveParamDto.FrequencyCompensationsFilePath) == false)
                {
                    frequencyCompensations = [.. MiniExcel.Query<AodPowerUniformityItemDto>(GenerateChirpAodWaveParamDto.FrequencyCompensationsFilePath).Select(t => new Point(t.CenterFrequency, t.Coefficient))];
                }

                var (isSuccess,
                    aodWaveFilePath,
                    aodWaveFlatnessLinearFrequencySignals,
                    aodWaveFlatnessNonLinearFrequencySignals,
                    aodWaveFlatnessTotalFrequencySignals,
                    aodWaveFlatnessLinearCompensationSignals,
                    aodWaveFlatnessAstigmatismCompensationSignals,
                    aodWaveFlatnessSphericalAberrationCompensationSignals,
                    aodWaveFlatnessSecondaryAstigmatismCompensationSignals,
                    aodWaveFlatnessComaCompensationSignals,
                    aodWaveFlatnessTrefoilCompensationSignals,
                    aodWaveFlatnessQuadrafoilCompensationSignals,
                    aodWaveFlatnessAlphaOrderCompensationSignals,
                    aodWaveFlatnessNonlinearCompensationSignals,
                    aodWaveFlatnessTotalCompensationSignals,
                    aodWaveSignals,
                    aodWaveSignalsFourier,
                    aodWaveSignalsSinc,
                    exception) = AodWaveGenerator.GenerateChirpAodWaveFile(
                    GenerateChirpAodWaveParamDto.BandWidth,
                    GenerateChirpAodWaveParamDto.CenterFrequency,
                    GenerateChirpAodWaveParamDto.SoundPackageLength,
                    GenerateChirpAodWaveParamDto.MonotonicTypeEnum,
                    GenerateChirpAodWaveParamDto.SampleRate,
                    GenerateChirpAodWaveParamDto.Amplitude,
                    GenerateChirpAodWaveParamDto.AodWaveDirectory,
                    zeroSampleCount: GenerateChirpAodWaveParamDto.ZeroSampleCount,
                    endpointSampleCount: GenerateChirpAodWaveParamDto.EndpointSampleCount,
                    sincCoefficient: GenerateChirpAodWaveParamDto.SincCoefficient,
                    astigmatismCompensationCoefficient: GenerateChirpAodWaveParamDto.AstigmatismCompensationCoefficient,
                    sphericalAberrationCompensationCoefficient: GenerateChirpAodWaveParamDto.SphericalAberrationCompensationCoefficient,
                    secondaryAstigmatismCompensationCoefficient: GenerateChirpAodWaveParamDto.SecondaryAstigmatismCompensationCoefficient,
                    comaCompensationCoefficient: GenerateChirpAodWaveParamDto.ComaCompensationCoefficient,
                    trefoilCompensationCoefficient: GenerateChirpAodWaveParamDto.TrefoilCompensationCoefficient,
                    quadrafoilCompensationCoefficient: GenerateChirpAodWaveParamDto.QuadrafoilCompensationCoefficient,
                    alphaOrder: GenerateChirpAodWaveParamDto.AlphaOrder,
                    alphaOrderCoefficient: GenerateChirpAodWaveParamDto.AlphaOrderCoefficient,
                    frequencyCompensations: frequencyCompensations,
                    generateRetryTimes: GenerateChirpAodWaveParamDto.GenerateRetryTimes);

                ChirpAodWaveFilePath = aodWaveFilePath;
                AodWaveFlatnessLinearFrequencySignals = aodWaveFlatnessLinearFrequencySignals;
                AodWaveFlatnessNonLinearFrequencySignals = aodWaveFlatnessNonLinearFrequencySignals;
                AodWaveFlatnessTotalFrequencySignals = aodWaveFlatnessTotalFrequencySignals;
                AodWaveFlatnessLinearCompensationSignals = aodWaveFlatnessLinearCompensationSignals;
                AodWaveFlatnessAstigmatismCompensationSignals = aodWaveFlatnessAstigmatismCompensationSignals;
                AodWaveFlatnessSphericalAberrationCompensationSignals = aodWaveFlatnessSphericalAberrationCompensationSignals;
                AodWaveFlatnessSecondaryAstigmatismCompensationSignals = aodWaveFlatnessSecondaryAstigmatismCompensationSignals;
                AodWaveFlatnessComaCompensationSignals = aodWaveFlatnessComaCompensationSignals;
                AodWaveFlatnessTrefoilCompensationSignals = aodWaveFlatnessTrefoilCompensationSignals;
                AodWaveFlatnessQuadrafoilCompensationSignals = aodWaveFlatnessQuadrafoilCompensationSignals;
                AodWaveFlatnessAlphaOrderCompensationSignals = aodWaveFlatnessAlphaOrderCompensationSignals;
                AodWaveFlatnessNonlinearCompensationSignals = aodWaveFlatnessNonlinearCompensationSignals;
                AodWaveFlatnessTotalCompensationSignals = aodWaveFlatnessTotalCompensationSignals;
                AodWaveSignals = aodWaveSignals;
                AodWaveSignalsFourier = aodWaveSignalsFourier;
                AodWaveSignalsSinc = aodWaveSignalsSinc;

                dialogWindowProvider.ShowDialog(isSuccess
                    ? "Generate Chirp Aod Wave File Success!"
                    : $"""
                       Generate Chirp Aod Wave File Success!
                       """, DialogButtonsEnum.OK, isSuccess ? DialogIconEnum.Information : DialogIconEnum.Warning);
            }
            catch (Exception ex)
            {
                dialogWindowProvider.ShowDialog($"""
                                                 Generate Chirp Aod Wave File Failed!
                                                 {ex}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task SendChirpAodWaveFileAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ChirpAodWaveFilePath))
                {
                    dialogWindowProvider.ShowDialog("Chirp Aod Wave File Name Is Empty");
                    return;
                }

                var chirpAod = laserViewModel.ReadChirpAodByConfigFile(ChirpAodWaveFilePath);
                laserViewModel.SendChirpAodByList(chirpAod);

                dialogWindowProvider.ShowDialog("Send Chirp Aod Wave File Success!");
            }
            catch (Exception ex)
            {
                dialogWindowProvider.ShowDialog($"""
                                                 Send Chirp Aod Wave File Failed!
                                                 {ex}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        }).ConfigureAwait(false);
    }

    #endregion Chirp Aod

    #region Prescan Aod

    [RelayCommand]
    private void ChangePrescanAodWaveDirectory()
    {
        var dialog = dialogWindowProvider.TryShowSelectDirectoryPathDialog(out var directoryPath);
        if (dialog == false) return;

        GeneratePrescanAodWaveParamDto.AodWaveDirectory = directoryPath;
    }

    [RelayCommand]
    private async Task GeneratePrescanAodWaveFileAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                PrescanAodWaveFilePath = string.Empty;
                Clear();

                var (isSuccess,
                    aodWaveFilePath,
                    aodWaveFlatnessLinearFrequencySignals,
                    aodWaveFlatnessNonLinearFrequencySignals,
                    aodWaveFlatnessTotalFrequencySignals,
                    aodWaveFlatnessLinearCompensationSignals,
                    aodWaveFlatnessAstigmatismCompensationSignals,
                    aodWaveFlatnessSphericalAberrationCompensationSignals,
                    aodWaveFlatnessSecondaryAstigmatismCompensationSignals,
                    aodWaveFlatnessComaCompensationSignals,
                    aodWaveFlatnessTrefoilCompensationSignals,
                    aodWaveFlatnessQuadrafoilCompensationSignals,
                    aodWaveFlatnessAlphaOrderCompensationSignals,
                    aodWaveFlatnessNonlinearCompensationSignals,
                    aodWaveFlatnessTotalCompensationSignals,
                    aodWaveSignals,
                    aodWaveSignalsFourier,
                    aodWaveSignalsSinc,
                    exception) = AodWaveGenerator.GeneratePrescanAodWaveFile(
                    GeneratePrescanAodWaveParamDto.BandWidth,
                    GeneratePrescanAodWaveParamDto.CenterFrequency,
                    GeneratePrescanAodWaveParamDto.FlatnessTime,
                    GeneratePrescanAodWaveParamDto.MonotonicTypeEnum,
                    GeneratePrescanAodWaveParamDto.SampleRate,
                    GeneratePrescanAodWaveParamDto.Amplitude,
                    GeneratePrescanAodWaveParamDto.AodWaveDirectory,
                    zeroSampleCount: GeneratePrescanAodWaveParamDto.ZeroSampleCount,
                    endpointSampleCount: GeneratePrescanAodWaveParamDto.EndpointSampleCount,
                    sincCoefficient: GeneratePrescanAodWaveParamDto.SincCoefficient,
                    astigmatismCompensationCoefficient: GeneratePrescanAodWaveParamDto.AstigmatismCompensationCoefficient,
                    sphericalAberrationCompensationCoefficient: GeneratePrescanAodWaveParamDto.SphericalAberrationCompensationCoefficient,
                    secondaryAstigmatismCompensationCoefficient: GeneratePrescanAodWaveParamDto.SecondaryAstigmatismCompensationCoefficient,
                    comaCompensationCoefficient: GeneratePrescanAodWaveParamDto.ComaCompensationCoefficient,
                    trefoilCompensationCoefficient: GeneratePrescanAodWaveParamDto.TrefoilCompensationCoefficient,
                    quadrafoilCompensationCoefficient: GeneratePrescanAodWaveParamDto.QuadrafoilCompensationCoefficient,
                    alphaOrder: GeneratePrescanAodWaveParamDto.AlphaOrder,
                    alphaOrderCoefficient: GeneratePrescanAodWaveParamDto.AlphaOrderCoefficient,
                    generateRetryTimes: GeneratePrescanAodWaveParamDto.GenerateRetryTimes);

                PrescanAodWaveFilePath = aodWaveFilePath;
                AodWaveFlatnessLinearFrequencySignals = aodWaveFlatnessLinearFrequencySignals;
                AodWaveFlatnessNonLinearFrequencySignals = aodWaveFlatnessNonLinearFrequencySignals;
                AodWaveFlatnessTotalFrequencySignals = aodWaveFlatnessTotalFrequencySignals;
                AodWaveFlatnessLinearCompensationSignals = aodWaveFlatnessLinearCompensationSignals;
                AodWaveFlatnessAstigmatismCompensationSignals = aodWaveFlatnessAstigmatismCompensationSignals;
                AodWaveFlatnessSphericalAberrationCompensationSignals = aodWaveFlatnessSphericalAberrationCompensationSignals;
                AodWaveFlatnessSecondaryAstigmatismCompensationSignals = aodWaveFlatnessSecondaryAstigmatismCompensationSignals;
                AodWaveFlatnessComaCompensationSignals = aodWaveFlatnessComaCompensationSignals;
                AodWaveFlatnessTrefoilCompensationSignals = aodWaveFlatnessTrefoilCompensationSignals;
                AodWaveFlatnessQuadrafoilCompensationSignals = aodWaveFlatnessQuadrafoilCompensationSignals;
                AodWaveFlatnessAlphaOrderCompensationSignals = aodWaveFlatnessAlphaOrderCompensationSignals;
                AodWaveFlatnessNonlinearCompensationSignals = aodWaveFlatnessNonlinearCompensationSignals;
                AodWaveFlatnessTotalCompensationSignals = aodWaveFlatnessTotalCompensationSignals;
                AodWaveSignals = aodWaveSignals;
                AodWaveSignalsFourier = aodWaveSignalsFourier;
                AodWaveSignalsSinc = aodWaveSignalsSinc;

                dialogWindowProvider.ShowDialog(isSuccess
                    ? "Generate Prescan Aod Wave File Success!"
                    : $"""
                       Generate Prescan Aod Wave File Success!
                       """, DialogButtonsEnum.OK, isSuccess ? DialogIconEnum.Information : DialogIconEnum.Warning);
            }
            catch (Exception ex)
            {
                dialogWindowProvider.ShowDialog($"""
                                                 Generate Prescan Aod Wave File Failed!
                                                 {ex}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task SendPrescanAodWaveFileAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(PrescanAodWaveFilePath))
                {
                    dialogWindowProvider.ShowDialog("Prescan Aod Wave File Is Empty");
                    return;
                }

                var prescanDto = laserViewModel.ReadPrescanByFile(PrescanAodWaveFilePath, 1d);
                laserViewModel.SendPrescanByList(prescanDto);

                dialogWindowProvider.ShowDialog("Send Prescan Aod Wave File Success!");
            }
            catch (Exception ex)
            {
                dialogWindowProvider.ShowDialog($"""
                                                 Send Prescan Aod Wave File Failed!
                                                 {ex}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        }).ConfigureAwait(false);
    }

    #endregion Prescan Aod

    private void Clear()
    {
        AodWaveFlatnessLinearFrequencySignals = [];
        AodWaveFlatnessNonLinearFrequencySignals = [];
        AodWaveFlatnessTotalFrequencySignals = [];
        AodWaveFlatnessLinearCompensationSignals = [];
        AodWaveFlatnessAstigmatismCompensationSignals = [];
        AodWaveFlatnessSphericalAberrationCompensationSignals = [];
        AodWaveFlatnessSecondaryAstigmatismCompensationSignals = [];
        AodWaveFlatnessComaCompensationSignals = [];
        AodWaveFlatnessTrefoilCompensationSignals = [];
        AodWaveFlatnessQuadrafoilCompensationSignals = [];
        AodWaveFlatnessNonlinearCompensationSignals = [];
        AodWaveFlatnessTotalCompensationSignals = [];
        AodWaveSignals = [];
        AodWaveSignalsFourier = [];
        AodWaveSignalsSinc = [];
    }

    [RelayCommand]
    private void Close() => CloseView(true);
}