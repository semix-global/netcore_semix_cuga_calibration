using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AodDelay;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.PrescanChirpAodAlignment;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;
using System.IO;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserPrescanChirpAodAlignmentCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserPrescanChirpAodAlignmentCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public IReadOnlyList<LaserLightInformation> LaserLightInformationList => ApplicationCookie.LaserLightInformationList;

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.OpticsMagTypeEnum);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.OpticsMagTypeEnum);

    public string PrescanFileDirectory => Path.Combine(AppHomeDirectory, "Prescan", nameof(LaserPrescanChirpAodAlignmentCalibrationViewModel), DirectoryHelper.RemoveInvalidDirectoryName(CalibrateDirectoryName), DateTime.Now.ToString(Constants.MiddleFileDateTimeFormat));

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Config" },
        new() { StepName = "Select a Mag" },
        new() { StepName = "Gain" },
        new() { StepName = "Alignment" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ObservableCollection<OpticsMagTypeEnumCalibrationStatus> _calibrationStatusList =
    [
        .. EnumHelper.Enums<OpticsMagTypeEnum>().Select(t => new OpticsMagTypeEnumCalibrationStatus { OpticsMagTypeEnum = t, IsCalibrated = false })
    ];

    [ObservableProperty]
    private LaserPrescanChirpAodAlignmentDto _resultCalibrateDto = new();

    #endregion Calibrate

    [ObservableProperty]
    private ObservableCollection<LaserPrescanChirpAodAlignmentDto> _reviewList = [];

    [ObservableProperty]
    private LaserPrescanChirpAodAlignmentDto? _selectReviewItemDto;

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private LaserPrescanChirpAodAlignmentCache _cache = new();

    [ObservableProperty]
    private LaserPrescanChirpAodAlignmentDto[] _calibrations = [];

    [ObservableProperty]
    private MicroscopeCalChipDto _microscopeCalChip = new();

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

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserAODDelayDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<LaserPrescanChirpAodAlignmentCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<LaserPrescanChirpAodAlignmentDto>();

        foreach (var calibrationStatus in Calibrations)
        {
            CalibrationStatusList
                .Single(t => t.OpticsMagTypeEnum == calibrationStatus.OpticsMagTypeEnum)
                .IsCalibrated = calibrationStatus.IsCalibrated;
        }

        if (Cache.MicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.MicroscopeLensInformation = CalibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();

        if (isHasCache == false) CacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Cache.FindPosition = MicroscopeCalChip.HazeBrightFieldMachinePosition;
        StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));
        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewList =
        [
            .. Calibrations
                .Select(t => t.Clone())
                .OrderBy(t => t.OpticsMagTypeEnum)
        ];

        if (ReviewList.All(t => t.IsCalibrated == false))
            return false;

        StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(Cache.FindPosition);
        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 1:
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(Cache.FindPosition);
                return true;

            case 2:

                ResultCalibrateDto.Clear();
                return true;

            case 3:
                ResultCalibrateDto.IsCalibrated = true;
                if (Save(ResultCalibrateDto, cancellationToken) == false)
                {
                    ResultCalibrateDto.IsCalibrated = false;
                    Logger.LogError("{@Name} Error: Save Failed!", Name);
                    return false;
                }

                CalibrationStatusList.Single(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum).IsCalibrated = true;
                DialogWindowProvider.ShowDialog("Find Offset Ok!");

                IsCalibrated = CalibrationStatusList.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                return true;

            default:
                return true;
        }
    }

    #endregion 控制校准业务

    #region 校准



    [RelayCommand]
    private Task ConfigStepActionAsync()
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                IsAutoGain = Cache.CIBConfiguration.IsAutoGainControl,
                DcGainVoltage = Cache.CIBConfiguration.Gain,
                IsL0k = Cache.CIBConfiguration.IsL0K,
                CIBProfileTypeEnum = Cache.CIBConfiguration.CIBProfileMode
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var result = StageViewModel.GetBrightFieldStagePosition();

            Cache.FindPosition = result;

            ResultCalibrateDto.OpticsMagTypeEnum = Cache.OpticsMagTypeEnum;
            ResultCalibrateDto.FindPosition = Cache.FindPosition;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
                Cache.XSpeed,
                Cache.PmtId,
                Cache.WidthPixel,
                Cache.OpticsMagTypeEnum,
                Cache.FindPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
                Cache.XSpeed,
                Cache.PmtId,
                Cache.WidthPixel,
                Cache.OpticsMagTypeEnum,
                Cache.FindPosition
            }), HtmlLogUniqueId.LoggingHtml());

            LaserViewModel.SetGain(Cache.Gain);

            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

            ResultCalibrateDto.Gain = Cache.Gain;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Gain
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var detectPrescanDirectory = PrescanFileDirectory;
            var detectImageDirectory = ImageFileDirectory;
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
                Cache.XSpeed,
                Cache.PmtId,
                Cache.WidthPixel,
                Cache.OpticsMagTypeEnum,
                Cache.FindPosition,
                Cache.Gain,
                GeneratePrescanAODWaveformParam = new HtmlQuote(Cache.GeneratePrescanAODWaveformParam.ToHtmlAnonymous()),
                Cache.StartPrescanCenterFrequency,
                Cache.EndPrescanCenterFrequency,
                Cache.StepPrescanCenterFrequency,
                detectPrescanDirectory,
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            LaserViewModel.SetGain(Cache.Gain);

            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

            var yPixelHeight = LaserViewModel.GetDarkFieldLineScanImageYPixelHeight(Cache.OpticsMagTypeEnum);

            ResultCalibrateDto.Clear();

            foreach (var centerFrequency in Generate.LinearRange(Cache.StartPrescanCenterFrequency, Cache.StepPrescanCenterFrequency, Cache.EndPrescanCenterFrequency))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = new LaserPrescanChirpAodAlignmentItemDto();

                Cache.GeneratePrescanAODWaveformParam.OpticsMagTypeEnum = Cache.OpticsMagTypeEnum;
                Cache.GeneratePrescanAODWaveformParam.WithFrequencyFlatness(centerFrequency);
                Cache.GeneratePrescanAODWaveformParam.DirectoryPath = PrescanFileDirectory;
                var (aodWaveformResultItem, exceptionItem) = AODWaveformGenerator.GeneratePrescanAODWaveform(Cache.GeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);
                if (aodWaveformResultItem.IsSuccess == false) throw GuardUtils.IsNotNullAndReturn(exceptionItem);

                item.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResultItem);
                item.PrescanAODWaveformResultFilePath = aodWaveformResultItem.FilePath;

                item.OpticsMagTypeEnum = Cache.OpticsMagTypeEnum;
                item.PrescanCenterFrequency = centerFrequency;

                var (isSuccess, channel1DarkFieldImageDto, channel2DarkFieldImageDto, channel3DarkFieldImageDto) = GetDarkFieldLineScanImage(item.PrescanAODWaveformProfiles);
                if (isSuccess == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Get Dark Field Line Scan Image Error!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                using var _1 = channel1DarkFieldImageDto;
                using var _2 = channel2DarkFieldImageDto;
                using var _3 = channel3DarkFieldImageDto;
                var middleFileDateTimeFormat = DateTimeHelper.DateTime2String(DateTime.Now, Constants.MiddleFileDateTimeFormat);
                item.Channel1ImageFilePath = $"{detectImageDirectory}\\({HtmlLogUniqueId}_{middleFileDateTimeFormat}_Channel1_{item.PrescanCenterFrequency:0.###}).jpg";
                channel1DarkFieldImageDto.Image.Save(item.Channel1ImageFilePath);
                item.Channel2ImageFilePath = $"{detectImageDirectory}\\({HtmlLogUniqueId}_{middleFileDateTimeFormat}_Channel2_{item.PrescanCenterFrequency:0.###}).jpg";
                channel2DarkFieldImageDto.Image.Save(item.Channel2ImageFilePath);
                item.Channel3ImageFilePath = $"{detectImageDirectory}\\({HtmlLogUniqueId}_{middleFileDateTimeFormat}_Channel3_{item.PrescanCenterFrequency:0.###}).jpg";
                channel3DarkFieldImageDto.Image.Save(item.Channel3ImageFilePath);
                item.Channel1DarkFieldImageProjectionYs = channel1DarkFieldImageDto.ProjectionYs;
                item.Channel2DarkFieldImageProjectionYs = channel2DarkFieldImageDto.ProjectionYs;
                item.Channel3DarkFieldImageProjectionYs = channel3DarkFieldImageDto.ProjectionYs;
                item.DarkFieldImageProjectionYsMaxPixel = Vector<double>.Build.DenseOfArray(item.Channel3DarkFieldImageProjectionYs).MaximumIndex();
                item.DarkFieldImageProjectionYsMaxValue = item.Channel3DarkFieldImageProjectionYs[item.DarkFieldImageProjectionYsMaxPixel];

                ResultCalibrateDto.Items = [.. ResultCalibrateDto.Items, item];

                Logger.LogHtmlInformation($"{item.PrescanCenterFrequency:0.###}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    item.PrescanAODWaveformResultFilePath,
                    PrescanAODWaveformProfiles = new HtmlTable([.. item.PrescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())]),
                    DarkFieldImageProjectionYAveragesMaxPixel = item.DarkFieldImageProjectionYsMaxPixel,
                    DarkFieldImageProjectionYAveragesMaxValue = item.DarkFieldImageProjectionYsMaxValue,
                    DarkFieldImageProjectionYAverage = new HtmlPlot2DLinesChart([
                        (nameof(item.Channel1DarkFieldImageProjectionYs), item.Channel1DarkFieldImageProjectionYs.ToPoints()),
                        (nameof(item.Channel2DarkFieldImageProjectionYs), item.Channel2DarkFieldImageProjectionYs.ToPoints()),
                        (nameof(item.Channel3DarkFieldImageProjectionYs), item.Channel3DarkFieldImageProjectionYs.ToPoints())
                    ], "DarkFieldImageList"),
                    HtmlTab = new HtmlTab(new
                    {
                        CH1 = new HtmlImage(item.Channel1ImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        CH2 = new HtmlImage(item.Channel2ImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        CH3 = new HtmlImage(item.Channel3ImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                }), HtmlLogUniqueId.LoggingHtml());
            }

            var itemPoints = ResultCalibrateDto.ItemPoints.Skip(1).SkipLast(1).ToArray();
            var (slope, intercept, rSquared, yPredicted) = HostEnvironment.IsDevelopment()
                ? PolynomialLeastSquares.Polynomial1Fit(Vector<double>.Build.DenseOfArray([190, 195, 200, 205, 210, 215]), Vector<double>.Build.DenseOfArray([125, 233, 349, 452, 545, 654]))
                : PolynomialLeastSquares.Polynomial1Fit(Vector<double>.Build.DenseOfEnumerable(itemPoints.Select(t => t.X)), Vector<double>.Build.DenseOfEnumerable(itemPoints.Select(t => t.Y)));
            ResultCalibrateDto.Slope = slope;
            ResultCalibrateDto.Intercept = intercept;
            ResultCalibrateDto.RSquared = rSquared;
            ResultCalibrateDto.ItemFitPoints = [.. itemPoints.Select((t, i) => new Point(t.X, yPredicted[i]))];

            var generatePrescanAODWaveformParam = Cache.GeneratePrescanAODWaveformParam.Clone();
            Cache.GeneratePrescanAODWaveformParam.OpticsMagTypeEnum = Cache.OpticsMagTypeEnum;
            Cache.GeneratePrescanAODWaveformParam.BandWidth = Math.Abs(yPixelHeight / ResultCalibrateDto.Slope);
            Cache.GeneratePrescanAODWaveformParam.WithFrequencyFlatness((yPixelHeight / 2d - ResultCalibrateDto.Intercept) / ResultCalibrateDto.Slope);
            Cache.GeneratePrescanAODWaveformParam.FunctionMonotonicTypeEnum = Cache.FunctionMonotonicTypeEnum;
            Cache.GeneratePrescanAODWaveformParam.FlatnessTime = yPixelHeight * 4d;
            Cache.GeneratePrescanAODWaveformParam.DirectoryPath = PrescanFileDirectory;

            var (aodWaveformResult, exception) = AODWaveformGenerator.GeneratePrescanAODWaveform(generatePrescanAODWaveformParam.AdaptTo(), cancellationToken);
            if (aodWaveformResult.IsSuccess == false) throw GuardUtils.IsNotNullAndReturn(exception);

            ResultCalibrateDto.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResult);
            ResultCalibrateDto.PrescanAODWaveformResultFilePath = aodWaveformResult.FilePath;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                ResultCalibrateDto.PrescanAODWaveformResultFilePath,
                PrescanAODWaveformProfiles = new HtmlTable([.. ResultCalibrateDto.PrescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())]),
                Result = new HtmlPlot2DLinesChart([
                    (nameof(itemPoints), itemPoints),
                    (nameof(ResultCalibrateDto.ItemFitPoints), ResultCalibrateDto.ItemFitPoints)
                ], "Result")
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        if (SelectReviewItemDto is null)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(() =>
        {
            SelectReviewItemDto.IsVerified = true;

            if (Save(SelectReviewItemDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                SelectReviewItemDto.IsVerified = false;
                return false;
            }

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Result = new HtmlPlot2DLinesChart([
                    (nameof(ResultCalibrateDto.ItemPoints), ResultCalibrateDto.ItemPoints)
                ], "Result")
            }), HtmlLogUniqueId.LoggingHtml());

            DialogWindowProvider.ShowDialog("Verify OK");

            return true;
        }).ConfigureAwait(false);
    }

    private (bool IsSuccess,
        DarkFieldImageDto Channel1DarkFieldImageDto,
        DarkFieldImageDto Channel2DarkFieldImageDto,
        DarkFieldImageDto Channel3DarkFieldImageDto)
        GetDarkFieldLineScanImage(IReadOnlyList<PrescanAODWaveformProfile> aodWaveformProfile)
    {
        LaserViewModel.SetPrescanAODWaveProfiles(aodWaveformProfile);

        var list = LaserViewModel.GetDarkFieldLineScanImageList(
            CalChipSiteModelEnum.HazeModel,
            Cache.FindPosition,
            Cache.WidthPixel,
            Cache.OpticsMagTypeEnum,
            Cache.XSpeed,
            Cache.PmtId,
            StageCoordinateSystemEnum.Bright,
            Cache.CIBConfiguration,
            (true, null),
            false);

        var channel1DarkFieldImageDto = list.Single(t => t.ChannelId == 1);
        var channel2DarkFieldImageDto = list.Single(t => t.ChannelId == 2);
        var channel3DarkFieldImageDto = list.Single(t => t.ChannelId == 3);

        return (
            true,
            channel1DarkFieldImageDto,
            channel2DarkFieldImageDto,
            channel3DarkFieldImageDto
        );
    }

    private bool Save(LaserPrescanChirpAodAlignmentDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibrations =
        [
            .. Calibrations
                .Where(t => t.OpticsMagTypeEnum != dto.OpticsMagTypeEnum),
            dto.Clone()
        ];

        CacheProvider.SetArray(Calibrations, cancellationToken);
        CacheProvider.Set(Cache, cancellationToken);
    }) && EnableDependedCalibrationItems(cancellationToken);

    protected override bool EnableDependedCalibrationItems(CancellationToken cancellationToken)
    {
        if (CalibrationStatusService.EnableDependLaserPrescanChirpAodAlignmentCalibrations(false, cancellationToken, out var errorMsg) == false)
        {
            Logger.LogError("Toggle {@Name} Enable Status Failed!", errorMsg);
            return false;
        }

        return true;
    }

    #endregion 校准
}