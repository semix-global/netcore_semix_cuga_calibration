using System.IO;
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
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
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
using Constants = Net.Utilities.Models.Constants;

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

    partial void OnSelectedReviewItemsChanged(IReadOnlyList<CIBMMDDto> value)
    {
        foreach (var cibmmdDto in value) cibmmdDto.RefreshPlot();
    }

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

            DialogWindowProvider.ShowDialog("Import Gain Configuration OK!");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Import Gain Configuration");
            DialogWindowProvider.ShowDialog($"""
                                             Import Gain Configuration Failed!
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
            var laserOpticalPowerMeter = LaserOpticalPowerMeters.Single(t => t.ProductivityInformation.OpticsMagType == Cache.ProductivityInformation.OpticsMagType && t.IsOk);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.CIBInformations,
                Cache.FindBFMachinePosition,
                Cache.AFOffsetMotor,
                Cache.AFECS,
                Cache.IsAFEnable,
                Cache.ProductivityInformation,
                laserOpticalPowerMeter.MeasureMaxPowerPosition,
                GeneratePrescanAODWaveformParam = new HtmlQuote(Cache.GeneratePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
                GenerateChirpAODWaveformParam = new HtmlQuote(Cache.GenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
                Cache.CIBProfileMode,
                Cache.WaitTime,
                Cache.StartCoefficient,
                Cache.StepCoefficient,
                Cache.StopCoefficient,
                Cache.StartGain,
                Cache.StepGain,
                Cache.StopGain,
                Cache.ProtectedPMTValue,
                Cache.ProtectedCount,
                Cache.CatchPMTValueCount,
                Cache.ConcurrentCount,
                Table = new HtmlTable([..Cache.GainConfigurations])
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItems = [];
            Cache.PrescanAODWaveformProfiles = [];
            Cache.PrescanAODWaveformResultFilePath = string.Empty;
            Cache.ChirpAODWaveformResultFilePath = string.Empty;
            Cache.ChirpAODWaveformProfiles = [];

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

            LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);

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

            LaserViewModel.SetPrescanAODWaveProfiles([.. Cache.PrescanAODWaveformProfiles.Select(t => t.ApplyCoefficient(Cache.StartCoefficient))]);
            LaserViewModel.SetChirpAODWaveProfiles(Cache.ChirpAODWaveformProfiles);

            LaserViewModel.ToggleProfileMode(Cache.CIBProfileMode);
            LaserViewModel.SetGain(Cache.StartGain);

            var coefficients = Generate.LinearRange(Cache.StartCoefficient, Cache.StepCoefficient, Cache.StopCoefficient);
            Guard.IsNotEmpty(coefficients);
            var gains = Generate.LinearRange(Cache.StartGain, Cache.StepGain, Cache.StopGain);
            Guard.IsNotEmpty(gains);

            var gainConfigurations = (IReadOnlyList<CIBMMDCache.GainConfiguration>)[..gains.Select(t => Cache.GainConfigurations.Single(tt => Math.Abs(tt.Gain - t) < 1e-3))];

            Logger.LogHtmlInformation("AOD Waveform", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                GeneratePrescanAODWaveformParam = new HtmlQuote(Cache.GeneratePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
                Cache.PrescanAODWaveformResultFilePath,
                PrescanAODWaveformProfiles = new HtmlTable([.. Cache.PrescanAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())]),
                GenerateChirpAODWaveformParam = new HtmlQuote(Cache.GenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
                Cache.ChirpAODWaveformResultFilePath,
                ChirpAODWaveformProfiles = new HtmlTable([.. Cache.ChirpAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())]),
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItems = Cache.CIBInformations.Select(t => new CIBMMDDto
            {
                CIBInformation = t,
                Items =
                [
                    ..coefficients.Select(tt => new CIBMMDItemDto
                    {
                        Coefficient = tt,
                        MeasurePower = double.NaN,
                        Items = [..gains.Select(ttt => new CIBMMDItemDto.Item { Gain = ttt, PMTValue = double.NaN })]
                    })
                ]
            }).ToArray();

            foreach (var (coefficientIndex, coefficient) in coefficients.Index())
            {
                cancellationToken.ThrowIfCancellationRequested();

                double measurePower;
                try
                {
                    StageViewModel.SetMachineAbsoluteStageXy(laserOpticalPowerMeter.MeasureMaxPowerPosition);

                    LaserViewModel.SetPrescanAODWaveProfiles([.. Cache.PrescanAODWaveformProfiles.Select(t => t.ApplyCoefficient(coefficient))]);
                    LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);

                    measurePower = LaserViewModel.GetOpticalMeasurePower(Cache.ProductivityInformation, Cache.GeneratePrescanAODWaveformParam.FlatnessTime);
                }
                finally
                {
                    LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);
                }

                try
                {
                    StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.FindBFMachinePosition));
                    LaserViewModel.SetGain(Cache.StartGain);

                    LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);
                    foreach (var tuples in Cache.CIBInformations.Index().Chunk(Cache.ConcurrentCount))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var cibMMDDtos = tuples.Select(t => CalibratingItems[t.Index]).ToArray();

                        SelectedCalibratingItems = cibMMDDtos;

                        foreach (var (gainIndex, gain) in gains.Index())
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            foreach (var cibMMDDto in cibMMDDtos) cibMMDDto.Items[coefficientIndex].MeasurePower = measurePower;

                            var noProtectedCIBMMDDtos = cibMMDDtos
                                .Where(t => t.Items[coefficientIndex].ProtectedCount < Cache.ProtectedCount /* 不超过保护次数 */)
                                .ToArray();
                            LaserViewModel.SetGain([..noProtectedCIBMMDDtos.Select(t => t.CIBInformation)], gain);

                            await Task.Delay(TimeSpan.FromSeconds(Cache.WaitTime), cancellationToken).ConfigureAwait(false);

                            await Task.WhenAll(noProtectedCIBMMDDtos.Select(cibMMDDto => Task.Run(() =>
                            {
                                try
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    var item = cibMMDDto.Items[coefficientIndex];
                                    var itemItem = item.Items[gainIndex];

                                    var pmtValue = LaserViewModel.GetCIBOfPMTDataList(Cache.CatchPMTValueCount, cibMMDDto.CIBInformation).SelectMany(t => t).Average();
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
                }
                finally
                {
                    LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);
                }
            }

            Logger.LogHtmlInformation("Details", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            foreach (var tuples in Cache.CIBInformations.Index().Chunk(Cache.ConcurrentCount))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var cibMMDDtos = tuples.Select(t => CalibratingItems[t.Index]).ToArray();

                SelectedCalibratingItems = cibMMDDtos;

                await Task.WhenAll(cibMMDDtos.Select(cibMMDDto => Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Algorithm(cibMMDDto, gainConfigurations);
                }, cancellationToken)));
            }

            Guard.IsTrue(Save(CalibratingItems, cancellationToken));

            return CalibratingItems.All(t => t.IsCalibrated);
        });
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
            foreach (var selectedReviewItem in SelectedReviewItems) selectedReviewItem.IsVerified = true;

            Guard.IsTrue(Save(SelectedReviewItems, cancellationToken));

            DialogWindowProvider.ShowDialog("Verify OK");

            return true;
        }).ConfigureAwait(false);
    }

    private void Algorithm(CIBMMDDto cibMMDDto, IReadOnlyList<CIBMMDCache.GainConfiguration> gainConfigurations)
    {
        var htmlList = new List<BaseHtmlElement>();
        var htmlContainer = new HtmlContainer(htmlList);
        var isSuccess = false;

        try
        {
            var coefficientCount = cibMMDDto.Items.Count;
            var gainCount = cibMMDDto.Items[0].Items.Count;

            // A * X = B (最小二乘法)
            var aMatrix = Matrix<double>.Build.Dense(coefficientCount * gainCount, coefficientCount + gainCount);
            for (var i = 0; i < gainCount; i++)
            {
                var startRow = coefficientCount * i;
                var endRow = coefficientCount * (i + 1);
                for (var row = startRow; row < endRow; row++)
                {
                    aMatrix[row, i] = 1.0;
                }
            }

            for (var j = 0; j < coefficientCount; j++)
            {
                for (var k = 0; k < gainCount; k++)
                {
                    var row = coefficientCount * k + j;
                    aMatrix[row, j + gainCount] = 1.0;
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

            htmlList.Add(new HtmlBullet(new
            {
                aMatrix = Environment.NewLine + aMatrix.ToMatrixString(aMatrix.RowCount, aMatrix.ColumnCount),
                xLogMeasurePowerVector = Environment.NewLine + xLogMeasurePowerVector.ToVectorString(),
                bLogCurrentVector = Environment.NewLine + bLogCurrentVector.ToVectorString(),
            }));

            var validIndices = bLogCurrentVector
                .Index()
                .Where(t => double.IsNaN(t.Item) == false)
                .Select(t => t.Index).ToArray();

            var aValidMatrix = Matrix<double>.Build.Dense(validIndices.Length, aMatrix.ColumnCount);
            var bLogCurrentValidVector = Vector<double>.Build.Dense(validIndices.Length);
            for (var i = 0; i < validIndices.Length; i++)
            {
                var originalRow = validIndices[i];
                aValidMatrix.SetRow(i, aMatrix.Row(originalRow));
                bLogCurrentValidVector[i] = bLogCurrentVector[originalRow];
            }

            var aValidCoefficientSubMatrix = aValidMatrix.SubMatrix(0, validIndices.Length, 0, coefficientCount);
            var aValidGainSubMatrix = aValidMatrix.SubMatrix(0, validIndices.Length, coefficientCount, gainCount);

            var xLogGainVector = aValidGainSubMatrix.Solve(bLogCurrentValidVector - aValidCoefficientSubMatrix * xLogMeasurePowerVector);

            var xLogVector = Vector<double>.Build.Dense([..xLogMeasurePowerVector, ..xLogGainVector]);
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
                    .Select(t => new Point(cibMMDDto.Items[0].Items[t.Index].Gain, t.Item))
            ];
            cibMMDDto.LogGainPoints =
            [
                ..xLogGainVector
                    .Enumerate()
                    .Index()
                    .Select(t => new Point(cibMMDDto.Items[0].Items[t.Index].Gain, t.Item))
            ];

            cibMMDDto.RefreshPlot();

            htmlList.Add(new HtmlBullet(new
            {
                aValidMatrix = Environment.NewLine + aValidMatrix.ToMatrixString(aValidMatrix.RowCount, aValidMatrix.ColumnCount),
                xLogVector = Environment.NewLine + xLogVector.ToVectorString(),
                bLogCurrentValidVector = Environment.NewLine + bLogCurrentValidVector.ToVectorString(),
                gainResidual,
                gainNorm = gainL2Norm,
                Plot = new HtmlContainer([..cibMMDDto.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
            }));

            isSuccess = xLogGainVector.All(t => t is >= 0 and <= 14); // logGain 不能超过 14
            if (isSuccess == false)
            {
                htmlList.Add(new HtmlBullet("LogGain out of range! [0, 14]"));

                return;
            }

            var results = cibMMDDto.LogGainPoints
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

            cibMMDDto.RefreshPlot();

            htmlList.Add(new HtmlBullet(new
            {
                SuccessPlot = new HtmlContainer([..cibMMDDto.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
            }));

            isSuccess = true;

            LaserViewModel.SetCIBMMD(cibMMDDto.CIBInformation, [..logGainMul128U12BitPoints.Select(t => t.Y)], [..gainS16BitPoints.Select(t => t.Y)]);
        }
        finally
        {
            cibMMDDto.IsCalibrated = true;
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