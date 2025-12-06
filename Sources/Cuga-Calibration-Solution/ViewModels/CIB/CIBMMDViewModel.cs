using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.CIB.MMD;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.OpticalPowerMeter;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using System.IO;
using Core.Models.Models.Common.Pattern;
using Constants = Net.Utilities.Models.Constants;
using Generate = MathNet.Numerics.Generate;

namespace CugaCalibration.ViewModels.CIB;

[IOCAppService(ServiceType = typeof(CIBMMDViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBMMDViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => string.Join("_", Cache.CIBInformations);

    public override string CalibrateFileName => string.Join("_", Cache.CIBInformations);

    public string AODWaveformDirectoryPath => Path.Combine(AppHomeDirectory, "AODWaveform", GetType().Name, DirectoryHelper.RemoveInvalidDirectoryName(CalibrateDirectoryName), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select CIB Information" },
        new() { StepName = "Find Position" },
        new() { StepName = "MMD" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private IReadOnlyList<CIBMMDDto> _calibratingItems = [];

    [ObservableProperty]
    private IReadOnlyList<CIBMMDDto> _selectedCalibratingItems = [];

    [ObservableProperty]
    private IReadOnlyList<CIBInformationCalibrationStatus> _calibrationStatuses = [];

    #endregion Calibrate

    [ObservableProperty]
    private IReadOnlyList<CIBMMDDto> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<CIBMMDDto> _selectedReviewItems = [];

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private CIBMMDCache _cache = new();

    [ObservableProperty]
    private CIBMMDDto[] _calibrations = [];

    [ObservableProperty]
    private MicroscopeCalChipDto _microscopeCalChip = new();

    [ObservableProperty]
    private IReadOnlyList<LaserOpticalPowerMeterDto> _laserOpticalPowerMeters = [];

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (CalibrationStatusService.GetAdsCalibrationIsOKStatus() == false)
        {
            DialogWindowProvider.ShowDialog("The ADS precondition is Failure", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<MicroscopeFocusItemDto>(out _, out var errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<MicroscopeCalChipDto>(out var microscopeCalChip, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        MicroscopeCalChip = microscopeCalChip;

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<LaserAutoFocusDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<LaserBeamStabilizerObjDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserOpticalPowerMeterDto>(out var laserOpticalPowerItems, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        LaserOpticalPowerMeters = laserOpticalPowerItems;

        if (CalibrationStatuses.Count == 0)
            CalibrationStatuses =
            [
                .. ApplicationCookie.CIBInformations.Select(t => new CIBInformationCalibrationStatus { CIBInformation = t, IsCalibrated = false })
            ];

        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<CIBMMDCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<CIBMMDDto>();

        Calibrations =
        [
            ..Calibrations.Where(t => ApplicationCookie.CIBInformations.Contains(t.CIBInformation))
                .Select(t =>
                {
                    CalibrationStatuses.Single(tt => tt.CIBInformation == t.CIBInformation).IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        if (isHasCache == false) CacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Reviews =
        [
            .. Calibrations
                .Select(t => t.Clone())
                .OrderBy(t => t.CIBInformation)
        ];

        return Reviews.Any(t => t.IsCalibrated);
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                CalibratingItems = [];

                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.FindBFMachinePosition != Point.Origin
                    ? Cache.FindBFMachinePosition
                    : MicroscopeCalChip.HazeBrightFieldMachinePosition));

                return true;

            case 1:
                return true;

            case 2:
                foreach (var cacheCIBInformation in Cache.CIBInformations) CalibrationStatuses.Single(t => t.CIBInformation == cacheCIBInformation).IsCalibrated = true;

                DialogWindowProvider.ShowDialog($"AOD Alignment {CalibrateDirectoryName} Ok!");

                IsCalibrated = CalibrationStatuses.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                return true;

            default:
                return false;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand]
    private void ImportGainConfiguration()
    {
        try
        {
            var dialog = DialogWindowProvider.TryShowSelectFilePathDialog(".xlsx", out var filePath);
            if (dialog == false) return;

            var values = MiniExcel.Query<CIBMMDCache.GainConfiguration>(filePath).ToArray();
            if (values.Length > 0) Cache.GainConfigurations = values;

            DialogWindowProvider.ShowDialog($"{nameof(ImportGainConfiguration)} OK!");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, nameof(ImportGainConfiguration));
            DialogWindowProvider.ShowDialog($"""
                                             {nameof(ImportGainConfiguration)} Failed!
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand]
    private void SetCIBMMD(CIBMMDDto cibMMDDto)
    {
        try
        {
            if (cibMMDDto.IsOk == false)
            {
                DialogWindowProvider.ShowDialog($"{nameof(SetCIBMMD)} Is OK Failed!");

                return;
            }

            LaserViewModel.SetCIBMMD(cibMMDDto.CIBInformation, [.. cibMMDDto.LogGainMul128U12BitPoints.Select(t => t.Y)], [.. cibMMDDto.GainS16BitPoints.Select(t => t.Y)]);

            DialogWindowProvider.ShowDialog($"{nameof(SetCIBMMD)} {cibMMDDto.CIBInformation} OK!");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, nameof(SetCIBMMD));
            DialogWindowProvider.ShowDialog($"""
                                             {nameof(SetCIBMMD)} Failed!
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }


    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Guard.IsNotEmpty(Cache.CIBInformations);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.CIBInformations
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Guard.IsNotEmpty(Cache.CIBInformations);

            StageViewModel.SetAbsoluteStageTheta(0);
            Cache.FindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.CIBInformations,
                Cache.FindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            Guard.IsNotEmpty(Cache.CIBInformations);
            Cache.GeneratePrescanAODWaveformParam.WithFrequencyFlatness(Cache.PrescanFrequency);
            Cache.GenerateChirpAODWaveformParam.WithFrequencyFlatness(Cache.ChirpFrequency);

            CalibratingItems = [];
            Cache.PrescanAODWaveformResultFilePath = Cache.ChirpAODWaveformResultFilePath = string.Empty;
            Cache.PrescanAODWaveformProfiles = [];
            Cache.ChirpAODWaveformProfiles = [];
            Cache.MeasurePowerPoints = Cache.FitMeasurePowerPoints = Cache.NotUseODFilterMeasurePowerPoints = Cache.UseODFilterMeasurePowerPoints = [];
            Cache.P0 = Cache.P1 = Cache.P2 = Cache.P3 = Cache.RSquared = Cache.ODFilterRatio = 0;

            var laserOpticalPowerMeter = LaserOpticalPowerMeters.Single(t => t.ProductivityInformation.OpticsMagType == Cache.ProductivityInformation.OpticsMagType && t.IsOk);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                laserOpticalPowerMeter.MeasureMaxPowerPosition,
                Cache.CIBInformations,
                Cache.FindBFMachinePosition,
                Cache.OpticsIlluminationModeEnum,
                Cache.ProductivityInformation,
                Cache.AFOffsetMotor,
                Cache.AFECS,
                Cache.IsAFEnable,
                GeneratePrescanAODWaveformParam = new HtmlQuote(Cache.GeneratePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
                GenerateChirpAODWaveformParam = new HtmlQuote(Cache.GenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
                Cache.CIBProfileMode,
                Cache.MeasurePowerWaitTime,
                Cache.PMTValueWaitTime,
                Cache.StartCoefficient,
                Cache.StepCoefficient,
                Cache.StopCoefficient,
                Cache.MeasurePowerSequenceCommonRatio,
                Cache.MeasurePowerNotUseODFilterMinValue,
                Cache.MMDMeasurePowerRangeRatio,
                Cache.StartGain,
                Cache.StepGain,
                Cache.StopGain,
                Cache.ProtectedPMTValue,
                Cache.ProtectedCount,
                Cache.CatchPMTValueCount,
                Cache.DarkCurrent,
                Cache.Denominator,
                Cache.ScaleFactor,
                Cache.MinValidFraction,
                Cache.MaxValidFraction,
                Cache.MinLogGain,
                Table = new HtmlTable([.. Cache.GainConfigurations])
            }), HtmlLogUniqueId.LoggingHtml());

            LaserViewModel.ToggleOpticsMagType(Cache.OpticsIlluminationModeEnum, Cache.ProductivityInformation);

            Cache.GeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
            Cache.GeneratePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
            var (prescanAODWaveformResult, exception) = AODWaveformGenerator.GeneratePrescanAODWaveform(Cache.GeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);
            if (prescanAODWaveformResult.IsSuccess == false) ThrowHelper.ThrowInvalidOperationException(string.Empty, GuardUtils.IsNotNullAndReturn(exception));
            Cache.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(prescanAODWaveformResult);
            Cache.PrescanAODWaveformResultFilePath = prescanAODWaveformResult.FilePath;

            Cache.GenerateChirpAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
            Cache.GenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
            (var chirpAODWaveformResult, exception) = AODWaveformGenerator.GenerateChirpAODWaveform(Cache.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);
            if (chirpAODWaveformResult.IsSuccess == false) ThrowHelper.ThrowInvalidOperationException(string.Empty, GuardUtils.IsNotNullAndReturn(exception));
            Cache.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(chirpAODWaveformResult);
            Cache.ChirpAODWaveformResultFilePath = chirpAODWaveformResult.FilePath;

            LaserViewModel.SetPrescanAODWaveProfiles(Cache.OpticsIlluminationModeEnum, [.. Cache.PrescanAODWaveformProfiles.Select(t => t.ApplyCoefficient(Cache.StartCoefficient))]);
            LaserViewModel.SetChirpAODWaveProfiles(Cache.OpticsIlluminationModeEnum, Cache.ChirpAODWaveformProfiles);

            LaserViewModel.ToggleProfileMode(Cache.CIBProfileMode);
            LaserViewModel.SetGain(Cache.StartGain);

            Logger.LogHtmlInformation("AOD Waveform", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                GeneratePrescanAODWaveformParam = new HtmlQuote(Cache.GeneratePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
                Cache.PrescanAODWaveformResultFilePath,
                PrescanAODWaveformProfiles = new HtmlTable([.. Cache.PrescanAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())]),
                GenerateChirpAODWaveformParam = new HtmlQuote(Cache.GenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
                Cache.ChirpAODWaveformResultFilePath,
                ChirpAODWaveformProfiles = new HtmlTable([.. Cache.ChirpAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())])
            }), HtmlLogUniqueId.LoggingHtml());

            // 获取功率
            var coefficients = Generate.LinearRange(Cache.StartCoefficient, Cache.StepCoefficient, Cache.StopCoefficient);
            Guard.IsNotEmpty(coefficients);
            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(laserOpticalPowerMeter.MeasureMaxPowerPosition);
            try
            {
                LaserViewModel.ToggleOpticsODFilter(false);
                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Close);
                await Task.Delay(TimeSpan.FromSeconds(Cache.MeasurePowerWaitTime), cancellationToken).ConfigureAwait(false);
                var measurePowerNoises = Enumerable.Range(0, HostEnvironment.IsDevelopment() ? 0 : 10000)
                    .Select(_ =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        return LaserViewModel.GetOpticalMeasurePower(Cache.ProductivityInformation, Cache.GeneratePrescanAODWaveformParam.FlatnessTime);
                    })
                    .ToArray();
                var measurePowerNoise = HostEnvironment.IsDevelopment() ? 0 : measurePowerNoises.Average();

                foreach (var coefficient in coefficients)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        LaserViewModel.SetPrescanAODWaveProfiles(Cache.OpticsIlluminationModeEnum, [.. Cache.PrescanAODWaveformProfiles.Select(t => t.ApplyCoefficient(coefficient))]);
                        LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);
                        await Task.Delay(TimeSpan.FromSeconds(Cache.MeasurePowerWaitTime), cancellationToken).ConfigureAwait(false);

                        var measurePower = LaserViewModel.GetOpticalMeasurePower(Cache.ProductivityInformation, Cache.GeneratePrescanAODWaveformParam.FlatnessTime);

                        Cache.MeasurePowerPoints = [.. Cache.MeasurePowerPoints, new Point(coefficient, measurePower - measurePowerNoise)];
                    }
                    finally
                    {
                        LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);
                    }
                }

                var maxMeasurePowerPoint = Cache.MeasurePowerPoints.MaxBy(t => t.Y);

                Cache.MeasurePowerPoints = [.. Cache.MeasurePowerPoints.Where(t => Cache.MeasurePowerNotUseODFilterMinValue <= t.Y && t.Y <= maxMeasurePowerPoint.Y)];
                try
                {
                    LaserViewModel.ToggleOpticsODFilter(true);
                    LaserViewModel.SetPrescanAODWaveProfiles(Cache.OpticsIlluminationModeEnum, [.. Cache.PrescanAODWaveformProfiles.Select(t => t.ApplyCoefficient(maxMeasurePowerPoint.X))]);
                    LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);
                    await Task.Delay(TimeSpan.FromSeconds(Cache.MeasurePowerWaitTime), cancellationToken).ConfigureAwait(false);

                    Cache.ODFilterRatio = maxMeasurePowerPoint.Y / LaserViewModel.GetOpticalMeasurePower(Cache.ProductivityInformation, Cache.GeneratePrescanAODWaveformParam.FlatnessTime);
                }
                finally
                {
                    LaserViewModel.ToggleOpticsODFilter(false);
                }
            }
            finally
            {
                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);
            }

            (Cache.P0, Cache.P1, Cache.P2, Cache.P3, Cache.RSquared, var yPredicted) = PolynomialLeastSquares.Polynomial3Fit(Vector<double>.Build.DenseOfEnumerable(Cache.MeasurePowerPoints.Select(t => t.X)), Vector<double>.Build.DenseOfEnumerable(Cache.MeasurePowerPoints.Select(t => t.Y)));
            Cache.FitMeasurePowerPoints = [.. Cache.MeasurePowerPoints.Index().Select(t => new Point(t.Item.X, yPredicted[t.Index]))];
            Cache.NotUseODFilterMeasurePowerPoints = GeometricSequence.Generate(Cache.MeasurePowerPoints.Max(t => t.Y), Cache.MeasurePowerSequenceCommonRatio, Cache.MeasurePowerNotUseODFilterMinValue)
                .OrderBy(t => t)
                .Select(t =>
                {
                    var solveForX = GeometricSequence.SolveForX(Cache.P0, Cache.P1, Cache.P2, Cache.P3, t);

                    return new Point(HostEnvironment.IsDevelopment() ? solveForX.FirstOrDefault(tt => tt > 0, Cache.StartCoefficient) : solveForX.Single(), t);
                })
                .ToArray();
            Cache.UseODFilterMeasurePowerPoints = GeometricSequence.Generate(Cache.MeasurePowerNotUseODFilterMinValue, Cache.MeasurePowerSequenceCommonRatio, Cache.MeasurePowerPoints.Max(t => t.Y) / Cache.MMDMeasurePowerRangeRatio)
                .Where(t => Cache.NotUseODFilterMeasurePowerPoints[0].Y >= t)
                .OrderBy(t => t)
                .Select(t => t * Cache.ODFilterRatio)
                .Select(t =>
                {
                    var solveForX = GeometricSequence.SolveForX(Cache.P0, Cache.P1, Cache.P2, Cache.P3, t);

                    return new Point(HostEnvironment.IsDevelopment() ? solveForX.FirstOrDefault(tt => tt > 0, Cache.StartCoefficient) : solveForX.Single(), t);
                })
                .ToArray();

            Logger.LogHtmlInformation("Measure Power", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                measurePowerPoints = Cache.ScatterPlotControl.GetHtmlPlot2DLinesChart(),
            }), HtmlLogUniqueId.LoggingHtml());

            // 获取gain
            var gains = Generate.LinearRange(Cache.StartGain, Cache.StepGain, Cache.StopGain);
            Guard.IsNotEmpty(gains);
            CalibratingItems =
            [
                ..Cache.CIBInformations
                    .Select(t => new CIBMMDDto
                    {
                        CIBInformation = t,
                        Items =
                        [
                            .. Cache.NotUseODFilterMeasurePowerPoints.Select(tt => new CIBMMDItemDto
                            {
                                Coefficient = tt.X,
                                MeasurePower = tt.Y,
                                Items = [..gains.Select(ttt => new CIBMMDItemDto.Item { Gain = ttt, PMTValue = double.NaN })]
                            }),
                            .. Cache.UseODFilterMeasurePowerPoints.Select(tt => new CIBMMDItemDto
                            {
                                Coefficient = tt.X,
                                MeasurePower = tt.Y / Cache.ODFilterRatio,
                                Items = [..gains.Select(ttt => new CIBMMDItemDto.Item { Gain = ttt, PMTValue = double.NaN })]
                            })
                        ]
                    })
                    .OrderBy(t => t.CIBInformation)
            ];

            StageViewModel.SetAbsoluteStageTheta(0);
            StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.FindBFMachinePosition));
            if (Cache.IsAFEnable)
            {
                AfViewModel.SetSensorDarkFieldCalChipStandardEcsValue(CalChipSiteModelEnum.HazeModel, Cache.AFECS);
                AfViewModel.SetDarkFieldAutoFocusMotorAbsoluteValue(Cache.AFOffsetMotor);
                AfViewModel.ToggleDarkFieldEnable(true);
            }
            else
            {
                AfViewModel.ToggleBrightFieldEnable(false);
                AfViewModel.SetSensorEcsValue(Cache.AFECS);
            }

            try
            {
                foreach (var (coefficientIndex, (coefficient, isUseODFilter)) in ((IReadOnlyList<(double Coefficient, bool IsUseODFilter)>)
                         [
                             ..Cache.NotUseODFilterMeasurePowerPoints.Select(t => (t.X, false)),
                             ..Cache.UseODFilterMeasurePowerPoints.Select(t => (t.X, true))
                         ]).Index())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        LaserViewModel.ToggleOpticsODFilter(isUseODFilter);
                        LaserViewModel.SetGain(Cache.StartGain);
                        LaserViewModel.SetPrescanAODWaveProfiles(Cache.OpticsIlluminationModeEnum, [.. Cache.PrescanAODWaveformProfiles.Select(t => t.ApplyCoefficient(coefficient))]);
                        LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);

                        foreach (var (gainIndex, gain) in gains.Index())
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            SelectedCalibratingItems = CalibratingItems;

                            var noProtectedCIBMMDDtos = (IReadOnlyList<CIBMMDDto>)[.. CalibratingItems.Where(t => t.Items[coefficientIndex].ProtectedCount < Cache.ProtectedCount /* 不超过保护次数 */)];
                            var cibInformations = (IReadOnlyList<CIBInformation>)[.. noProtectedCIBMMDDtos.Select(t => t.CIBInformation)];
                            LaserViewModel.SetGain(cibInformations, gain);

                            await Task.Delay(TimeSpan.FromSeconds(Cache.PMTValueWaitTime), cancellationToken).ConfigureAwait(false);

                            var cibPMTValues = await LaserViewModel.GetCIBPMTValuesAsync(
                                StageCoordinateSystemEnum.Dark,
                                StageViewModel.MachineToBrightFieldPosition(Cache.FindBFMachinePosition),
                                Cache.CatchPMTValueCount,
                                Cache.OpticsIlluminationModeEnum,
                                Cache.ProductivityInformation,
                                cibInformations,
                                Cache.IsAFEnable,
                                cancellationToken);

                            await Task.WhenAll(cibPMTValues.Index().Select(t => Task.Run(() =>
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                var (index, pmtValue) = t;

                                var cibMMDDto = noProtectedCIBMMDDtos[index];
                                try
                                {
                                    var item = cibMMDDto.Items[coefficientIndex];
                                    var itemItem = item.Items[gainIndex];

                                    if (pmtValue >= Cache.ProtectedPMTValue /* 超过保护值 */) item.ProtectedCount++;
                                    itemItem.PMTValue = pmtValue;

                                    if (item.ProtectedCount >= Cache.ProtectedCount /* 超过保护次数 */) LaserViewModel.SetGain([cibMMDDto.CIBInformation], Cache.StartGain);
                                }
                                finally
                                {
                                    cibMMDDto.RefreshPlot();
                                }
                            }, cancellationToken)));
                        }
                    }
                    finally
                    {
                        LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);
                    }
                }
            }
            finally
            {
                LaserViewModel.ToggleOpticsODFilter(false);
            }

            Logger.LogHtmlInformation("Details", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            await Task.WhenAll(CalibratingItems.Select(cibMMDDto => Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                Algorithm(cibMMDDto);
            }, cancellationToken)));

            Guard.IsTrue(Save(CalibratingItems, cancellationToken));

            return CalibratingItems.All(t => t.IsCalibrated);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task AlgorithmActionAsync(CancellationToken cancellationToken)
    {
        if (SelectedReviewItems.Count == 0) return;

        await InvokeVerifyAsync(async () =>
        {
            Logger.LogHtmlInformation("Algorithm Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.DarkCurrent,
                Cache.Denominator,
                Cache.ScaleFactor,
                Cache.MinValidFraction,
                Cache.MaxValidFraction,
                Cache.MinLogGain,
                Table = new HtmlTable([.. Cache.GainConfigurations])
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlInformation("Details", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            await Task.WhenAll(SelectedReviewItems.Select(cibMMDDto => Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                Algorithm(cibMMDDto);
            }, cancellationToken)));

            var result = SelectedReviewItems.All(t => t.IsCalibrated);

            DialogWindowProvider.ShowDialog($"Algorithm {(result ? "OK" : "Failed")}",
                DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        if (SelectedReviewItems.Count == 0)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(() =>
        {
            Logger.LogHtmlInformation("Details", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            foreach (var selectedReviewItem in SelectedReviewItems)
            {
                if (selectedReviewItem.IsCalibrated) selectedReviewItem.IsVerified = true;

                var htmlBullet = new HtmlBullet(new
                {
                    SuccessPlot = new HtmlContainer([.. selectedReviewItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
                });

                if (selectedReviewItem.IsVerified)
                    Logger.LogHtmlInformation($"OK: {selectedReviewItem.CIBInformation.ToString()}", HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                    Logger.LogHtmlError($"Error: {selectedReviewItem.CIBInformation.ToString()}", HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
            }

            Guard.IsTrue(Save(SelectedReviewItems, cancellationToken));

            var result = SelectedReviewItems.All(t => t.IsOk);

            DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}",
                DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        }).ConfigureAwait(false);
    }

    private void Algorithm(CIBMMDDto cibMMDDto)
    {
        var htmlList = new List<BaseHtmlElement>();
        var htmlContainer = new HtmlContainer(htmlList);
        var isSuccess = false;

        try
        {
            var gains = (IReadOnlyList<double>)[.. cibMMDDto.Items[0].Items.Select(t => t.Gain)];
            var gainConfigurations = (IReadOnlyList<CIBMMDCache.GainConfiguration>)
            [
                ..gains.Select(t => Cache.GainConfigurations.Single(tt => Math.Abs(tt.Gain - t) < 1e-3))
            ];

            var coefficientCount = cibMMDDto.Items.Count;
            var gainCount = gains.Count;

            // A * X = B (最小二乘法)
            var aMatrix = Matrix<double>.Build.Dense(coefficientCount * gainCount, coefficientCount + gainCount);
            for (var i = 0; i < coefficientCount; i++)
            {
                var startRow = gainCount * i;
                var endRow = gainCount * (i + 1);
                for (var row = startRow; row < endRow; row++)
                {
                    aMatrix[row, i] = 1d;
                }
            }

            for (var j = 0; j < gainCount; j++)
            {
                for (var k = 0; k < coefficientCount; k++)
                {
                    aMatrix[gainCount * k + j, j + coefficientCount] = 1d;
                }
            }

            var xMeasurePowerVector = Vector<double>.Build.DenseOfEnumerable(cibMMDDto.Items.Select(t => t.MeasurePower));
            var xLogMeasurePowerVector = xMeasurePowerVector.Map(t => Math.Log(t, 2));

            var currentMatrix = Matrix<double>.Build.Dense(gainCount, coefficientCount);
            for (var row = 0; row < gainCount; row++)
            {
                for (var col = 0; col < coefficientCount; col++)
                {
                    currentMatrix[row, col] = cibMMDDto.Items[col].Items[row].PMTValue;
                }
            }

            currentMatrix -= Cache.DarkCurrent;
            currentMatrix /= Cache.Denominator;
            currentMatrix *= Cache.ScaleFactor;

            var logCurrentMatrix = currentMatrix.Map(t => t <= Cache.MinValidFraction || Cache.MaxValidFraction <= t ? double.NaN : Math.Log(t, 2));
            var bLogCurrentVector = Vector<double>.Build.Dense(logCurrentMatrix.ToColumnMajorArray());

            /*htmlList.Add(new HtmlBullet(new
            {
                aMatrix = Environment.NewLine + aMatrix.ToMatrixString(aMatrix.RowCount, aMatrix.ColumnCount),
                xLogMeasurePowerVector = Environment.NewLine + xLogMeasurePowerVector.ToVectorString(xLogMeasurePowerVector.Count, 1),
                bLogCurrentVector = Environment.NewLine + bLogCurrentVector.ToVectorString(bLogCurrentVector.Count, 1),
            }));*/

            var validIndices = (IReadOnlyList<int>)
            [
                ..bLogCurrentVector
                    .Index()
                    .Where(t => double.IsNaN(t.Item) == false)
                    .Select(t => t.Index)
            ];

            var aValidMatrix = Matrix<double>.Build.Dense(validIndices.Count, aMatrix.ColumnCount);
            var bLogCurrentValidVector = Vector<double>.Build.Dense(validIndices.Count);
            for (var i = 0; i < validIndices.Count; i++)
            {
                var originalRow = validIndices[i];
                aValidMatrix.SetRow(i, aMatrix.Row(originalRow));
                bLogCurrentValidVector[i] = bLogCurrentVector[originalRow];
            }

            var aValidCoefficientSubMatrix = aValidMatrix.SubMatrix(0, validIndices.Count, 0, coefficientCount);
            var aValidGainSubMatrix = aValidMatrix.SubMatrix(0, validIndices.Count, coefficientCount, gainCount);

            var xLogGainVector = aValidGainSubMatrix.Solve(bLogCurrentValidVector - aValidCoefficientSubMatrix * xLogMeasurePowerVector);

            var xLogVector = Vector<double>.Build.Dense([.. xLogMeasurePowerVector, .. xLogGainVector]);
            var gainResidual = (aValidMatrix * xLogVector - bLogCurrentValidVector).L2Norm();
            var gainL2Norm = xLogGainVector.L2Norm();

            cibMMDDto.GainResidual = gainResidual;
            cibMMDDto.GainL2Norm = gainL2Norm;

            cibMMDDto.GainPoints =
            [
                ..xLogGainVector
                    .Map(t => Math.Pow(2, t))
                    .Enumerate()
                    .Index()
                    .Select(t => new Point(gains[t.Index], t.Item))
            ];
            cibMMDDto.OriginLogGainPoints =
            [
                ..xLogGainVector
                    .Enumerate()
                    .Index()
                    .Select(t => new Point(gains[t.Index], t.Item))
            ];

            var (a1, a2, x0, dx, rSquared, yPredicted) = Boltzmann.BoltzmannFit(Vector<double>.Build.DenseOfEnumerable(cibMMDDto.OriginLogGainPoints.Select(t => t.X)), Vector<double>.Build.DenseOfEnumerable(cibMMDDto.OriginLogGainPoints.Select(t => t.Y)));
            cibMMDDto.LogGainA1 = a1;
            cibMMDDto.LogGainA2 = a2;
            cibMMDDto.LogGainX0 = x0;
            cibMMDDto.LogGainDx = dx;
            cibMMDDto.LogGainRSquared = rSquared;
            cibMMDDto.FitLogGainPoints = [.. cibMMDDto.OriginLogGainPoints.Index().Select(t => new Point(t.Item.X, yPredicted[t.Index]))];
            var distance = Math.Abs(cibMMDDto.FitLogGainPoints.Min(t => t.Y) - Cache.MinLogGain);
            cibMMDDto.ResultLogGainPoints = [.. cibMMDDto.OriginLogGainPoints.Index().Select(t => new Point(t.Item.X, cibMMDDto.FitLogGainPoints[t.Index].Y - distance))];

            htmlList.Add(new HtmlBullet(new
            {
                /*aValidMatrix = Environment.NewLine + aValidMatrix.ToMatrixString(aValidMatrix.RowCount, aValidMatrix.ColumnCount),
                xLogVector = Environment.NewLine + xLogVector.ToVectorString(xLogVector.Count, 1),
                bLogCurrentValidVector = Environment.NewLine + bLogCurrentValidVector.ToVectorString(bLogCurrentValidVector.Count, 1),*/
                gainResidual,
                gainNorm = gainL2Norm,
                Plot = new HtmlContainer([.. cibMMDDto.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
            }));

            isSuccess = cibMMDDto.ResultLogGainPoints.All(t => t.Y is >= 0 and <= 14) && cibMMDDto.ResultLogGainPoints.Select(t => t.Y).IsIncreasing(true); // logGain 不能超过 14, 且严格递增
            if (isSuccess == false)
            {
                htmlList.Add(new HtmlComment("LogGain out of range[0, 14]"));

                return;
            }

            var results = cibMMDDto.ResultLogGainPoints
                .Index()
                .Select(t => (
                    Gain: t.Item.X,
                    LogGainMultiplication128: (int)Math.Round(t.Item.Y * 128 /* KLA写死128 */, MidpointRounding.AwayFromZero),
                    gainConfigurations[t.Index].SenseU14Bit,
                    gainConfigurations[t.Index].GainS16Bit
                ))
                .ToArray();

            var logGainMul128U12BitPoints = Enumerable.Range(0, (int)Math.Pow(2, 14)).Select(t => new Point(t, double.NaN)).ToArray();
            /*
             * logGainMul128U12BitPoints
             * 0 - results.SenseU14Bit[0] 的所有索引: 全部设置为 results.LogGainMultiplication128[0]
             * (results.SenseU14Bit[0] + 1) - results.SenseU14Bit[1] 的所有索引: 全部设置为 results.LogGainMultiplication128[1]
             * ...
             * (results.SenseU14Bit[^2] + 1) - results.SenseU14Bit[^1] 的所有索引: 全部设置为 results.LogGainMultiplication128[^1]
             * (results.SenseU14Bit[^1] + 1) - (logGainMul128U12BitPoints.Length - 1) 的所有索引: 全部设置为 results.LogGainMultiplication128[^1]
             */
            for (var i = 0; i < results.Length; i++)
            {
                var startIndex = i == 0 ? 0 : results[i - 1].SenseU14Bit + 1;
                var endIndex = i == results.Length - 1 ? logGainMul128U12BitPoints.Length - 1 : results[i].SenseU14Bit; // 最后一个合并
                var yValue = results[i].LogGainMultiplication128;

                for (var j = startIndex; j <= endIndex; j++) logGainMul128U12BitPoints[j] = new Point(j, yValue);
            }

            var gainS16BitPoints = Enumerable.Range(0, (int)Math.Pow(2, 12)).Select(t => new Point(t, double.NaN)).ToArray();
            /*
             * gainS16BitPoints
             * 0 - results.LogGainMultiplication128[0] 的所有索引: 全部设置为 results.GainS16Bit[0]
             * (results.LogGainMultiplication128[0] + 1) - results.LogGainMultiplication128[1] 的所有索引: 全部设置为 results.GainS16Bit[1]
             * ...
             * (results.LogGainMultiplication128[^2] + 1) - results.LogGainMultiplication128[^1] 的所有索引: 全部设置为 results.GainS16Bit[^1]
             * (results.LogGainMultiplication128[^1] + 1) - (gainS16BitPoints.Length - 1) 的所有索引: 全部设置为 results.GainS16Bit[^1]
             */
            for (var i = 0; i < results.Length; i++)
            {
                var startIndex = i == 0 ? 0 : results[i - 1].LogGainMultiplication128 + 1;
                var endIndex = i == results.Length - 1 ? gainS16BitPoints.Length - 1 : results[i].LogGainMultiplication128; // 最后一个合并
                double yValue = results[i].GainS16Bit;

                for (var j = startIndex; j <= endIndex; j++) gainS16BitPoints[j] = new Point(j, yValue);
            }

            cibMMDDto.LogGainMul128U12BitPoints = logGainMul128U12BitPoints;
            cibMMDDto.GainS16BitPoints = gainS16BitPoints;

            htmlList.Add(new HtmlBullet(new
            {
                SuccessPlot = new HtmlContainer([.. cibMMDDto.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
            }));

            isSuccess = true;
        }
        catch (Exception ex)
        {
            isSuccess = false;

            htmlList.Add(new HtmlQuote(new
            {
                Exception = ex
            }));
        }
        finally
        {
            cibMMDDto.IsCalibrated = isSuccess;
            cibMMDDto.IsVerified = false;
            if (isSuccess)
                Logger.LogHtmlInformation($"OK: {cibMMDDto.CIBInformation.ToString()}", HtmlHeaderLevelEnum.Header4, htmlContainer, HtmlLogUniqueId.LoggingHtml());
            else
                Logger.LogHtmlError($"Error: {cibMMDDto.CIBInformation.ToString()}", HtmlHeaderLevelEnum.Header4, htmlContainer, HtmlLogUniqueId.LoggingHtml());
        }
    }

    private bool Save(IReadOnlyList<CIBMMDDto> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                .. Calibrations.Where(t => t.CIBInformation != dto.CIBInformation),
                dto.Clone()
            ];
        }

        CacheProvider.SetArray(Calibrations, cancellationToken);
        CacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}