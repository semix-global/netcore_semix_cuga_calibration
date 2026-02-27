using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.CIB.MMD;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.OpticalPowerMeter;
using Core.Models.Models.Microscope.CalChip;
using Core.Utilities;
using Core.Utilities.SourceGenerators.Attributes;
using Humanizer;
using Local.SQL.Cache.Providers.Extensions;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Algorithms.Modules.CurveFitting.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using System.IO;
using System.Text;
using Constants = Net.Utilities.Models.Constants;
using Generate = MathNet.Numerics.Generate;

namespace CugaCalibration.ViewModels.CIB;

/// <summary>
/// Mixed Mode Detection
/// </summary>
[IOCAppService(ServiceType = typeof(CIBMMDViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBMMDViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => string.Join("_", Cache.CIBInformations).Truncate(50);

    public override string CalibrateFileName => string.Join("_", Cache.CIBInformations).Truncate(50);

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select CIB Information" },
        new() { StepName = "Find Haze Position" },
        new() { StepName = "MMD" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private IReadOnlyList<CIBMMDDTO> _calibratings = [];

    [ObservableProperty]
    private IReadOnlyList<CIBMMDDTO> _selectedCalibratingItems = [];

    [ObservableProperty]
    private IReadOnlyList<CIBInformationStatus> _calibratingStatuses = [];

    #endregion Calibrate

    [ObservableProperty]
    private IReadOnlyList<CIBMMDDTO> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<CIBMMDDTO> _selectedReviewItems = [];

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private CIBMMDCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private CIBMMDDTO[] _calibrations = [];

    [ObservableProperty]
    private MicroscopeCalChipDTO _microscopeCalChip = new();

    [ObservableProperty]
    private IReadOnlyList<LaserOpticalPowerMeterDTO> _laserOpticalPowerMeters = [];

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false) return false;

        MicroscopeCalChip = CalibrationStatusService.GetCalibration<MicroscopeCalChipDTO>();
        LaserOpticalPowerMeters = CalibrationStatusService.GetCalibrations<LaserOpticalPowerMeterDTO>();

        if (CalibratingStatuses.Count == 0)
            CalibratingStatuses =
            [
                .. ApplicationCookie.CIBInformations.Select(t => new CIBInformationStatus { SelectedItem = t, IsCalibrated = false })
            ];

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<CIBMMDCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<CIBMMDDTO>();

        Calibrations =
        [
            ..Calibrations
                .Where(t => ApplicationCookie.CIBInformations.Contains(t.CIBInformation))
                .Select(t =>
                {
                    CalibratingStatuses.Single(tt => tt.SelectedItem == t.CIBInformation).IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

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
                .OrderBy(t => t.CIBInformation)
        ];

        return Reviews.Count > 0;
    }

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                return true;

            case 1:
                return true;

            case 2:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.HazeFindBFMachinePosition));

                return true;

            default:
                return false;
        }
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                Calibratings = [];

                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.HazeFindBFMachinePosition != Point.Origin
                    ? Cache.HazeFindBFMachinePosition
                    : GuardUtils.IsNotNullAndReturn(MicroscopeCalChip.HazeItem).BrightFieldMachinePosition));

                return true;

            case 1:
                return true;

            case 2:
                foreach (var cacheCIBInformation in Cache.CIBInformations) CalibratingStatuses.Single(t => t.SelectedItem == cacheCIBInformation).IsCalibrated = true;

                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

                IsCalibrated = CalibratingStatuses.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                return true;

            default:
                return false;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand]
    private void SetCIBMMD(CIBMMDDTO item)
    {
        try
        {
            /*if (item.IsOk == false)
            {
                DialogWindowProvider.ShowDialog($"{nameof(SetCIBMMD)} Is OK Failed!");

                return;
            }*/

            CIBViewModel.SetMMD(
                item.CIBInformation,
                [.. item.LogGainMul128U12BitPoints.Select(t => t.Y)],
                [.. item.GainS16BitPoints.Select(t => t.Y)]);

            DialogWindowProvider.ShowDialog($"{nameof(SetCIBMMD)} {item.CIBInformation} OK!");
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
    private Task Step0Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Guard.IsNotEmpty(Cache.CIBInformations);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation,
                Cache.CIBInformations
            }), HtmlLogUniqueId.LoggingHtml());

            return Cache.CIBInformations.All(t => ApplicationCookie.CIBInformations.Contains(t))
                   && ApplicationCookie.MicroscopeLensInformations.Contains(Cache.MicroscopeLensInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Guard.IsEqualTo(Cache.MicroscopeLensInformation, MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            StageViewModel.SetAbsoluteStageTheta(0);
            Cache.HazeFindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.CIBInformations,
                Cache.HazeFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var detectImageDirectory = ImageFileDirectory;

            Guard.IsTrue(ApplicationCookie.ProductivityInformations.Contains(Cache.ProductivityInformation));
            Guard.IsGreaterThan(Cache.MeasurePowerNotUseODFilterMinValue, CalibrationSetting.SettingCommonParam.MeasurePowerMeasurementMinValue);
            Guard.IsGreaterThan(Cache.MeasurePowerSequenceCommonRatio, 0);
            Guard.IsLessThan(Cache.MeasurePowerSequenceCommonRatio, 1);

            Cache.GeneratePrescanAODWaveformParam.WithFrequencyFlatness(Cache.PrescanFrequency);
            Cache.GenerateChirpAODWaveformParam.WithFrequencyFlatness(Cache.ChirpFrequency);

            Calibratings = [];
            Cache.PrescanAODWaveformResultFilePath = string.Empty;
            Cache.ChirpAODWaveformResultFilePath = string.Empty;
            Cache.PrescanAODWaveformProfiles = [];
            Cache.ChirpAODWaveformProfiles = [];
            Cache.MeasurePowerPoints = [];
            Cache.FitMeasurePowerPoints = [];
            Cache.NotUseODFilterMeasurePowerPoints = [];
            Cache.UseODFilterMeasurePowerPoints = [];
            Cache.P0 = 0d;
            Cache.P1 = 0d;
            Cache.P2 = 0d;
            Cache.P3 = 0d;
            Cache.RSquared = 0d;
            Cache.ODFilterRatio = 0d;

            var laserOpticalPowerMeter = LaserOpticalPowerMeters.Single(t => t.ProductivityInformation.OpticsMagType == Cache.ProductivityInformation.OpticsMagType && t.IsOk);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                MeasureMaxPowerPosition = laserOpticalPowerMeter.MaxMeasurePowerPosition,
                Cache.CIBInformations,
                Cache.HazeFindBFMachinePosition,
                Cache.ProductivityInformation,
                Cache.AFOffsetMotor,
                Cache.AFECS,
                Cache.IsAFEnable,
                GeneratePrescanAODWaveformParam = new HtmlQuote(Cache.GeneratePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
                GenerateChirpAODWaveformParam = new HtmlQuote(Cache.GenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
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
                Cache.ProtectedOverflowProtectedPMTValueCount,
                Cache.ImageWidth,
                Cache.DarkCurrent,
                Cache.Denominator,
                Cache.ScaleFactor,
                Cache.MinValidFraction,
                Cache.MaxValidFraction,
                MMDConfigurations = new HtmlExpand(string.Empty, new HtmlTable([.. Cache.MMDConfigurations])),
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);

            Cache.GeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
            Cache.GeneratePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
            var prescanAODWaveformResult = AODWaveformGenerator1.GeneratePrescanAODWaveform(Cache.GeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);
            Cache.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(prescanAODWaveformResult);
            Cache.PrescanAODWaveformResultFilePath = prescanAODWaveformResult.FilePath;

            Cache.GenerateChirpAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
            Cache.GenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
            var chirpAODWaveformResult = AODWaveformGenerator1.GenerateChirpAODWaveform(Cache.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);
            Cache.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(chirpAODWaveformResult);
            Cache.ChirpAODWaveformResultFilePath = chirpAODWaveformResult.FilePath;

            LaserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, [.. Cache.PrescanAODWaveformProfiles.Select(t => t.ApplyCoefficient(Cache.StartCoefficient))]);
            LaserViewModel.SetChirpAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, Cache.ChirpAODWaveformProfiles);

            CIBViewModel.ToggleProfileMode(Cache.CIBInformations, CIBProfileModeEnum.PMTVoltage);
            CIBViewModel.SetGain(Cache.CIBInformations, Cache.StartGain);

            Logger.LogHtmlInformation("AOD Waveform", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                GeneratePrescanAODWaveformParam = new HtmlQuote(Cache.GeneratePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
                Cache.PrescanAODWaveformResultFilePath,
                PrescanAODWaveformProfiles = new HtmlTable([.. Cache.PrescanAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())]),
                GenerateChirpAODWaveformParam = new HtmlQuote(Cache.GenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
                Cache.ChirpAODWaveformResultFilePath,
                ChirpAODWaveformProfiles = new HtmlTable([.. Cache.ChirpAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())])
            }), HtmlLogUniqueId.LoggingHtml());

            #region 获取功率

            var coefficients = Generate.LinearRange(Cache.StartCoefficient, Cache.StepCoefficient, Cache.StopCoefficient);
            Guard.IsNotEmpty(coefficients);
            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(laserOpticalPowerMeter.MaxMeasurePowerPosition);
            try
            {
                OpticsViewModel.ToggleODFilter(false);
                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Close);
                await Task.Delay(TimeSpan.FromSeconds(Cache.MeasurePowerWaitTime), cancellationToken).ConfigureAwait(false);
                var measurePowerNoises = (IReadOnlyList<double>)
                [
                    ..Enumerable.Range(0, HostEnvironment.IsProduction() ? 10000 : 0)
                        .Select(_ =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            return LaserViewModel.GetOpticalMeasurePower();
                        })
                ];
                var measurePowerNoise = HostEnvironment.IsProduction() ? measurePowerNoises.Average() : 0;

                foreach (var coefficient in coefficients)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        LaserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, [.. Cache.PrescanAODWaveformProfiles.Select(t => t.ApplyCoefficient(coefficient))]);
                        LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);
                        await Task.Delay(TimeSpan.FromSeconds(Cache.MeasurePowerWaitTime), cancellationToken).ConfigureAwait(false);

                        var measurePower = LaserViewModel.GetOpticalMeasurePower();

                        Cache.MeasurePowerPoints = [.. Cache.MeasurePowerPoints, new Point(coefficient, measurePower - measurePowerNoise)];
                    }
                    finally
                    {
                        LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);
                    }
                }

                var maxMeasurePowerPoint = Cache.MeasurePowerPoints.MaxBy(t => t.Y);
                try
                {
                    OpticsViewModel.ToggleODFilter(true);
                    LaserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, [.. Cache.PrescanAODWaveformProfiles.Select(t => t.ApplyCoefficient(maxMeasurePowerPoint.X))]);
                    LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);
                    await Task.Delay(TimeSpan.FromSeconds(Cache.MeasurePowerWaitTime), cancellationToken).ConfigureAwait(false);

                    Cache.ODFilterRatio = maxMeasurePowerPoint.Y / LaserViewModel.GetOpticalMeasurePower();
                }
                finally
                {
                    OpticsViewModel.ToggleODFilter(false);
                }
            }
            finally
            {
                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);
            }

            Cache.MeasurePowerPoints = [.. Cache.MeasurePowerPoints.Where(t => t.Y >= CalibrationSetting.SettingCommonParam.MeasurePowerMeasurementMinValue)]; // 过滤量程下限
            (Cache.P0, Cache.P1, Cache.P2, Cache.P3, Cache.RSquared, var yPredicted) =
                PolynomialCurve.Fit3(Vector<double>.Build.DenseOfEnumerable(Cache.MeasurePowerPoints.Select(t => t.X)), Vector<double>.Build.DenseOfEnumerable(Cache.MeasurePowerPoints.Select(t => t.Y)));

            Cache.FitMeasurePowerPoints = [.. Cache.MeasurePowerPoints.Index().Select(t => new Point(t.Item.X, yPredicted[t.Index]))];
            var minFitMeasurePowerPoint = Cache.FitMeasurePowerPoints.MinBy(t => t.Y);
            var maxFitMeasurePowerPoint = Cache.FitMeasurePowerPoints.MaxBy(t => t.Y);

            if (HostEnvironment.IsDevelopment()) Cache.MeasurePowerNotUseODFilterMinValue = (minFitMeasurePowerPoint.Y + maxFitMeasurePowerPoint.Y) / 2d;
            Guard.IsGreaterThan(Cache.MeasurePowerNotUseODFilterMinValue, minFitMeasurePowerPoint.Y);

            Cache.NotUseODFilterMeasurePowerPoints =
            [
                ..GeometricSequence.Generate(maxFitMeasurePowerPoint.Y, Cache.MeasurePowerSequenceCommonRatio, Cache.MeasurePowerNotUseODFilterMinValue)
                    .Select((t, index) =>
                    {
                        if (index == 0) return maxFitMeasurePowerPoint;

                        var solveForX = GeometricSequence.SolveForX(Cache.P0, Cache.P1, Cache.P2, Cache.P3, t);

                        return HostEnvironment.IsProduction()
                            ? new Point(solveForX.Single(tt => minFitMeasurePowerPoint.X < tt && tt < maxFitMeasurePowerPoint.X), t)
                            : new Point(solveForX.FirstOrDefault(tt => minFitMeasurePowerPoint.X < tt && tt < maxFitMeasurePowerPoint.X, solveForX.First()), t);
                    })
                    .OrderBy(t => t.X)
            ];
            Cache.UseODFilterMeasurePowerPoints =
            [
                ..GeometricSequence.Generate(Cache.MeasurePowerNotUseODFilterMinValue, Cache.MeasurePowerSequenceCommonRatio, maxFitMeasurePowerPoint.Y / Cache.MMDMeasurePowerRangeRatio)
                    .Where(t => t < Cache.NotUseODFilterMeasurePowerPoints[0].Y)
                    .Select(t => t * Cache.ODFilterRatio)
                    .Where(t => t < maxFitMeasurePowerPoint.Y)
                    .Where(t => t > minFitMeasurePowerPoint.Y)
                    .Select(t =>
                    {
                        var solveForX = GeometricSequence.SolveForX(Cache.P0, Cache.P1, Cache.P2, Cache.P3, t);

                        return HostEnvironment.IsProduction()
                            ? new Point(solveForX.Single(tt => minFitMeasurePowerPoint.X < tt && tt < maxFitMeasurePowerPoint.X), t)
                            : new Point(solveForX.FirstOrDefault(tt => minFitMeasurePowerPoint.X < tt && tt < maxFitMeasurePowerPoint.X, solveForX.First()), t);
                    })
                    .OrderBy(t => t.X)
            ];

            Logger.LogHtmlInformation("Measure Power", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                measurePowerPoints = Cache.ScatterPlotControl.GetHtmlPlot2DLinesChart()
            }), HtmlLogUniqueId.LoggingHtml());

            #endregion

            #region 获取 Gain Relationship / 初始化 Calibratings

            var cibmmdGains = CIBViewModel.GetCIBMMDGains(Cache.CIBInformations, Cache.StartGain, Cache.StepGain, Cache.StopGain);

            // 获取gain
            var gains = Generate.LinearRange(Cache.StartGain, Cache.StepGain, Cache.StopGain);
            Guard.IsNotEmpty(gains);
            Calibratings =
            [
                ..Cache.CIBInformations
                    .Index()
                    .Select(t => new CIBMMDDTO
                    {
                        CIBInformation = t.Item,
                        GainRelationships = cibmmdGains[t.Index],
                        Items =
                        [
                            .. Cache.UseODFilterMeasurePowerPoints.Select(tt => new CIBMMDDTOItem
                            {
                                Coefficient = tt.X,
                                MeasurePower = tt.Y / Cache.ODFilterRatio,
                                Items = [..gains.Select(ttt => new CIBMMDDTOItem.Item { Gain = ttt, PMTValue = double.NaN })]
                            }),
                            .. Cache.NotUseODFilterMeasurePowerPoints.Select(tt => new CIBMMDDTOItem
                            {
                                Coefficient = tt.X,
                                MeasurePower = tt.Y,
                                Items = [..gains.Select(ttt => new CIBMMDDTOItem.Item { Gain = ttt, PMTValue = double.NaN })]
                            })
                        ]
                    })
                    .OrderBy(t => t.CIBInformation)
            ];

            #endregion

            var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.HazeFindBFMachinePosition);
            StageViewModel.SetAbsoluteStageTheta(0);
            StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(hazeBFPosition);

            if (Cache.IsAFEnable)
            {
                AfViewModel.SetDarkField(CalChipSiteModelEnum.HazeModel, Cache.AFECS, Cache.AFOffsetMotor);
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
                             ..Cache.UseODFilterMeasurePowerPoints.Select(t => (t.X, true)),
                             ..Cache.NotUseODFilterMeasurePowerPoints.Select(t => (t.X, false))
                         ]).Index())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Logger.LogHtmlInformation($"{coefficient:0.###}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    try
                    {
                        OpticsViewModel.ToggleODFilter(isUseODFilter);
                        CIBViewModel.SetGain(Cache.CIBInformations, Cache.StartGain);
                        LaserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, [.. Cache.PrescanAODWaveformProfiles.Select(t => t.ApplyCoefficient(coefficient))]);
                        LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);

                        foreach (var (gainIndex, gain) in gains.Index())
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            Logger.LogHtmlInformation($"{gain:0.###}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                            SelectedCalibratingItems = Calibratings;

                            var noProtectedCIBMMDDtos = (IReadOnlyList<CIBMMDDTO>)[.. Calibratings.Where(t => t.Items[coefficientIndex].ProtectedOverflowProtectedPMTValueCount < Cache.ProtectedOverflowProtectedPMTValueCount /* 不超过保护次数 */)];
                            if (noProtectedCIBMMDDtos.All(t => double.IsNaN(t.Items[coefficientIndex].Items[gainIndex].PMTValue) == false)) continue;

                            var cibInformations = (IReadOnlyList<CIBInformation>)[.. noProtectedCIBMMDDtos.Select(t => t.CIBInformation)];
                            CIBViewModel.SetGain(cibInformations, gain);

                            await Task.Delay(TimeSpan.FromSeconds(Cache.PMTValueWaitTime), cancellationToken).ConfigureAwait(false);

                            var cibPMTImages = await CIBViewModel.GetPMTImagesAsync(
                                Cache.ProductivityInformation,
                                StageCoordinateSystemEnum.Dark,
                                hazeBFPosition,
                                Cache.ImageWidth,
                                cibInformations,
                                (true, null),
                                (true, null),
                                (true, null),
                                true, cancellationToken);

                            Logger.LogHtmlInformation("Images", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());
                            await Task.WhenAll(cibPMTImages.Index().Select(t => Task.Run(() =>
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                var (index, darkFieldImage) = t;
                                using var _ = darkFieldImage;

                                var pmtValue = darkFieldImage.Image.GetIntensity().Average;

                                var item = noProtectedCIBMMDDtos[index];
                                var itemItem = item.Items[coefficientIndex];
                                var itemItemData = itemItem.Items[gainIndex];
                                itemItemData.RawImageFilePath = darkFieldImage.RawImageFilePath;

                                if (pmtValue >= Cache.ProtectedPMTValue /* 超过保护值 */) itemItem.ProtectedOverflowProtectedPMTValueCount++;
                                itemItemData.PMTValue = pmtValue;

                                if (gainIndex == gains.Length - 1) /* 最后一次 */ SaveImage();

                                if (itemItem.ProtectedOverflowProtectedPMTValueCount >= Cache.ProtectedOverflowProtectedPMTValueCount /* 超过保护次数 */)
                                {
                                    SaveImage();
                                    CIBViewModel.SetGain([item.CIBInformation], Cache.StartGain);
                                }

                                Logger.LogHtmlInformation(item.CIBInformation.ToString(), HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                                {
                                    itemItemData.Gain,
                                    itemItemData.PMTValue,
                                    itemItemData.RawImageFilePath,
                                    Image = string.IsNullOrWhiteSpace(itemItemData.ImageFilePath) ? (BaseHtmlElement)new HtmlComment("The image was not saved. For details, see the raw file path.") : new HtmlImage(itemItemData.ImageFilePath)
                                }), HtmlLogUniqueId.LoggingHtml());

                                return;

                                void SaveImage()
                                {
                                    var imageFilePath = Path.Combine(detectImageDirectory, item.CIBInformation.ToString(), $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                                    darkFieldImage.Image.Save(imageFilePath);
                                    itemItemData.ImageFilePath = imageFilePath;
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
                CIBViewModel.ToggleEnableAGC(Cache.CIBInformations, true);
                CIBViewModel.ToggleProfileMode(Cache.CIBInformations, CIBProfileModeEnum.PMTLog);
                OpticsViewModel.ToggleODFilter(false);
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(hazeBFPosition);
            }

            Logger.LogHtmlInformation("Details", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            await Task.WhenAll(Calibratings.Select(t => Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                Algorithm(t);
            }, cancellationToken)));

            Guard.IsTrue(Save(Calibratings, cancellationToken));

            return Calibratings.All(t => t.IsCalibrated);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task AlgorithmAsync(CancellationToken cancellationToken)
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
                MMDConfigurations = new HtmlExpand(string.Empty, new HtmlTable([.. Cache.MMDConfigurations]))
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlInformation("Details", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            await Task.WhenAll(SelectedReviewItems.Select(t => Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                Algorithm(t);
            }, cancellationToken)));

            var result = SelectedReviewItems.All(t => t.IsCalibrated);

            DialogWindowProvider.ShowDialog($"Algorithm {(result ? "OK" : "Failed")}",
                DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        if (SelectedReviewItems.Count == 0)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(() =>
        {
            var errorMessageStringBuilder = new StringBuilder();

            foreach (var selectedReviewItem in SelectedReviewItems.OrderBy(t => t.CIBInformation))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var title = selectedReviewItem.CIBInformation.ToString();

                /*if (selectedReviewItem.IsCalibrated == false)
                {
                    errorMessageStringBuilder.AppendLine($"{title}: Error");
                    continue;
                }*/

                Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                if (selectedReviewItem.IsCalibrated) selectedReviewItem.IsVerified = true;

                var htmlBullet = new HtmlBullet(new
                {
                    SuccessPlot = new HtmlContainer([.. selectedReviewItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
                });

                if (selectedReviewItem.IsVerified)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                {
                    errorMessageStringBuilder.AppendLine($"{title}: Error");
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                }
            }

            Guard.IsTrue(Save(SelectedReviewItems, cancellationToken));

            var result = SelectedReviewItems.All(t => t.IsOk);

            DialogWindowProvider.ShowDialog($"""
                                             Verify : {(result ? "OK" : "Failed")}
                                             {errorMessageStringBuilder}
                                             """,
                DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        }).ConfigureAwait(false);
    }

    private void Algorithm(CIBMMDDTO item)
    {
        var htmlList = new List<BaseHtmlElement>
        {
            new HtmlBullet(new
            {
                GainRelationships = new HtmlExpand(string.Empty, new HtmlTable([.. item.GainRelationships]))
            })
        };

        var htmlContainer = new HtmlContainer(htmlList);
        var isSuccess = false;

        try
        {
            var mmdConfiguration = Cache.MMDConfigurations.Single(t => t.CIBInformation == item.CIBInformation);

            item.GainRSquared = 0d;
            item.GainResidual = 0d;
            item.GainPoints = [];
            item.OriginLogGainPoints = [];
            item.LogGainA1 = 0d;
            item.LogGainA2 = 0d;
            item.LogGainX0 = 0d;
            item.LogGainDx = 0d;
            item.LogGainRSquared = 0d;
            item.FitLogGainPoints = [];
            item.LogGainMul128U12BitPoints = [];
            item.GainS16BitPoints = [];

            var point3DList = (
                    from itemItem in item.Items
                    from itemItemData in itemItem.Items.Where(t => t.Gain >= mmdConfiguration.FilterMinGain)
                    where double.IsNaN(itemItemData.PMTValue) == false
                    select new Point3D(itemItemData.Gain, Math.Log(itemItem.MeasurePower), itemItemData.PMTValue))
                .ToList();

            htmlList.Add(new HtmlBullet(new
            {
                Map = new HtmlPlot3DChart(point3DList, "Map(X: Gain(V) - Y: mW - Z: PMT Value)", HtmlPlot3DType.Bar3D)
            }));

            var gains = item.Items[0].Items.Where(t => t.Gain >= mmdConfiguration.FilterMinGain).Select(t => t.Gain).ToArray();

            var coefficientCount = item.Items.Count;
            var gainCount = gains.Length;

            // Log(Light) + Log(Gain) = Log(Current)
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

            var xMeasurePowerVector = Vector<double>.Build.DenseOfEnumerable(item.Items.Select(t => t.MeasurePower));
            var xLogMeasurePowerVector = xMeasurePowerVector.Map(t => Math.Log(t * mmdConfiguration.PowerRate /* mW */ * 1000_000 / 0.34 /* 266nm激光当前采样下的单光子功率nW */, 2));

            var currentMatrix = Matrix<double>.Build.Dense(gainCount, coefficientCount);
            for (var row = 0; row < gainCount; row++)
            {
                for (var col = 0; col < coefficientCount; col++)
                {
                    currentMatrix[row, col] = item.Items[col].Items.Where(t => t.Gain >= mmdConfiguration.FilterMinGain).ToArray()[row].PMTValue;
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
            var bValidLogCurrentVector = Vector<double>.Build.Dense(validIndices.Count);
            for (var i = 0; i < validIndices.Count; i++)
            {
                var originalRow = validIndices[i];
                aValidMatrix.SetRow(i, aMatrix.Row(originalRow));
                bValidLogCurrentVector[i] = bLogCurrentVector[originalRow];
            }

            var aValidCoefficientSubMatrix = aValidMatrix.SubMatrix(0, validIndices.Count, 0, coefficientCount);
            var aValidGainSubMatrix = aValidMatrix.SubMatrix(0, validIndices.Count, coefficientCount, gainCount);

            var bValidVector = bValidLogCurrentVector - aValidCoefficientSubMatrix * xLogMeasurePowerVector;
            var xValidLogGainVector = aValidGainSubMatrix.QR().Solve(bValidVector);

            var xLogVector = Vector<double>.Build.Dense([.. xLogMeasurePowerVector, .. xValidLogGainVector]);
            var gainRSquared = Fit.RSquared(aValidGainSubMatrix * xValidLogGainVector, bValidVector);
            var gainResidual = (aValidMatrix * xLogVector - bValidLogCurrentVector).L2Norm();

            item.GainRSquared = gainRSquared;
            item.GainResidual = gainResidual;

            item.GainPoints =
            [
                ..xValidLogGainVector
                    .Map(t => Math.Pow(2, t))
                    .Enumerate()
                    .Index()
                    .Select(t => new Point(gains[t.Index], t.Item))
            ];
            item.OriginLogGainPoints =
            [
                ..xValidLogGainVector
                    .Enumerate()
                    .Index()
                    .Select(t => new Point(gains[t.Index], t.Item))
            ];

            var (a1, a2, x0, dx, rSquared, yPredicted) = BoltzmannCurve.Fit(Vector<double>.Build.DenseOfEnumerable(item.OriginLogGainPoints.Select(t => t.X)), Vector<double>.Build.DenseOfEnumerable(item.OriginLogGainPoints.Select(t => t.Y)));
            item.LogGainA1 = a1;
            item.LogGainA2 = a2;
            item.LogGainX0 = x0;
            item.LogGainDx = dx;
            item.LogGainRSquared = rSquared;
            item.FitLogGainPoints = [.. item.OriginLogGainPoints.Index().Select(t => new Point(t.Item.X, yPredicted[t.Index]))];

            htmlList.Add(new HtmlBullet(new
            {
                /*aValidMatrix = Environment.NewLine + aValidMatrix.ToMatrixString(aValidMatrix.RowCount, aValidMatrix.ColumnCount),
                xLogVector = Environment.NewLine + xLogVector.ToVectorString(xLogVector.Count, 1),
                bLogCurrentValidVector = Environment.NewLine + bValidLogCurrentVector.ToVectorString(bValidLogCurrentVector.Count, 1),*/
                gainRSquared,
                gainResidual
            }));

            isSuccess = item.FitLogGainPoints.All(t => t.Y is >= 0 and <= 14) && item.FitLogGainPoints.Select(t => t.Y).IsIncreasing(true); // logGain [0, 14], 且严格递增
            if (isSuccess == false)
            {
                ThrowHelper.ThrowArgumentException("LogGain out of range[0, 14]", nameof(item));

                return;
            }

            var results = item.FitLogGainPoints
                .Index()
                .Select(t => (
                    Gain: t.Item.X,
                    LogGainMultiplication128: (int)Math.Round(t.Item.Y * 128 /* KLA写死128 */, MidpointRounding.AwayFromZero),
                    item.GainRelationships[t.Index].SenseU14Bit,
                    item.GainRelationships[t.Index].GainS16Bit
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

            item.LogGainMul128U12BitPoints = logGainMul128U12BitPoints;
            item.GainS16BitPoints = gainS16BitPoints;

            htmlList.Add(new HtmlBullet(new
            {
                SuccessPlot = new HtmlContainer([.. item.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
            }));

            isSuccess = true;
        }
        catch (Exception ex)
        {
            isSuccess = false;

            htmlList.Add(new HtmlQuote(new
            {
                SuccessPlot = new HtmlContainer([.. item.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()]),
                Exception = ex
            }));
        }
        finally
        {
            item.IsCalibrated = isSuccess;
            item.IsVerified = false;

            if (isSuccess)
                Logger.LogHtmlInformation($"OK: {item.CIBInformation.ToString()}", HtmlHeaderLevelEnum.Header4, htmlContainer, HtmlLogUniqueId.LoggingHtml());
            else
                Logger.LogHtmlError($"Error: {item.CIBInformation.ToString()}", HtmlHeaderLevelEnum.Header4, htmlContainer, HtmlLogUniqueId.LoggingHtml());
        }
    }

    private bool Save(IReadOnlyList<CIBMMDDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                .. Calibrations.Where(t => t.CIBInformation != dto.CIBInformation),
                dto
            ];
        }

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}