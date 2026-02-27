using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Optics.CollectPolarization;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using Core.Utilities;
using Core.Utilities.SourceGenerators.Attributes;
using Local.SQL.Cache.Providers.Extensions;
using MathNet.Numerics;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using System.IO;
using Point = Net.Utilities.Models.Geometries.Point;


namespace CugaCalibration.ViewModels.Optics;

[IOCAppService(ServiceType = typeof(CollectionPolarizationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CollectionPolarizationViewModel(
    ICalibrationAlgorithmService calibrationAlgorithmService,
    CalibrationSetting calibrationSetting,
    ICalibrationFourierService calibrationFlourierService,
    ICalibrationOpticsService calibrationOpticService,
    ICalibrationLaserService calibrationLaserService) : CalibrationViewModelBase
{
    #region 界面相关

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Haze Wafer Position" },
        new() { StepName = "Set Laser Light And CIB Configuration" },
        new() { StepName = "Get S Polarization Position Of CH1" },
        new() { StepName = "Get S Polarization Position Of CH2" },
        new() { StepName = "Get S Polarization Position Of CH3" },
        new() { StepName = "Get P Polarization Position Of CH1" },
        new() { StepName = "Get P Polarization Position Of CH2" },
        new() { StepName = "Get P Polarization Position Of CH3" }
    ];

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private CollectPolarizationDTO _resultCollectItemDto = new();

    [ObservableProperty]
    private MicroscopeCalChipDTO _microscopeCalChip = new();

    [RecipeCache]
    [ObservableProperty]
    private CollectPolarizationCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private CollectPolarizationDTO _calibration = new();

    #endregion 缓存

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false)
            return false;

        MicroscopeCalChip = CalibrationStatusService.GetCalibration<MicroscopeCalChipDTO>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<CollectPolarizationCache>();
        Calibration = CacheProvider.GetOrDefault<CollectPolarizationDTO>();
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

        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        switch (CalibrationStepIndex)
        {
            case 1:

                return true;

            case 2:

                return true;

            case 3:

                return true;

            case 4:

                return true;

            case 5:

                return true;

            case 6:

                return true;

            case 7:

                Save(ResultCollectItemDto, cancellationToken);
                IsCalibrated = true;
                return true;

            default:

                return true;
        }
    }

    protected override Task<bool> CancelingAsync()
    {
        return Task.FromResult(true);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0Async(CancellationToken cancellationToken)
    {
        Cache.HazeWaferPosition = Cache.HazeWaferPosition != Point.Origin
            ? Cache.HazeWaferPosition
            : GuardUtils.IsNotNullAndReturn(MicroscopeCalChip.HazeItem).BrightFieldMachinePosition;

        StageViewModel.SetAbsoluteStageTheta(0);
        StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.HazeWaferPosition));
        AfViewModel.ToggleDarkFieldEnable(true);

        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.HazeWaferPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.LaserLightInformation,
                CIBConfiguration = new HtmlQuote(Cache.CIBConfiguration.ToHtmlAnonymous()),
                Cache.CIBInformation
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.LaserLightInformations.Contains(Cache.LaserLightInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
    {
        calibrationOpticService.SetPolarization(OpticsPolarizationModeEnum.P);
        return InvokeCalibrateAsync(async () =>
        {
            Cache.PolarizationPositionNDFSListCH1 = [];
            var CIBInfor = ApplicationCookie.CIBInformations.Single(x => x.PMTId == 8 && x.ChannelId == 1);
            calibrationOpticService.SetNDF(OpticsChannelModeEnum.CH1_NDF, OpticsNDFTypeEnum.S);

            double[] angleArray = Generate.LinearRange(
                Cache.FindAngleMin,
                Cache.FindAngleInterval,
                Cache.FindAngleMax);

            foreach (double i in angleArray)
            {
                using var darkFieldImage = await CIBViewModel.GetPMTImageAsync(
                    ApplicationCookie.OILowProductivityInformation,
                    StageCoordinateSystemEnum.Dark,
                    StageViewModel.MachineToBrightFieldPosition(Cache.HazeWaferPosition),
                    Cache.ImageWidth,
                    CIBInfor,
                    (false, CalChipSiteModelEnum.HazeModel),
                    (false, Cache.CIBConfiguration),
                    (false, Cache.LaserLightInformation),
                    false,
                    cancellationToken);

                if (darkFieldImage == null)
                    continue;

                calibrationOpticService.SetNDFRotary(OpticsChannelModeEnum.CH1_NDF, i);

                double pmtValue = darkFieldImage.Image.GetIntensity().Average;
                Cache.PolarizationPositionNDFSListCH1 = [.. Cache.PolarizationPositionNDFSListCH1, new Point(i, pmtValue)];

                var OriginImageFilePath = Path.Combine(ImageFileDirectory, "CH1", "SInitial" + "__" + i + "__" + $"{Guid.NewGuid():N}.jpg");
                darkFieldImage.Image.Save(OriginImageFilePath);
                Logger.LogHtmlInformation("Angle:" + i, HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    ResultImage = new HtmlImage(OriginImageFilePath)
                }), HtmlLogUniqueId.LoggingHtml());
            }

            var pointWithMinY = Cache.PolarizationPositionNDFSListCH1.OrderBy(p => p.Y).First();
            Cache.PolarizationPositionNDFSCH1 = pointWithMinY.X;

            Logger.LogHtmlInformation("SlopeParam", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                CH1MINY = Cache.PolarizationPositionNDFSListCH1.Min(p => p.Y),
                CH1MINX = Cache.PolarizationPositionNDFSListCH1.OrderBy(p => p.Y).First().X,
                PolarizationPositionNDFSListCH1 = new HtmlPlot2DLinesChart([("SList", Cache.PolarizationPositionNDFSListCH1)], string.Empty)
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            Cache.PolarizationPositionNDFSListCH2 = [];
            var CIBInfor = ApplicationCookie.CIBInformations.Single(x => x.PMTId == 8 && x.ChannelId == 2);
            calibrationOpticService.SetNDF(OpticsChannelModeEnum.CH2_NDF, OpticsNDFTypeEnum.S);

            double[] angleArray = Generate.LinearRange(
                Cache.FindAngleMin,
                Cache.FindAngleInterval,
                Cache.FindAngleMax);

            foreach (double i in angleArray)
            {
                using var darkFieldImage = await CIBViewModel.GetPMTImageAsync(
                    ApplicationCookie.OILowProductivityInformation,
                    StageCoordinateSystemEnum.Dark,
                    StageViewModel.MachineToBrightFieldPosition(Cache.HazeWaferPosition),
                    Cache.ImageWidth,
                    CIBInfor,
                    (false, CalChipSiteModelEnum.HazeModel),
                    (false, Cache.CIBConfiguration),
                    (false, Cache.LaserLightInformation),
                    false,
                    cancellationToken);

                if (darkFieldImage == null)
                    continue;

                calibrationOpticService.SetNDFRotary(OpticsChannelModeEnum.CH2_NDF, i);

                double pmtValue = darkFieldImage.Image.GetIntensity().Average;
                Cache.PolarizationPositionNDFSListCH2 = [.. Cache.PolarizationPositionNDFSListCH2, new Point(i, pmtValue)];

                var OriginImageFilePath = Path.Combine(ImageFileDirectory, "CH2", "SInitial" + "__" + i + "__" + $"{Guid.NewGuid():N}.jpg");
                darkFieldImage.Image.Save(OriginImageFilePath);
                Logger.LogHtmlInformation("Angle:" + i, HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    ResultImage = new HtmlImage(OriginImageFilePath)
                }), HtmlLogUniqueId.LoggingHtml());
            }

            var pointWithMinY = Cache.PolarizationPositionNDFSListCH2.OrderBy(p => p.Y).First();
            Cache.PolarizationPositionNDFSCH2 = pointWithMinY.X;

            Logger.LogHtmlInformation("SlopeParam", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                CH2MINY = Cache.PolarizationPositionNDFSListCH2.Min(p => p.Y),
                CH2MINX = Cache.PolarizationPositionNDFSListCH2.OrderBy(p => p.Y).First().X,
                PolarizationPositionNDFSListCH2 = new HtmlPlot2DLinesChart([("SList", Cache.PolarizationPositionNDFSListCH2)], string.Empty)
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step4Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            Cache.PolarizationPositionNDFSListCH3 = [];
            var CIBInfor = ApplicationCookie.CIBInformations.Single(x => x.PMTId == 8 && x.ChannelId == 3);
            calibrationOpticService.SetNDF(OpticsChannelModeEnum.CH3_NDF, OpticsNDFTypeEnum.S);

            double[] angleArray = Generate.LinearRange(
                Cache.FindAngleMin,
                Cache.FindAngleInterval,
                Cache.FindAngleMax);

            foreach (double i in angleArray)
            {
                using var darkFieldImage = await CIBViewModel.GetPMTImageAsync(
                    ApplicationCookie.OILowProductivityInformation,
                    StageCoordinateSystemEnum.Dark,
                    StageViewModel.MachineToBrightFieldPosition(Cache.HazeWaferPosition),
                    Cache.ImageWidth,
                    CIBInfor,
                    (false, CalChipSiteModelEnum.HazeModel),
                    (false, Cache.CIBConfiguration),
                    (false, Cache.LaserLightInformation),
                    false,
                    cancellationToken);

                if (darkFieldImage == null)
                    continue;

                calibrationOpticService.SetNDFRotary(OpticsChannelModeEnum.CH3_NDF, i);

                double pmtValue = darkFieldImage.Image.GetIntensity().Average;
                Cache.PolarizationPositionNDFSListCH3 = [.. Cache.PolarizationPositionNDFSListCH3, new Point(i, pmtValue)];

                var OriginImageFilePath = Path.Combine(ImageFileDirectory, "CH3", "SInitial" + "__" + i + "__" + $"{Guid.NewGuid():N}.jpg");
                darkFieldImage.Image.Save(OriginImageFilePath);
                Logger.LogHtmlInformation("Angle:" + i, HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    ResultImage = new HtmlImage(OriginImageFilePath)
                }), HtmlLogUniqueId.LoggingHtml());
            }

            var pointWithMinY = Cache.PolarizationPositionNDFSListCH3.OrderBy(p => p.Y).First();
            Cache.PolarizationPositionNDFSCH3 = pointWithMinY.X;

            Logger.LogHtmlInformation("SlopeParam", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                CH3MINY = Cache.PolarizationPositionNDFSListCH3.Min(p => p.Y),
                CH3MINX = Cache.PolarizationPositionNDFSListCH3.OrderBy(p => p.Y).First().X,
                PolarizationPositionNDFSListCH3 = new HtmlPlot2DLinesChart([("SList", Cache.PolarizationPositionNDFSListCH3)], string.Empty)
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step5Async(CancellationToken cancellationToken)
    {
        calibrationOpticService.SetPolarization(OpticsPolarizationModeEnum.S);
        return InvokeCalibrateAsync(async () =>
        {
            Cache.PolarizationPositionNDFPListCH1 = [];
            var CIBInfor = ApplicationCookie.CIBInformations.Single(x => x.PMTId == 8 && x.ChannelId == 1);
            calibrationOpticService.SetNDF(OpticsChannelModeEnum.CH1_NDF, OpticsNDFTypeEnum.P);

            double[] angleArray = Generate.LinearRange(
                Cache.FindAngleMin,
                Cache.FindAngleInterval,
                Cache.FindAngleMax);

            foreach (double i in angleArray)
            {
                using var darkFieldImage = await CIBViewModel.GetPMTImageAsync(
                    ApplicationCookie.OILowProductivityInformation,
                    StageCoordinateSystemEnum.Dark,
                    StageViewModel.MachineToBrightFieldPosition(Cache.HazeWaferPosition),
                    Cache.ImageWidth,
                    CIBInfor,
                    (false, CalChipSiteModelEnum.HazeModel),
                    (false, Cache.CIBConfiguration),
                    (false, Cache.LaserLightInformation),
                    false,
                    cancellationToken);

                if (darkFieldImage == null)
                    continue;

                calibrationOpticService.SetNDFRotary(OpticsChannelModeEnum.CH1_NDF, i);

                double pmtValue = darkFieldImage.Image.GetIntensity().Average;
                Cache.PolarizationPositionNDFPListCH1 = [.. Cache.PolarizationPositionNDFPListCH1, new Point(i, pmtValue)];

                var OriginImageFilePath = Path.Combine(ImageFileDirectory, "CH1", "PInitial" + "__" + i + "__" + $"{Guid.NewGuid():N}.jpg");
                darkFieldImage.Image.Save(OriginImageFilePath);
                Logger.LogHtmlInformation("Angle:" + i, HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    ResultImage = new HtmlImage(OriginImageFilePath)
                }), HtmlLogUniqueId.LoggingHtml());
            }

            var pointWithMinY = Cache.PolarizationPositionNDFPListCH1.OrderBy(p => p.Y).First();
            Cache.PolarizationPositionNDFPCH1 = pointWithMinY.X;

            Logger.LogHtmlInformation("SlopeParam", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                CH1MINY = Cache.PolarizationPositionNDFPListCH1.Min(p => p.Y),
                CH1MINX = Cache.PolarizationPositionNDFPListCH1.OrderBy(p => p.Y).First().X,
                PolarizationPositionNDFPListCH1 = new HtmlPlot2DLinesChart([("PList", Cache.PolarizationPositionNDFPListCH1)], string.Empty)
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step6Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            Cache.PolarizationPositionNDFPListCH2 = [];
            var CIBInfor = ApplicationCookie.CIBInformations.Single(x => x.PMTId == 8 && x.ChannelId == 2);
            calibrationOpticService.SetNDF(OpticsChannelModeEnum.CH2_NDF, OpticsNDFTypeEnum.P);

            double[] angleArray = Generate.LinearRange(
                Cache.FindAngleMin,
                Cache.FindAngleInterval,
                Cache.FindAngleMax);

            foreach (double i in angleArray)
            {
                using var darkFieldImage = await CIBViewModel.GetPMTImageAsync(
                    ApplicationCookie.OILowProductivityInformation,
                    StageCoordinateSystemEnum.Dark,
                    StageViewModel.MachineToBrightFieldPosition(Cache.HazeWaferPosition),
                    Cache.ImageWidth,
                    CIBInfor,
                    (false, CalChipSiteModelEnum.HazeModel),
                    (false, Cache.CIBConfiguration),
                    (false, Cache.LaserLightInformation),
                    false,
                    cancellationToken);

                if (darkFieldImage == null)
                    continue;

                calibrationOpticService.SetNDFRotary(OpticsChannelModeEnum.CH2_NDF, i);

                double pmtValue = darkFieldImage.Image.GetIntensity().Average;
                Cache.PolarizationPositionNDFPListCH2 = [.. Cache.PolarizationPositionNDFPListCH2, new Point(i, pmtValue)];

                var OriginImageFilePath = Path.Combine(ImageFileDirectory, "CH2", "PInitial" + "__" + i + "__" + $"{Guid.NewGuid():N}.jpg");
                darkFieldImage.Image.Save(OriginImageFilePath);
                Logger.LogHtmlInformation("Angle:" + i, HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    ResultImage = new HtmlImage(OriginImageFilePath)
                }), HtmlLogUniqueId.LoggingHtml());
            }

            var pointWithMinY = Cache.PolarizationPositionNDFPListCH2.OrderBy(p => p.Y).First();
            Cache.PolarizationPositionNDFPCH2 = pointWithMinY.X;

            Logger.LogHtmlInformation("SlopeParam", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                CH2MINY = Cache.PolarizationPositionNDFPListCH2.Min(p => p.Y),
                CH2MINX = Cache.PolarizationPositionNDFPListCH2.OrderBy(p => p.Y).First().X,
                PolarizationPositionNDFPListCH2 = new HtmlPlot2DLinesChart([("PList", Cache.PolarizationPositionNDFPListCH2)], string.Empty)
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step7Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            Cache.PolarizationPositionNDFPListCH3 = [];
            var CIBInfor = ApplicationCookie.CIBInformations.Single(x => x.PMTId == 8 && x.ChannelId == 3);
            calibrationOpticService.SetNDF(OpticsChannelModeEnum.CH3_NDF, OpticsNDFTypeEnum.P);

            double[] angleArray = Generate.LinearRange(
                Cache.FindAngleMin,
                Cache.FindAngleInterval,
                Cache.FindAngleMax);

            foreach (double i in angleArray)
            {
                using var darkFieldImage = await CIBViewModel.GetPMTImageAsync(
                    ApplicationCookie.OILowProductivityInformation,
                    StageCoordinateSystemEnum.Dark,
                    StageViewModel.MachineToBrightFieldPosition(Cache.HazeWaferPosition),
                    Cache.ImageWidth,
                    CIBInfor,
                    (false, CalChipSiteModelEnum.HazeModel),
                    (false, Cache.CIBConfiguration),
                    (false, Cache.LaserLightInformation),
                    false,
                    cancellationToken);

                if (darkFieldImage == null)
                    continue;

                calibrationOpticService.SetNDFRotary(OpticsChannelModeEnum.CH3_NDF, i);

                double pmtValue = darkFieldImage.Image.GetIntensity().Average;
                Cache.PolarizationPositionNDFPListCH3 = [.. Cache.PolarizationPositionNDFPListCH3, new Point(i, pmtValue)];

                var OriginImageFilePath = Path.Combine(ImageFileDirectory, "CH3", "PInitial" + "__" + i + "__" + $"{Guid.NewGuid():N}.jpg");
                darkFieldImage.Image.Save(OriginImageFilePath);
                Logger.LogHtmlInformation("Angle:" + i, HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    ResultImage = new HtmlImage(OriginImageFilePath)
                }), HtmlLogUniqueId.LoggingHtml());
            }

            var pointWithMinY = Cache.PolarizationPositionNDFPListCH3.OrderBy(p => p.Y).First();
            Cache.PolarizationPositionNDFPCH3 = pointWithMinY.X;

            ResultCollectItemDto.PolarizationPositionNDFSCH1 = Cache.PolarizationPositionNDFSCH1;
            ResultCollectItemDto.PolarizationPositionNDFSCH2 = Cache.PolarizationPositionNDFSCH2;
            ResultCollectItemDto.PolarizationPositionNDFSCH3 = Cache.PolarizationPositionNDFSCH3;
            ResultCollectItemDto.PolarizationPositionNDFPCH1 = Cache.PolarizationPositionNDFPCH1;
            ResultCollectItemDto.PolarizationPositionNDFPCH2 = Cache.PolarizationPositionNDFPCH2;
            ResultCollectItemDto.PolarizationPositionNDFPCH3 = Cache.PolarizationPositionNDFPCH3;

            Logger.LogHtmlInformation("SlopeParam", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                CH3MINY = Cache.PolarizationPositionNDFPListCH3.Min(p => p.Y),
                CH3MINX = Cache.PolarizationPositionNDFPListCH3.OrderBy(p => p.Y).First().X,
                PolarizationPositionNDFPListCH3 = new HtmlPlot2DLinesChart([("PList", Cache.PolarizationPositionNDFPListCH3)], string.Empty)
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    private bool Save(CollectPolarizationDTO itemDto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        Calibration = itemDto.Clone();

        CacheProvider.Set(Calibration, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });
}