using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
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

        GenerateChirpAodWaveParamDto.AodWaveDirectory = directoryPath;
    }

    [RelayCommand]
    private void ChangeChirpAodWaveFrequencyCompensationsFilePath()
    {
        var dialog = dialogWindowProvider.TryShowSelectFilePathDialog(".xlsx", out var filePath);
        if (dialog == false) return;

        GenerateChirpAodWaveParamDto.FrequencyAmplitudesFilePath = filePath;
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
                    GenerateChirpAodWaveParamDto.BandWidth,
                    GenerateChirpAodWaveParamDto.CenterFrequency,
                    GenerateChirpAodWaveParamDto.SoundPackageLength,
                    GenerateChirpAodWaveParamDto.FunctionMonotonicTypeEnum,
                    GenerateChirpAodWaveParamDto.SampleRate,
                    GenerateChirpAodWaveParamDto.Amplitude,
                    GenerateChirpAodWaveParamDto.AodWaveDirectory,
                    zeroSampleCount: GenerateChirpAodWaveParamDto.ZeroSampleCount,
                    endpointSampleCount: GenerateChirpAodWaveParamDto.EndpointSampleCount,
                    offsetFrequency: GenerateChirpAodWaveParamDto.OffsetFrequency,
                    offsetFrequencyPeriodMultiple: GenerateChirpAodWaveParamDto.OffsetFrequencyPeriodMultiple,
                    sincCoefficient: GenerateChirpAodWaveParamDto.SincCoefficient,
                    astigmatismCompensationCoefficient: GenerateChirpAodWaveParamDto.AstigmatismCompensationCoefficient,
                    sphericalAberrationCompensationCoefficient: GenerateChirpAodWaveParamDto.SphericalAberrationCompensationCoefficient,
                    secondaryAstigmatismCompensationCoefficient: GenerateChirpAodWaveParamDto.SecondaryAstigmatismCompensationCoefficient,
                    comaCompensationCoefficient: GenerateChirpAodWaveParamDto.ComaCompensationCoefficient,
                    trefoilCompensationCoefficient: GenerateChirpAodWaveParamDto.TrefoilCompensationCoefficient,
                    quadrafoilCompensationCoefficient: GenerateChirpAodWaveParamDto.QuadrafoilCompensationCoefficient,
                    alphaOrder: GenerateChirpAodWaveParamDto.AlphaOrder,
                    alphaOrderCoefficient: GenerateChirpAodWaveParamDto.AlphaOrderCoefficient,
                    frequencyAmplitudes: GenerateChirpAodWaveParamDto.FrequencyAmplitudes,
                    generateRetryTimes: GenerateChirpAodWaveParamDto.GenerateRetryTimes);

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

        GeneratePrescanAodWaveParamDto.AodWaveDirectory = directoryPath;
    }

    [RelayCommand]
    private void ChangePrescanAodWaveFrequencyCompensationsFilePath()
    {
        var dialog = dialogWindowProvider.TryShowSelectFilePathDialog(".xlsx", out var filePath);
        if (dialog == false) return;

        GeneratePrescanAodWaveParamDto.FrequencyAmplitudesFilePath = filePath;
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
                    GeneratePrescanAodWaveParamDto.BandWidth,
                    GeneratePrescanAodWaveParamDto.CenterFrequency,
                    GeneratePrescanAodWaveParamDto.FlatnessTime,
                    GeneratePrescanAodWaveParamDto.FunctionMonotonicTypeEnum,
                    GeneratePrescanAodWaveParamDto.SampleRate,
                    GeneratePrescanAodWaveParamDto.Amplitude,
                    GeneratePrescanAodWaveParamDto.AodWaveDirectory,
                    zeroSampleCount: GeneratePrescanAodWaveParamDto.ZeroSampleCount,
                    endpointSampleCount: GeneratePrescanAodWaveParamDto.EndpointSampleCount,
                    offsetFrequency: GeneratePrescanAodWaveParamDto.OffsetFrequency,
                    offsetFrequencyPeriodMultiple: GeneratePrescanAodWaveParamDto.OffsetFrequencyPeriodMultiple,
                    sincCoefficient: GeneratePrescanAodWaveParamDto.SincCoefficient,
                    astigmatismCompensationCoefficient: GeneratePrescanAodWaveParamDto.AstigmatismCompensationCoefficient,
                    sphericalAberrationCompensationCoefficient: GeneratePrescanAodWaveParamDto.SphericalAberrationCompensationCoefficient,
                    secondaryAstigmatismCompensationCoefficient: GeneratePrescanAodWaveParamDto.SecondaryAstigmatismCompensationCoefficient,
                    comaCompensationCoefficient: GeneratePrescanAodWaveParamDto.ComaCompensationCoefficient,
                    trefoilCompensationCoefficient: GeneratePrescanAodWaveParamDto.TrefoilCompensationCoefficient,
                    quadrafoilCompensationCoefficient: GeneratePrescanAodWaveParamDto.QuadrafoilCompensationCoefficient,
                    alphaOrder: GeneratePrescanAodWaveParamDto.AlphaOrder,
                    alphaOrderCoefficient: GeneratePrescanAodWaveParamDto.AlphaOrderCoefficient,
                    frequencyAmplitudes: GeneratePrescanAodWaveParamDto.FrequencyAmplitudes,
                    generateRetryTimes: GeneratePrescanAodWaveParamDto.GenerateRetryTimes);

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