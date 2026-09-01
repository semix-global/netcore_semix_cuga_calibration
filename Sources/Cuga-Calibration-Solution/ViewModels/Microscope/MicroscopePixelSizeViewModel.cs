using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.PixelSize;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using System.Runtime.CompilerServices;

namespace CugaCalibration.ViewModels.Microscope;

[IOCAppService(ServiceType = typeof(MicroscopePixelSizeViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopePixelSizeViewModel : CalibrationViewModelBase<MicroscopePixelSizeCache>
{
    #region 属性

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.MicroscopeLensInformation.LensName);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.MicroscopeLensInformation.LensName);

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select a lens" },
        new() { StepName = "Find Position" },
        new() { StepName = "Find Pixel Size" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial MicroscopePixelSizeDTO CalibratingItem { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopePixelSizeDTOItem? SelectedItem { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<MicroscopeLensInformationStatus> CalibratingStatuses { get; set; } = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    public partial IReadOnlyList<MicroscopePixelSizeDTO> Reviews { get; set; } = [];

    [ObservableProperty]
    public partial MicroscopePixelSizeDTO? SelectedReviewItem { get; set; }

    [ObservableProperty]
    public partial List<(string Microscope, Size OldPixelSize, Size NewPixelSize, Size OffsetPixelSize)> ReviewResultList { get; set; } = [];

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial MicroscopePixelSizeCache Cache { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial MicroscopePixelSizeDTO[] Calibrations { get; set; } = [];

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务重载

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<MicroscopePixelSizeCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<MicroscopePixelSizeDTO>(cancellationToken);

        UpdateEntryStatus(Unsafe.As<CalibrationDTOBase[]>(Calibrations), cancellationToken);

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
                .Where(t => t.IsCalibrated)
                .OrderBy(t => t.MicroscopeLensInformation.ObjectiveMagnification)
                .ThenBy(t => t.MicroscopeLensInformation.LensCode)
        ];

        return Reviews.Count > 0;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        switch (CalibrationStepIndex)
        {
            case 0:
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (Cache.CalChipSiteModelEnum is CalChipSiteModelEnum.DswModel) StageViewModel.SetAbsoluteStageTheta(MicroscopeCalChip.DSWAlignmentDegree);

                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(
                    Cache.CalChipSiteModelEnum switch
                    {
                        CalChipSiteModelEnum.ChuckModel => Cache.Item.FindPosition,
                        CalChipSiteModelEnum.DswModel => Cache.Item.FindPosition == Point.Origin ? StageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.DSWBrightFieldMachineAffinePosition) : Cache.Item.FindPosition,
                        _ => ThrowHelper.ThrowNotSupportedException<Point>("Current CalChip Mode Is Not Supported!")
                    }, Cache.CalChipSiteModelEnum);

                return true;

            case 1:

                return true;

            case 2:

                return true;

            default:
                return false;
        }
    }

    protected override async Task<bool> CancelingAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);

        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

        return true;
    }

    #endregion 控制校准业务重载

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation,
                Cache.CalChipSiteModelEnum
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1Async(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(() =>
        {
            var (isSuccess, errorMessage) = Cache.Verify();
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            Cache.Item.FindPosition = StageViewModel.GetBrightFieldStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
                Cache.Item.FindPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2Async(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(async () =>
        {
            CalibratingItem = new()
            {
                MicroscopeLensInformation = Cache.MicroscopeLensInformation,
                Items = [],
                IsCalibrated = false
            };

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation,
                Cache.Item.FindPosition,
                Cache.AngleThreshold,
                ImageFileDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            if (await GetPixelSizeAsync(cancellationToken).ConfigureAwait(false) == false) return false;

            var averageWidth = CalibratingItem.Items.Average(t => t.PixelSize.Width);
            var averageHeight = CalibratingItem.Items.Average(t => t.PixelSize.Height);
            var averagePixelSize = new Size(averageWidth, averageHeight);

            SelectedItem = CalibratingItem.Items.OrderBy(t => ((Point)t.PixelSize - (Vector)averagePixelSize).ToOriginLength).First();
            CalibratingItem.Result = SelectedItem.Clone();
            CalibratingItem.Result.PixelSize = averagePixelSize;
            CalibratingItem.IsCalibrated = true;
            Guard.IsTrue(Save([CalibratingItem], cancellationToken));

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                CalibratingItem.MicroscopeLensInformation,
                CalibratingItem.Result.PixelSize,
                HtmlTab = new HtmlTab(new
                {
                    OriginImage = new HtmlImage(CalibratingItem.Result.OriginFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    DrawingImage = new HtmlImage(CalibratingItem.Result.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                }),
                Plots = new HtmlContainer(CalibratingItem.PlotDataSource.GetAllHtmlPlot2DLinesCharts())
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> VerifyAsync(CancellationToken cancellationToken)
    {
        return await InvokeVerifyAsync(async () =>
        {
            CalibratingItem.Items = [];
            if (SelectedReviewItem is null)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            if (Cache.CalChipSiteModelEnum is CalChipSiteModelEnum.DswModel)
                StageViewModel.SetAbsoluteStageTheta(MicroscopeCalChip.DSWAlignmentDegree);

            SelectedReviewItem.IsVerified = false;
            Cache.MicroscopeLensInformation = SelectedReviewItem.MicroscopeLensInformation;

            await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
            Logger.LogHtmlInformation($"{Cache.MicroscopeLensInformation.LensName}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation,
                Cache.Item.FindPosition,
                Cache.Threshold,
                ImageFileDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            if (await GetPixelSizeAsync(cancellationToken, 1).ConfigureAwait(false) == false) return false;

            var verifyPixelSize = CalibratingItem.Items[0].PixelSize;
            var error = (Size)((Vector)verifyPixelSize - (Vector)SelectedReviewItem.Result.PixelSize);
            ReviewResultList.Add((Cache.MicroscopeLensInformation.LensName, SelectedReviewItem.Result.PixelSize, verifyPixelSize, error));
            var result = error.DiagonalLength < Cache.Threshold.DiagonalLength;

            Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                CalibrationPixelSize = SelectedReviewItem.Result.PixelSize,
                VerifyPixelSize = verifyPixelSize,
                Error = error
            }), HtmlLogUniqueId.LoggingHtml());

            SelectedReviewItem.IsVerified = result;

            Guard.IsTrue(Save([SelectedReviewItem], cancellationToken));

            DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}, verify PixelSize: ({verifyPixelSize}) Calibration PixelSize: ({SelectedReviewItem.Result.PixelSize}) Error: ({error})", DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        }).ConfigureAwait(false);
    }

    private async Task<bool> GetPixelSizeAsync(CancellationToken cancellationToken, int repeatCount = 10)
    {
        var detectImageDirectory = ImageFileDirectory;

        await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
        StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.Item.FindPosition, Cache.CalChipSiteModelEnum);

        foreach (var times in Enumerable.Range(1, repeatCount))
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var image = ReviewViewModel.GetBrightFieldImage();

            var pixelSize = CalibrationAlgorithmService.GetPixelSize(image, Cache.Item.AlgorithmStandardMaskSquareSizeEnum.ToSize(), HtmlLogUniqueId, out var drawingImage, out var angle);
            if (Math.Abs(angle) > Cache.AngleThreshold)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment($"The current horizontal angle exceeds the limit! Angle:{angle}"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            using var _2 = drawingImage;
            var microscopePixelSizeItem = new MicroscopePixelSizeDTOItem
            {
                Index = times - 1,
                OriginFilePath = $"{detectImageDirectory}\\PixelSize({pixelSize})_Times({times})_Guid({HtmlLogUniqueId}).jpg",
                FilePath = $"{detectImageDirectory}\\PixelSize({pixelSize})_Drawing_Times({times})_Guid({HtmlLogUniqueId}).jpg",
                PixelSize = pixelSize
            };

            image.SaveImage(microscopePixelSizeItem.OriginFilePath);
            drawingImage.SaveImage(microscopePixelSizeItem.FilePath);

            Logger.LogHtmlInformation($"Get Pixel Size OK! Time:{times}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
            {
                Cache.MicroscopeLensInformation,
                HorizontalAngle = angle,
                Cache.Item.AlgorithmStandardMaskSquareSizeEnum,
                microscopePixelSizeItem.PixelSize,
                HtmlTab = new HtmlTab(new
                {
                    OriginImage = new HtmlImage(microscopePixelSizeItem.OriginFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    DrawingImage = new HtmlImage(microscopePixelSizeItem.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.Items = [.. CalibratingItem.Items, microscopePixelSizeItem];
        }

        return true;
    }

    private bool Save(IReadOnlyList<MicroscopePixelSizeDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                dto.Clone(),
                .. Calibrations.Where(t => t.MicroscopeLensInformation != dto.MicroscopeLensInformation)
            ];
        }

        ApplicationCookieService.SetCalibrations(Calibrations, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var temps = Guard.IsAssignableToTypeAndReturn<MicroscopePixelSizeDTO[]>(calibrations);
        var status = Entry.Status;

        CalibratingStatuses =
        [
            .. ApplicationCookie.MicroscopeLensInformations.Select(t => new MicroscopeLensInformationStatus { SelectedItem = t, IsCalibrated = false })
        ];

        Calibrations =
        [
            .. temps
                .Where(t => ApplicationCookie.MicroscopeLensInformations.Contains(t.MicroscopeLensInformation))
                .DistinctBy(t => t.MicroscopeLensInformation)
                .Select(t =>
                {
                    CalibratingStatuses.Single(tt => tt.SelectedItem == t.MicroscopeLensInformation).IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        status.TotalCalibrationCount = ApplicationCookie.MicroscopeLensInformations.Count;
        status.CalibratedCount = Calibrations.Count(t => t.IsCalibrated);
        status.VerifiedCount = Calibrations.Count(t => t.IsVerified);
        status.Details =
        [
            .. ApplicationCookie.MicroscopeLensInformations.Select(productivityInformation =>
            {
                var item = Calibrations.SingleOrDefault(t => t.MicroscopeLensInformation == productivityInformation);

                return new CalibrationViewModelStatus.Detail(
                    productivityInformation.ToString(),
                    item?.IsCalibrated,
                    item?.IsVerified);
            })
        ];
    }

    #endregion 校准
}