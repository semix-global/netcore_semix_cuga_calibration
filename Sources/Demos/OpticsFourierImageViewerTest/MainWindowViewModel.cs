using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Net.Utilities.Graphics;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Editors;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;

namespace OpticsFourierImageViewerTest;

public sealed partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    public partial BitmapImageDrawable BitmapImageDrawable { get; set; }

    [ObservableProperty]
    public partial string ImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial CanvasDocument Document { get; set; }

    [ObservableProperty]
    public partial int ROICount { get; set; } = 4;

    public MainWindowViewModel()
    {
        BitmapImageDrawable = new BitmapImageDrawable();
        Document = new CanvasDocument();

        Document.DefaultModel.Add(BitmapImageDrawable);
    }

    [RelayCommand]
    private async Task OpenImageFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select an image file",
            Filter = Constants.ImageFileExtensionsFilter,
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;

        await Task.Run(() =>
        {
            ImageFilePath = dialog.FileName;

            Document.RunDesign(() => Document.OverlayerModel.RemoveRange([.. Document.OverlayerModel.OfType<BitmapImageROIDrawable>()]));
            BitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(dialog.FileName);

            Document.View.ZoomToFit();
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task EditAllAnchorROIAsync(CancellationToken cancellationToken) => EditROIAsync(
        BitmapImageROIControlPointTypeEnum.All,
        dragMoveTypeEnum: BitmapImageROIDragMoveTypeEnum.All,
        cancellationToken: cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task EditThreeBottomAnchorROIAsync(CancellationToken cancellationToken) => EditROIAsync(
        BitmapImageROIControlPointTypeEnum.XMinYMin |
        BitmapImageROIControlPointTypeEnum.XCenterYMin |
        BitmapImageROIControlPointTypeEnum.XMaxYMin,
        dragMoveTypeEnum: BitmapImageROIDragMoveTypeEnum.X,
        cancellationToken: cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task EditBottomAnchorROIAsync(CancellationToken cancellationToken) => EditROIAsync(
        BitmapImageROIControlPointTypeEnum.XCenterYMin,
        dragMoveTypeEnum: BitmapImageROIDragMoveTypeEnum.None,
        cancellationToken: cancellationToken);

    [RelayCommand]
    private void CancelEditROI()
    {
        EditAllAnchorROICancelCommand.Execute(null);
        EditThreeBottomAnchorROICancelCommand.Execute(null);
        EditBottomAnchorROICancelCommand.Execute(null);
    }

    private async Task EditROIAsync(
        BitmapImageROIControlPointTypeEnum controlPointTypeEnum,
        BitmapImageROIDragMoveTypeEnum dragMoveTypeEnum,
        CancellationToken cancellationToken)
    {
        if (ROICount <= 0) return;

        await Task.Run(async () =>
        {
            var bitmapImage = BitmapImageDrawable.BitmapImage;
            if (bitmapImage?.IsEmpty != false) return;

            var imageWidth = (double)bitmapImage.Width;
            var imageHeight = (double)bitmapImage.Height;
            var roiWidth = imageWidth / (ROICount * 2d - 1d);
            var roiHeight = imageHeight / 2d;

            var roiY = BitmapImageDrawable.Point.Y + imageHeight - roiHeight;

            Document.RunDesign(() =>
            {
                Document.OverlayerModel.RemoveRange([.. Document.OverlayerModel.OfType<BitmapImageROIDrawable>()]);

                for (var i = 0; i < ROICount; i++)
                {
                    var roiX = BitmapImageDrawable.Point.X + i * roiWidth * 2d;

                    Document.OverlayerModel.Add(new BitmapImageROIDrawable(BitmapImageDrawable)
                    {
                        Rect = new Rect(roiX, roiY, roiWidth, roiHeight),
                        ControlPointTypeEnum = controlPointTypeEnum,
                        IsModified = false
                    });
                }
            });

            var options = new ModifyBitmapImageROIDrawableInputOptions(BitmapImageDrawable)
            {
                BitmapImageROIDragMoveTypeEnum = dragMoveTypeEnum,
                CancellationToken = cancellationToken
            };

            await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, options);
        }, cancellationToken).ConfigureAwait(false);
    }
}