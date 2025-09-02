using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.DarkField;
using Core.Utilities;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using Point = Net.Utilities.Models.Geometries.Point;

namespace CugaCalibration.ViewModels.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(AodGenerateWaveFileWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AodGenerateWaveFileWindowViewModel(
    IDialogWindowProvider dialogWindowProvider,
    LaserViewModel laserViewModel,
    ILogger<AodGenerateWaveFileWindowViewModel> logger) : ViewModelBase
{
    [ObservableProperty]
    private GenerateChirpAODWaveformParam _generateChirpAODWaveformParam = new();

    [ObservableProperty]
    private GeneratePrescanAODWaveformParam _generatePrescanAODWaveformParam = new();

    [ObservableProperty]
    private string _chirpAodWaveFilePath = string.Empty;

    [ObservableProperty]
    private string _prescanAodWaveFilePath = string.Empty;

    [ObservableProperty]
    private Point[] _aodWaveFlatnessLinearFrequencySignals = [];

    [ObservableProperty]
    private Point[] _aodWaveFlatnessTotalFrequencySignals = [];

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
    private Point[] _aodWaveSignals = [];

    [ObservableProperty]
    private Point[] _aodWaveSignalsFourier = [];

    [ObservableProperty]
    private Point[] _aodWaveFrequencyAmplitudes = [];

    #region Chirp Aod

    [RelayCommand]
    private void ChangeChirpAodWaveDirectory()
    {
        var dialog = dialogWindowProvider.TryShowSelectDirectoryPathDialog(out var directoryPath);
        if (dialog == false) return;

        GenerateChirpAODWaveformParam.AodWaveDirectory = directoryPath;
    }

    [RelayCommand]
    private void ChangeChirpAodWaveFrequencyCompensationsFilePath()
    {
        var dialog = dialogWindowProvider.TryShowSelectFilePathDialog(".xlsx", out var filePath);
        if (dialog == false) return;

        GenerateChirpAODWaveformParam.FrequencyAmplitudesFilePath = filePath;
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

                var (isSuccess,
                    aodWaveFilePath,
                    aodWaveFlatnessLinearFrequencySignals,
                    aodWaveFlatnessTotalFrequencySignals,
                    aodWaveFlatnessAstigmatismCompensationSignals,
                    aodWaveFlatnessSphericalAberrationCompensationSignals,
                    aodWaveFlatnessSecondaryAstigmatismCompensationSignals,
                    aodWaveFlatnessComaCompensationSignals,
                    aodWaveFlatnessTrefoilCompensationSignals,
                    aodWaveFlatnessQuadrafoilCompensationSignals,
                    aodWaveFlatnessAlphaOrderCompensationSignals,
                    aodWaveSignals,
                    aodWaveSignalsFourier,
                    aodWaveFrequencyAmplitudes,
                    exception) = AODWaveformGenerator.GenerateChirpAodWaveFile(
                    GenerateChirpAODWaveformParam.BandWidth,
                    GenerateChirpAODWaveformParam.CenterFrequency,
                    GenerateChirpAODWaveformParam.SoundPackageLength,
                    GenerateChirpAODWaveformParam.FunctionMonotonicTypeEnum,
                    GenerateChirpAODWaveformParam.SampleRate,
                    GenerateChirpAODWaveformParam.Amplitude,
                    GenerateChirpAODWaveformParam.AodWaveDirectory,
                    zeroSampleCount: GenerateChirpAODWaveformParam.ZeroSampleCount,
                    endpointSampleCount: GenerateChirpAODWaveformParam.EndpointSampleCount,
                    offsetFrequency: GenerateChirpAODWaveformParam.OffsetFrequency,
                    offsetFrequencyPeriodMultiple: GenerateChirpAODWaveformParam.OffsetFrequencyPeriodMultiple,
                    sincCoefficient: GenerateChirpAODWaveformParam.SincCoefficient,
                    astigmatismCompensationCoefficient: GenerateChirpAODWaveformParam.AstigmatismCompensationCoefficient,
                    sphericalAberrationCompensationCoefficient: GenerateChirpAODWaveformParam.SphericalAberrationCompensationCoefficient,
                    secondaryAstigmatismCompensationCoefficient: GenerateChirpAODWaveformParam.SecondaryAstigmatismCompensationCoefficient,
                    comaCompensationCoefficient: GenerateChirpAODWaveformParam.ComaCompensationCoefficient,
                    trefoilCompensationCoefficient: GenerateChirpAODWaveformParam.TrefoilCompensationCoefficient,
                    quadrafoilCompensationCoefficient: GenerateChirpAODWaveformParam.QuadrafoilCompensationCoefficient,
                    alphaOrder: GenerateChirpAODWaveformParam.AlphaOrder,
                    alphaOrderCoefficient: GenerateChirpAODWaveformParam.AlphaOrderCoefficient,
                    frequencyAmplitudes: GenerateChirpAODWaveformParam.FrequencyAmplitudes,
                    generateRetryTimes: GenerateChirpAODWaveformParam.GenerateRetryTimes);

                ChirpAodWaveFilePath = aodWaveFilePath;
                AodWaveFlatnessLinearFrequencySignals = aodWaveFlatnessLinearFrequencySignals;
                AodWaveFlatnessTotalFrequencySignals = aodWaveFlatnessTotalFrequencySignals;
                AodWaveFlatnessAstigmatismCompensationSignals = aodWaveFlatnessAstigmatismCompensationSignals;
                AodWaveFlatnessSphericalAberrationCompensationSignals = aodWaveFlatnessSphericalAberrationCompensationSignals;
                AodWaveFlatnessSecondaryAstigmatismCompensationSignals = aodWaveFlatnessSecondaryAstigmatismCompensationSignals;
                AodWaveFlatnessComaCompensationSignals = aodWaveFlatnessComaCompensationSignals;
                AodWaveFlatnessTrefoilCompensationSignals = aodWaveFlatnessTrefoilCompensationSignals;
                AodWaveFlatnessQuadrafoilCompensationSignals = aodWaveFlatnessQuadrafoilCompensationSignals;
                AodWaveFlatnessAlphaOrderCompensationSignals = aodWaveFlatnessAlphaOrderCompensationSignals;
                AodWaveSignals = aodWaveSignals;
                AodWaveSignalsFourier = aodWaveSignalsFourier;
                AodWaveFrequencyAmplitudes = aodWaveFrequencyAmplitudes;

                if (isSuccess) dialogWindowProvider.ShowDialog("Generate Chirp Aod Wave File Success!");
                else throw GuardUtils.IsNotNullAndReturn(exception);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Generate Chirp Aod Wave File Failed!");
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

                laserViewModel.SetChirpAODWaveProfileList([AODWaveformProfileFactory.CreateChirp(OpticsAODElectrodeEnum.Electrode1, ChirpAodWaveFilePath)]);

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

        GeneratePrescanAODWaveformParam.AodWaveDirectory = directoryPath;
    }

    [RelayCommand]
    private void ChangePrescanAodWaveFrequencyCompensationsFilePath()
    {
        var dialog = dialogWindowProvider.TryShowSelectFilePathDialog(".xlsx", out var filePath);
        if (dialog == false) return;

        GeneratePrescanAODWaveformParam.FrequencyAmplitudesFilePath = filePath;
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
                    aodWaveFlatnessTotalFrequencySignals,
                    aodWaveFlatnessAstigmatismCompensationSignals,
                    aodWaveFlatnessSphericalAberrationCompensationSignals,
                    aodWaveFlatnessSecondaryAstigmatismCompensationSignals,
                    aodWaveFlatnessComaCompensationSignals,
                    aodWaveFlatnessTrefoilCompensationSignals,
                    aodWaveFlatnessQuadrafoilCompensationSignals,
                    aodWaveFlatnessAlphaOrderCompensationSignals,
                    aodWaveSignals,
                    aodWaveSignalsFourier,
                    aodWaveFrequencyAmplitudes,
                    exception) = AODWaveformGenerator.GeneratePrescanAodWaveFile(
                    GeneratePrescanAODWaveformParam.BandWidth,
                    GeneratePrescanAODWaveformParam.CenterFrequency,
                    GeneratePrescanAODWaveformParam.FlatnessTime,
                    GeneratePrescanAODWaveformParam.FunctionMonotonicTypeEnum,
                    GeneratePrescanAODWaveformParam.SampleRate,
                    GeneratePrescanAODWaveformParam.Amplitude,
                    GeneratePrescanAODWaveformParam.AodWaveDirectory,
                    zeroSampleCount: GeneratePrescanAODWaveformParam.ZeroSampleCount,
                    endpointSampleCount: GeneratePrescanAODWaveformParam.EndpointSampleCount,
                    offsetFrequency: GeneratePrescanAODWaveformParam.OffsetFrequency,
                    offsetFrequencyPeriodMultiple: GeneratePrescanAODWaveformParam.OffsetFrequencyPeriodMultiple,
                    sincCoefficient: GeneratePrescanAODWaveformParam.SincCoefficient,
                    astigmatismCompensationCoefficient: GeneratePrescanAODWaveformParam.AstigmatismCompensationCoefficient,
                    sphericalAberrationCompensationCoefficient: GeneratePrescanAODWaveformParam.SphericalAberrationCompensationCoefficient,
                    secondaryAstigmatismCompensationCoefficient: GeneratePrescanAODWaveformParam.SecondaryAstigmatismCompensationCoefficient,
                    comaCompensationCoefficient: GeneratePrescanAODWaveformParam.ComaCompensationCoefficient,
                    trefoilCompensationCoefficient: GeneratePrescanAODWaveformParam.TrefoilCompensationCoefficient,
                    quadrafoilCompensationCoefficient: GeneratePrescanAODWaveformParam.QuadrafoilCompensationCoefficient,
                    alphaOrder: GeneratePrescanAODWaveformParam.AlphaOrder,
                    alphaOrderCoefficient: GeneratePrescanAODWaveformParam.AlphaOrderCoefficient,
                    frequencyAmplitudes: GeneratePrescanAODWaveformParam.FrequencyAmplitudes,
                    generateRetryTimes: GeneratePrescanAODWaveformParam.GenerateRetryTimes);

                PrescanAodWaveFilePath = aodWaveFilePath;
                AodWaveFlatnessLinearFrequencySignals = aodWaveFlatnessLinearFrequencySignals;
                AodWaveFlatnessTotalFrequencySignals = aodWaveFlatnessTotalFrequencySignals;
                AodWaveFlatnessAstigmatismCompensationSignals = aodWaveFlatnessAstigmatismCompensationSignals;
                AodWaveFlatnessSphericalAberrationCompensationSignals = aodWaveFlatnessSphericalAberrationCompensationSignals;
                AodWaveFlatnessSecondaryAstigmatismCompensationSignals = aodWaveFlatnessSecondaryAstigmatismCompensationSignals;
                AodWaveFlatnessComaCompensationSignals = aodWaveFlatnessComaCompensationSignals;
                AodWaveFlatnessTrefoilCompensationSignals = aodWaveFlatnessTrefoilCompensationSignals;
                AodWaveFlatnessQuadrafoilCompensationSignals = aodWaveFlatnessQuadrafoilCompensationSignals;
                AodWaveFlatnessAlphaOrderCompensationSignals = aodWaveFlatnessAlphaOrderCompensationSignals;
                AodWaveSignals = aodWaveSignals;
                AodWaveSignalsFourier = aodWaveSignalsFourier;
                AodWaveFrequencyAmplitudes = aodWaveFrequencyAmplitudes;

                if (isSuccess) dialogWindowProvider.ShowDialog("Generate Prescan Aod Wave File Success!");
                else throw GuardUtils.IsNotNullAndReturn(exception);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Generate Prescan Aod Wave File Failed!");
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

                laserViewModel.SetPrescanAODWaveProfileList([AODWaveformProfileFactory.CreatePrescan(OpticsAODElectrodeEnum.Electrode1, PrescanAodWaveFilePath)]);

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
        AodWaveFlatnessTotalFrequencySignals = [];
        AodWaveFlatnessAstigmatismCompensationSignals = [];
        AodWaveFlatnessSphericalAberrationCompensationSignals = [];
        AodWaveFlatnessSecondaryAstigmatismCompensationSignals = [];
        AodWaveFlatnessComaCompensationSignals = [];
        AodWaveFlatnessTrefoilCompensationSignals = [];
        AodWaveFlatnessQuadrafoilCompensationSignals = [];
        AodWaveFlatnessAlphaOrderCompensationSignals = [];
        AodWaveSignals = [];
        AodWaveSignalsFourier = [];
        AodWaveFrequencyAmplitudes = [];
    }

    [RelayCommand]
    private void Close() => CloseView(true);
}