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
            BitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(dialog.FileName);

            Document.RunDesign(ClearRectROIs);
            Document.View.ZoomToFit();
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task EditAllAnchorROIsAsync(CancellationToken cancellationToken)
    {
        return EditROIsAsync(
            BitmapImageROIControlPointTypeEnum.All,
            dragMoveTypeEnum: BitmapImageROIDragMoveTypeEnum.All,
            cancellationToken: cancellationToken);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task EditBottomAnchorROIAsync(CancellationToken cancellationToken)
    {
        return EditROIsAsync(
            BitmapImageROIControlPointTypeEnum.XCenterYMin,
            dragMoveTypeEnum: BitmapImageROIDragMoveTypeEnum.None,
            cancellationToken: cancellationToken);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task EditThreeBottomAnchorROIsAsync(CancellationToken cancellationToken)
    {
        return EditROIsAsync(
            BitmapImageROIControlPointTypeEnum.XMinYMin |
            BitmapImageROIControlPointTypeEnum.XCenterYMin |
            BitmapImageROIControlPointTypeEnum.XMaxYMin,
            dragMoveTypeEnum: BitmapImageROIDragMoveTypeEnum.X,
            cancellationToken: cancellationToken);
    }

    [RelayCommand]
    private void CancelEditROI()
    {
        EditAllAnchorROIsCancelCommand.Execute(null);
        EditBottomAnchorROICancelCommand.Execute(null);
        EditThreeBottomAnchorROIsCancelCommand.Execute(null);
    }

    private async Task EditROIsAsync(
        BitmapImageROIControlPointTypeEnum controlPointTypeEnum,
        BitmapImageROIDragMoveTypeEnum dragMoveTypeEnum,
        CancellationToken cancellationToken)
    {
        var roiCount = ROICount;
        if (roiCount <= 0) return;

        await Task.Run(async () =>
        {
            var bitmapImage = BitmapImageDrawable.BitmapImage;
            if (bitmapImage?.IsEmpty != false) return;

            var imageWidth = (double)bitmapImage.Width;
            var imageHeight = (double)bitmapImage.Height;
            var roiWidth = imageWidth / (roiCount * 2d - 1d);
            var roiHeight = imageHeight / 2d;
            // 画布使用笛卡尔坐标，图片顶部对应 Point.Y + imageHeight。
            var roiY = BitmapImageDrawable.Point.Y + imageHeight - roiHeight;

            Document.RunDesign(() =>
            {
                // 矩形宽度与矩形之间的间距相同，因此 N 个矩形正好均匀铺满图片宽度。
                ClearRectROIs();

                for (var i = 0; i < roiCount; i++)
                {
                    var roiX = BitmapImageDrawable.Point.X + i * roiWidth * 2d;

                    Document.OverlayerModel.Add(new BitmapImageROIDrawable
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

    private void ClearRectROIs()
    {
        Document.OverlayerModel.RemoveRange(Document.OverlayerModel.OfType<BitmapImageROIDrawable>().ToArray());
    }
}