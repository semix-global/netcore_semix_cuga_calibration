using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Optics.CollectPolarization;
using Core.Utilities.SourceGenerators.Attributes;
using MathNet.Numerics;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using System.IO;
using Point = Net.Utilities.Models.Geometries.Point;


namespace CugaCalibration.ViewModels.Optics;

[IOCAppService(ServiceType = typeof(CollectionPolarizationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CollectionPolarizationViewModel : CalibrationViewModelBase
{
    #region 界面相关

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select Haze Wafer Position" },
        new() { StepName = "Set Laser Light And CIB Configuration" },
        new() { StepName = "Get S Polarization Position Of CH123" },
        new() { StepName = "Get P Polarization Position Of CH123" }
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

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<CollectPolarizationCache>(cancellationToken);
        Calibration = ApplicationCookieService.GetCalibration<CollectPolarizationDTO>(cancellationToken);

        UpdateEntryStatus(Calibration, cancellationToken);

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

                Save(ResultCollectItemDto, cancellationToken);
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
            : Guard.IsNotNullAndReturn(MicroscopeCalChip.HazeItem).BrightFieldMachinePosition;

        StageViewModel.SetAbsoluteStageTheta(0d);
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
                CIBConfiguration = new HtmlQuote(Cache.CIBConfiguration.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.LaserLightInformations.Contains(Cache.LaserLightInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
    {
        OpticsViewModel.SetPolarizationMode(OpticsPolarizationModeEnum.P);
        return InvokeCalibrateAsync(async () =>
        {
            Cache.PolarizationPositionNDFSListCH1 = [];
            Cache.PolarizationPositionNDFSListCH2 = [];
            Cache.PolarizationPositionNDFSListCH3 = [];

            var cibInfors = ApplicationCookie.CIBInformations.Where(x => x.PMTId == 8).ToArray();
            OpticsViewModel.SetCollectorPolarizationMode(cibInfors[0].ChannelId, OpticsCollectorPolarizationModeEnum.S);
            OpticsViewModel.SetCollectorPolarizationMode(cibInfors[1].ChannelId, OpticsCollectorPolarizationModeEnum.S);
            OpticsViewModel.SetCollectorPolarizationMode(cibInfors[2].ChannelId, OpticsCollectorPolarizationModeEnum.S);

            double[] angleArray = Generate.LinearRange(
                Cache.FindAngleMin,
                Cache.FindAngleInterval,
                Cache.FindAngleMax);
            {
                if (angleArray[^1] < Cache.FindAngleMax)
                    angleArray = [.. angleArray, Cache.FindAngleMax];

                foreach (double i in angleArray)
                {
                    var darkFieldImages = await CIBViewModel.GetPMTImagesAsync(
                        ApplicationCookie.OILowProductivityInformation,
                        StageCoordinateSystemEnum.Dark,
                        StageViewModel.MachineToBrightFieldPosition(Cache.HazeWaferPosition),
                        Cache.ImageWidth,
                        cibInfors,
                        (false, CalChipSiteModelEnum.HazeModel),
                        (true, OpticsConfiguration: null),
                        (false, Cache.CIBConfiguration),
                        (false, Cache.LaserLightInformation),
                        false,
                        cancellationToken);

                    OpticsViewModel.SetCollectorPolarizationMotorAbsoluteValue(cibInfors[0].ChannelId, i);

                    using var intensity0 = darkFieldImages[0].Image.ToHImage();
                    double pmtValue = intensity0.GetIntensity().Average;
                    Cache.PolarizationPositionNDFSListCH1 = [.. Cache.PolarizationPositionNDFSListCH1, new Point(i, pmtValue)];

                    var originImageFilePath = Path.Combine(ImageFileDirectory, "CH1", "SInitial" + "__" + i + "__" + $"{Guid.NewGuid():N}.jpg");
                    intensity0.Save(originImageFilePath);
                    Logger.LogHtmlInformation("CH1Angle:" + i, HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                    {
                        ResultImage = new HtmlImage(originImageFilePath)
                    }), HtmlLogUniqueId.LoggingHtml());

                    OpticsViewModel.SetCollectorPolarizationMotorAbsoluteValue(cibInfors[1].ChannelId, i);

                    using var intensity1 = darkFieldImages[1].Image.ToHImage();
                    pmtValue = intensity1.GetIntensity().Average;
                    Cache.PolarizationPositionNDFSListCH2 = [.. Cache.PolarizationPositionNDFSListCH2, new Point(i, pmtValue)];

                    originImageFilePath = Path.Combine(ImageFileDirectory, "CH2", "SInitial" + "__" + i + "__" + $"{Guid.NewGuid():N}.jpg");
                    intensity1.Save(originImageFilePath);
                    Logger.LogHtmlInformation("CH2Angle:" + i, HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                    {
                        ResultImage = new HtmlImage(originImageFilePath)
                    }), HtmlLogUniqueId.LoggingHtml());


                    OpticsViewModel.SetCollectorPolarizationMotorAbsoluteValue(cibInfors[2].ChannelId, i);

                    using var intensity2 = darkFieldImages[2].Image.ToHImage();
                    pmtValue = intensity2.GetIntensity().Average;
                    Cache.PolarizationPositionNDFSListCH3 = [.. Cache.PolarizationPositionNDFSListCH3, new Point(i, pmtValue)];

                    originImageFilePath = Path.Combine(ImageFileDirectory, "CH3", "SInitial" + "__" + i + "__" + $"{Guid.NewGuid():N}.jpg");
                    intensity2.Save(originImageFilePath);
                    Logger.LogHtmlInformation("CH3Angle:" + i, HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                    {
                        ResultImage = new HtmlImage(originImageFilePath)
                    }), HtmlLogUniqueId.LoggingHtml());
                }
            }

            var pointWithMinY = Cache.PolarizationPositionNDFSListCH1.Where(p => p.X > 20).OrderBy(p => p.Y).First();
            Cache.PolarizationPositionNDFSCH1 = pointWithMinY.X;

            pointWithMinY = Cache.PolarizationPositionNDFSListCH2.Where(p => p.X > 20).OrderBy(p => p.Y).First();
            Cache.PolarizationPositionNDFSCH2 = pointWithMinY.X;

            pointWithMinY = Cache.PolarizationPositionNDFSListCH3.Where(p => p.X > 20).OrderBy(p => p.Y).First();
            Cache.PolarizationPositionNDFSCH3 = pointWithMinY.X;

            double[] values = [Cache.PolarizationPositionNDFSCH1, Cache.PolarizationPositionNDFSCH2, Cache.PolarizationPositionNDFSCH3];
            Cache.FindAngleMin = values.Average() - 30;
            Cache.FindAngleMax = values.Average() + 30;

            Logger.LogHtmlInformation("SlopeParam", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                CH1MINY = Cache.PolarizationPositionNDFSListCH1.Where(p => p.X > 20).Min(p => p.Y),
                CH1MINX = Cache.PolarizationPositionNDFSListCH1.Where(p => p.X > 20).OrderBy(p => p.Y).First().X,
                CH2MINY = Cache.PolarizationPositionNDFSListCH2.Where(p => p.X > 20).Min(p => p.Y),
                CH2MINX = Cache.PolarizationPositionNDFSListCH2.Where(p => p.X > 20).OrderBy(p => p.Y).First().X,
                CH3MINY = Cache.PolarizationPositionNDFSListCH3.Where(p => p.X > 20).Min(p => p.Y),
                CH3MINX = Cache.PolarizationPositionNDFSListCH3.Where(p => p.X > 20).OrderBy(p => p.Y).First().X,
                PolarizationPositionNDFSListCH1 = new HtmlPlot2DLinesChart([("SList", Cache.PolarizationPositionNDFSListCH1)], string.Empty),
                PolarizationPositionNDFSListCH2 = new HtmlPlot2DLinesChart([("SList", Cache.PolarizationPositionNDFSListCH2)], string.Empty),
                PolarizationPositionNDFSListCH3 = new HtmlPlot2DLinesChart([("SList", Cache.PolarizationPositionNDFSListCH3)], string.Empty)
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3Async(CancellationToken cancellationToken)
    {
        OpticsViewModel.SetPolarizationMode(OpticsPolarizationModeEnum.S);
        return InvokeCalibrateAsync(async () =>
        {
            Cache.PolarizationPositionNDFPListCH1 = [];
            Cache.PolarizationPositionNDFPListCH2 = [];
            Cache.PolarizationPositionNDFPListCH3 = [];

            var cibInfors = ApplicationCookie.CIBInformations.Where(x => x.PMTId == 8).ToArray();
            OpticsViewModel.SetCollectorPolarizationMode(cibInfors[0].ChannelId, OpticsCollectorPolarizationModeEnum.P);
            OpticsViewModel.SetCollectorPolarizationMode(cibInfors[1].ChannelId, OpticsCollectorPolarizationModeEnum.P);
            OpticsViewModel.SetCollectorPolarizationMode(cibInfors[2].ChannelId, OpticsCollectorPolarizationModeEnum.P);

            double[] angleArray = Generate.LinearRange(
                Cache.FindAngleMin,
                Cache.FindAngleInterval,
                Cache.FindAngleMax);

            if (angleArray[^1] < Cache.FindAngleMax)
                angleArray = [.. angleArray, Cache.FindAngleMax];

            foreach (double i in angleArray)
            {
                var darkFieldImages = await CIBViewModel.GetPMTImagesAsync(
                    ApplicationCookie.OILowProductivityInformation,
                    StageCoordinateSystemEnum.Dark,
                    StageViewModel.MachineToBrightFieldPosition(Cache.HazeWaferPosition),
                    Cache.ImageWidth,
                    cibInfors,
                    (false, CalChipSiteModelEnum.HazeModel),
                    (true, OpticsConfiguration: null),
                    (false, Cache.CIBConfiguration),
                    (false, Cache.LaserLightInformation),
                    false,
                    cancellationToken);

                OpticsViewModel.SetCollectorPolarizationMotorAbsoluteValue(cibInfors[0].ChannelId, i);

                using var intensity0 = darkFieldImages[0].Image.ToHImage();
                double pmtValue = intensity0.GetIntensity().Average;
                Cache.PolarizationPositionNDFPListCH1 = [.. Cache.PolarizationPositionNDFPListCH1, new Point(i, pmtValue)];

                var originImageFilePath = Path.Combine(ImageFileDirectory, "CH1", "PInitial" + "__" + i + "__" + $"{Guid.NewGuid():N}.jpg");
                intensity0.Save(originImageFilePath);
                Logger.LogHtmlInformation("Angle:" + i, HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    ResultImage = new HtmlImage(originImageFilePath)
                }), HtmlLogUniqueId.LoggingHtml());


                OpticsViewModel.SetCollectorPolarizationMotorAbsoluteValue(cibInfors[1].ChannelId, i);

                using var intensity1 = darkFieldImages[1].Image.ToHImage();
                pmtValue = intensity1.GetIntensity().Average;
                Cache.PolarizationPositionNDFPListCH2 = [.. Cache.PolarizationPositionNDFPListCH2, new Point(i, pmtValue)];

                originImageFilePath = Path.Combine(ImageFileDirectory, "CH2", "PInitial" + "__" + i + "__" + $"{Guid.NewGuid():N}.jpg");
                intensity1.Save(originImageFilePath);
                Logger.LogHtmlInformation("Angle:" + i, HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    ResultImage = new HtmlImage(originImageFilePath)
                }), HtmlLogUniqueId.LoggingHtml());


                OpticsViewModel.SetCollectorPolarizationMotorAbsoluteValue(cibInfors[2].ChannelId, i);

                using var intensity2 = darkFieldImages[2].Image.ToHImage();
                pmtValue = intensity2.GetIntensity().Average;
                Cache.PolarizationPositionNDFPListCH3 = [.. Cache.PolarizationPositionNDFPListCH3, new Point(i, pmtValue)];

                originImageFilePath = Path.Combine(ImageFileDirectory, "CH3", "PInitial" + "__" + i + "__" + $"{Guid.NewGuid():N}.jpg");
                intensity2.Save(originImageFilePath);
                Logger.LogHtmlInformation("Angle:" + i, HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    ResultImage = new HtmlImage(originImageFilePath)
                }), HtmlLogUniqueId.LoggingHtml());
            }

            var pointWithMinY = Cache.PolarizationPositionNDFPListCH1.Where(p => p.X > 20).OrderBy(p => p.Y).First();
            Cache.PolarizationPositionNDFPCH1 = pointWithMinY.X;
            pointWithMinY = Cache.PolarizationPositionNDFPListCH2.Where(p => p.X > 20).OrderBy(p => p.Y).First();
            Cache.PolarizationPositionNDFPCH2 = pointWithMinY.X;
            pointWithMinY = Cache.PolarizationPositionNDFPListCH3.Where(p => p.X > 20).OrderBy(p => p.Y).First();
            Cache.PolarizationPositionNDFPCH3 = pointWithMinY.X;

            double[] values = [Cache.PolarizationPositionNDFPCH1, Cache.PolarizationPositionNDFPCH2, Cache.PolarizationPositionNDFPCH3];
            Cache.FindAngleMin = values.Average() - 30;
            Cache.FindAngleMax = values.Average() + 30;

            Logger.LogHtmlInformation("SlopeParam", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                CH1MINY = Cache.PolarizationPositionNDFPListCH1.Where(p => p.X > 20).Min(p => p.Y),
                CH1MINX = Cache.PolarizationPositionNDFPListCH1.Where(p => p.X > 20).OrderBy(p => p.Y).First().X,
                CH2MINY = Cache.PolarizationPositionNDFPListCH2.Where(p => p.X > 20).Min(p => p.Y),
                CH2MINX = Cache.PolarizationPositionNDFPListCH2.Where(p => p.X > 20).OrderBy(p => p.Y).First().X,
                CH3MINY = Cache.PolarizationPositionNDFPListCH3.Where(p => p.X > 20).Min(p => p.Y),
                CH3MINX = Cache.PolarizationPositionNDFPListCH3.Where(p => p.X > 20).OrderBy(p => p.Y).First().X,
                PolarizationPositionNDFPListCH1 = new HtmlPlot2DLinesChart([("PList", Cache.PolarizationPositionNDFPListCH1)], string.Empty),
                PolarizationPositionNDFPListCH2 = new HtmlPlot2DLinesChart([("PList", Cache.PolarizationPositionNDFPListCH2)], string.Empty),
                PolarizationPositionNDFPListCH3 = new HtmlPlot2DLinesChart([("PList", Cache.PolarizationPositionNDFPListCH3)], string.Empty)
            }), HtmlLogUniqueId.LoggingHtml());

            ResultCollectItemDto.PolarizationPositionNDFSCH1 = Cache.PolarizationPositionNDFSCH1;
            ResultCollectItemDto.PolarizationPositionNDFSCH2 = Cache.PolarizationPositionNDFSCH2;
            ResultCollectItemDto.PolarizationPositionNDFSCH3 = Cache.PolarizationPositionNDFSCH3;
            ResultCollectItemDto.PolarizationPositionNDFPCH1 = Cache.PolarizationPositionNDFPCH1;
            ResultCollectItemDto.PolarizationPositionNDFPCH2 = Cache.PolarizationPositionNDFPCH2;
            ResultCollectItemDto.PolarizationPositionNDFPCH3 = Cache.PolarizationPositionNDFPCH3;

            return true;
        });
    }

    private void Save(CollectPolarizationDTO itemDto, CancellationToken cancellationToken)
    {
        InvokeSave(update =>
        {
            update(itemDto);
            update(Cache);

            Calibration = itemDto.Clone();

            ApplicationCookieService.SetCalibration(Calibration, cancellationToken);
            ApplicationCookieService.SetCache(Cache, cancellationToken);
        });
    }

    public override void UpdateEntryStatus(CalibrationDTOBase calibration, CancellationToken cancellationToken)
    {
        var temp = Guard.IsAssignableToTypeAndReturn<CollectPolarizationDTO>(calibration);
        var status = Entry.Status;

        Calibration = temp;

        status.TotalCalibrationCount = 1;
        status.CalibratedCount = Calibration.IsCalibrated ? 1 : 0;
        status.VerifiedCount = Calibration.IsVerified ? 1 : 0;
        status.Details = [];
    }
}