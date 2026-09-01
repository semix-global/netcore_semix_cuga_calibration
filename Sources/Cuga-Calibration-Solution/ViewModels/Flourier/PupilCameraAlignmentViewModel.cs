using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models;
using Core.Models.Models.Common.Fourier;
using Core.Models.Models.Fourier.CameraAlignment;
using Core.Models.Models.Microscope.CalChip;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.OpticsFourierImageViewer.WPF;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Editors;
using Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using System.IO;
using Core.Models.Models.Fourier.PupilCameraAlignment;
using BitmapImage = Net.Utilities.Graphics.Primitives.Medias.Imaging.BitmapImage;

namespace CugaCalibration.ViewModels.Flourier;

[IOCAppService(ServiceType = typeof(PupilCameraAlignmentViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class PupilCameraAlignmentViewModel : CalibrationViewModelBase<PupilCameraAlignmentCache>
{
    private const int FourierImageWidth = 100;

    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select Haze Wafer Position" },
        new() { StepName = "Get And Save CH1 Image" },
        new() { StepName = "Get And Save CH2 Image" },
        new() { StepName = "Get And Save CH3 Image" }
    ];

    #region 界面相关

    public OpticsFourierImageDocument DocumentCh1 { get; } = new();

    public OpticsFourierImageDocument DocumentCh2 { get; } = new();

    public OpticsFourierImageDocument DocumentCh3 { get; } = new();

    public OpticsFourierImageDocument ReviewDocumentCh1 { get; } = new();

    public OpticsFourierImageDocument ReviewDocumentCh2 { get; } = new();

    public OpticsFourierImageDocument ReviewDocumentCh3 { get; } = new();

    [ObservableProperty]
    public partial Point SxPos { get; set; }

    [ObservableProperty]
    public partial PupilCameraAlignmentDTO CalibratingItem { get; set; } = new();

    [ObservableProperty]
    public partial PupilCameraAlignmentDTO Review { get; set; } = new();

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial PupilCameraAlignmentCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial PupilCameraAlignmentDTO Calibration { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);
        Cache = ApplicationCookieService.GetCache<PupilCameraAlignmentCache>(cancellationToken);
        Calibration = ApplicationCookieService.GetCalibration<PupilCameraAlignmentDTO>(cancellationToken);

        if (Cache.HazeFindBFMachinePosition == Point.Origin)
            Cache.HazeFindBFMachinePosition = Guard.IsNotNullAndReturn(MicroscopeCalChip.HazeItem).BrightFieldMachinePosition;

        UpdateEntryStatus(Calibration, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        CalibratingItem = new PupilCameraAlignmentDTO
        {
            IsRequiredSelfCheck = Calibration.IsRequiredSelfCheck
        };

        ClearDocument(DocumentCh1);
        ClearDocument(DocumentCh2);
        ClearDocument(DocumentCh3);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (Calibration.IsCalibrated == false) return false;

        Review = Calibration.Clone();
        RefreshDocument(ReviewDocumentCh1, Review.ChannelId1);
        RefreshDocument(ReviewDocumentCh2, Review.ChannelId2);
        RefreshDocument(ReviewDocumentCh3, Review.ChannelId3);

        return true;
    }

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        return true;
    }

    protected override Task<bool> CancelingAsync() => Task.FromResult(true);

    #endregion 控制校准业务

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var isParamValid = ApplicationCookie.ProductivityInformations.Contains(Cache.ProductivityInformation)
                               && ApplicationCookie.MicroscopeLensInformations.Contains(Cache.MicroscopeLensInformation)
                               && ApplicationCookie.LaserLightInformations.Contains(Cache.LaserLightInformation)
                               && ApplicationCookie.CIBInformations.Contains(Cache.CIBInformation);
            if (isParamValid == false)
            {
                DialogWindowProvider.ShowDialog("Please select valid productivity, microscope, laser and CIB information!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            FourierViewModel.SetFFHome(FFCH.Ch1);
            FourierViewModel.SetFFHome(FFCH.Ch2);
            FourierViewModel.SetFFHome(FFCH.Ch3_X);
            FourierViewModel.SetFFHome(FFCH.Ch3_Y);

            if (Cache.HazeFindBFMachinePosition == Point.Origin)
                Cache.HazeFindBFMachinePosition = Guard.IsNotNullAndReturn(MicroscopeCalChip.HazeItem).BrightFieldMachinePosition;

            SxPos = StageViewModel.MachineToBrightFieldPosition(Cache.HazeFindBFMachinePosition);
            AfViewModel.ToggleDarkFieldEnable(true);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.MicroscopeLensInformation,
                Cache.LaserLightInformation,
                Cache.CIBInformation,
                OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous()),
                Cache.HazeFindBFMachinePosition,
                BrightFieldPosition = SxPos
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken) => ValidateChannelAsync(
        CalibratingItem.ChannelId1,
        DocumentCh1,
        isLastChannel: false,
        cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken) => ValidateChannelAsync(
        CalibratingItem.ChannelId2,
        DocumentCh2,
        isLastChannel: false,
        cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3Async(CancellationToken cancellationToken) => ValidateChannelAsync(
        CalibratingItem.ChannelId3,
        DocumentCh3,
        isLastChannel: true,
        cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task AcquireChannel1ImageAsync(CancellationToken cancellationToken) => AcquireChannelImageAsync(
        CalibratingItem.ChannelId1,
        DocumentCh1,
        cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task AcquireChannel2ImageAsync(CancellationToken cancellationToken) => AcquireChannelImageAsync(
        CalibratingItem.ChannelId2,
        DocumentCh2,
        cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task AcquireChannel3ImageAsync(CancellationToken cancellationToken) => AcquireChannelImageAsync(
        CalibratingItem.ChannelId3,
        DocumentCh3,
        cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task EditChannel1ROIAsync(CancellationToken cancellationToken) => EditChannelROIAsync(DocumentCh1, cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task EditChannel2ROIAsync(CancellationToken cancellationToken) => EditChannelROIAsync(DocumentCh2, cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task EditChannel3ROIAsync(CancellationToken cancellationToken) => EditChannelROIAsync(DocumentCh3, cancellationToken);

    private Task<bool> ValidateChannelAsync(
        PupilCameraAlignmentDTOItem item,
        OpticsFourierImageDocument document,
        bool isLastChannel,
        CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            if (TrySynchronizeChannel(item, document) == false)
            {
                DialogWindowProvider.ShowDialog($"Please acquire CH{item.ChannelId} image and select a valid ROI before continuing!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                item.ChannelId,
                item.ChannelImageFilePath,
                item.ImageROI,
                Image = new HtmlImage(item.ChannelImageFilePath)
            }), HtmlLogUniqueId.LoggingHtml());

            if (isLastChannel == false) return true;

            CalibratingItem.IsCalibrated = true;
            CalibratingItem.IsVerified = false;

            return Save(CalibratingItem, cancellationToken);
        });
    }

    private async Task AcquireChannelImageAsync(
        PupilCameraAlignmentDTOItem item,
        OpticsFourierImageDocument document,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bytes = await Task.Run(
                () => FourierViewModel.GetFFReviewImgForTrigger(
                    item.ChannelId - 1,
                    Cache.ProductivityInformation,
                    Cache.LaserLightInformation.Level,
                    SxPos,
                    FourierImageWidth),
                cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var bitmapImage = BytesToBitmapImage(bytes);
            if (bitmapImage is null)
            {
                DialogWindowProvider.ShowDialog($"CH{item.ChannelId} Fourier image is empty!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            var imageFilePath = Path.Combine(ImageFileDirectory, $"CH{item.ChannelId}", $"{Guid.NewGuid():N}.jpg");
            bitmapImage.SaveImage(imageFilePath);

            item.ChannelImageFilePath = imageFilePath;
            item.ImageROI = new Rect(Point.Origin, bitmapImage.Size);

            RefreshDocument(document, item, bitmapImage);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Acquire CH{ChannelId} Fourier image failed", item.ChannelId);
            DialogWindowProvider.ShowDialog($"Acquire CH{item.ChannelId} Fourier image failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    private async Task EditChannelROIAsync(OpticsFourierImageDocument document, CancellationToken cancellationToken)
    {
        BitmapImageDrawable? bitmapImageDrawable;
        using (document.View.Sync.EnterScope()) bitmapImageDrawable = document.ImageModel.SingleOrDefault();

        if (bitmapImageDrawable?.BitmapImage?.IsEmpty != false)
        {
            DialogWindowProvider.ShowDialog("Please acquire the Fourier image before editing ROI!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        var options = new ModifyBitmapImageROIDrawableInputOptions(bitmapImageDrawable)
        {
            CancellationToken = cancellationToken
        };

        await ModifyBitmapImageROIDrawableGetterEditor
            .RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(document.Edit, options)
            .ConfigureAwait(false);
    }

    private static bool TrySynchronizeChannel(PupilCameraAlignmentDTOItem item, OpticsFourierImageDocument document)
    {
        BitmapImageDrawable? bitmapImageDrawable;
        BitmapImageROIDrawable? bitmapImageROIDrawable;

        using (document.View.Sync.EnterScope())
        {
            bitmapImageDrawable = document.ImageModel.SingleOrDefault();
            bitmapImageROIDrawable = document.ROIModel.SingleOrDefault();
        }

        if (bitmapImageDrawable?.BitmapImage?.IsEmpty != false
            || bitmapImageROIDrawable is null
            || string.IsNullOrWhiteSpace(item.ChannelImageFilePath)
            || File.Exists(item.ChannelImageFilePath) == false)
            return false;

        var imageROI = bitmapImageDrawable.CartesianCoordinateToImageCoordinate(bitmapImageROIDrawable.Rect);
        var imageBounds = new Rect(Point.Origin, bitmapImageDrawable.BitmapImage.Size);

        if (imageROI.IsEmpty
            || imageROI.Width <= 0d
            || imageROI.Height <= 0d
            || imageROI.XMin < imageBounds.XMin
            || imageROI.YMin < imageBounds.YMin
            || imageROI.XMax > imageBounds.XMax
            || imageROI.YMax > imageBounds.YMax)
            return false;

        item.ImageROI = imageROI;

        return true;
    }

    #endregion 校准

    #region Review

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(() =>
        {
            if (Review.IsCalibrated == false) return false;

            Review.IsVerified = true;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                CH1 = new { Review.ChannelId1.ChannelImageFilePath, Review.ChannelId1.ImageROI },
                CH2 = new { Review.ChannelId2.ChannelImageFilePath, Review.ChannelId2.ImageROI },
                CH3 = new { Review.ChannelId3.ChannelImageFilePath, Review.ChannelId3.ImageROI }
            }), HtmlLogUniqueId.LoggingHtml());

            var result = Save(Review, cancellationToken);
            if (result) DialogWindowProvider.ShowDialog("Verify Success!");

            return result;
        }).ConfigureAwait(false);
    }

    #endregion Review

    #region 私有方法

    private bool Save(PupilCameraAlignmentDTO dto, CancellationToken cancellationToken)
    {
        var result = InvokeSave(update =>
        {
            update(dto);
            update(Cache);

            Calibration = dto.Clone();

            ApplicationCookieService.SetCalibration(Calibration, cancellationToken);
            ApplicationCookieService.SetCache(Cache, cancellationToken);
        });

        if (result) UpdateEntryStatus(Calibration, cancellationToken);

        return result;
    }

    private static BitmapImage? BytesToBitmapImage(byte[] bytes) => bytes.Length == 0
        ? null
        : new BitmapImage(bytes, isCopy: true);

    private static void RefreshDocument(
        OpticsFourierImageDocument document,
        PupilCameraAlignmentDTOItem item,
        BitmapImage? bitmapImage = null)
    {
        if (bitmapImage is null)
        {
            if (string.IsNullOrWhiteSpace(item.ChannelImageFilePath) || File.Exists(item.ChannelImageFilePath) == false)
            {
                ClearDocument(document);
                return;
            }

            bitmapImage = BitmapHelper.OpenImage(item.ChannelImageFilePath);
        }

        var bitmapImageDrawable = new BitmapImageDrawable
        {
            BitmapImage = bitmapImage
        };
        var imageBounds = new Rect(Point.Origin, bitmapImage.Size);
        var imageROI = item.ImageROI.IsEmpty ? imageBounds : item.ImageROI.ClampToBounds(imageBounds);
        var bitmapImageROIDrawable = new BitmapImageROIDrawable(
            bitmapImageDrawable,
            rect => item.ImageROI = bitmapImageDrawable.CartesianCoordinateToImageCoordinate(rect))
        {
            Rect = bitmapImageDrawable.ImageCoordinateToCartesianCoordinate(imageROI),
            IsModified = false
        };

        document.RunDesign(() =>
        {
            foreach (var drawable in document.ImageModel) drawable.BitmapImage = null;

            document.ROIModel.Clear();
            document.ImageModel.Clear();
            document.ImageModel.Add(bitmapImageDrawable);
            document.ROIModel.Add(bitmapImageROIDrawable);
        });

        document.View.ZoomToFit();
    }

    private static void ClearDocument(OpticsFourierImageDocument document)
    {
        document.RunDesign(() =>
        {
            foreach (var drawable in document.ImageModel) drawable.BitmapImage = null;

            document.ROIModel.Clear();
            document.ImageModel.Clear();
        });
    }

    public override void UpdateEntryStatus(CalibrationDTOBase calibration, CancellationToken cancellationToken)
    {
        Calibration = Guard.IsAssignableToTypeAndReturn<PupilCameraAlignmentDTO>(calibration);

        var status = Entry.Status;
        status.TotalCalibrationCount = 1;
        status.CalibratedCount = Calibration.IsCalibrated ? 1 : 0;
        status.VerifiedCount = Calibration.IsVerified ? 1 : 0;
        status.Details = [];
    }

    #endregion 私有方法
}
