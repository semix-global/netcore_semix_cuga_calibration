using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF;
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
    public partial OpticsFourierImageDocument Document { get; set; }

    [ObservableProperty]
    public partial BitmapImageROIDrawable[] BitmapImageROIDrawables { get; set; } = [];

    [ObservableProperty]
    public partial int ROICount { get; set; } = 8;

    [ObservableProperty]
    public partial bool IsOutsideImageAllowed { get; set; }

    public MainWindowViewModel()
    {
        BitmapImageDrawable = new BitmapImageDrawable();
        Document = new OpticsFourierImageDocument();

        Document.RunDesign(() => Document.ImageModel.Add(BitmapImageDrawable));
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
            BitmapImageROIDrawables = [];

            Document.RunDesign(() => Document.ROIModel.Clear());
            BitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(dialog.FileName);

            Document.View.ZoomToFit();
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task EditAllAnchorROIAsync(CancellationToken cancellationToken) => EditROIAsync(
        BitmapImageROIResizeJoystickStateEnum.All,
        dragMoveTypeEnum: BitmapImageROIDragMoveTypeEnum.All,
        true,
        cancellationToken: cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task EditThreeBottomAnchorROIAsync(CancellationToken cancellationToken) => EditROIAsync(
        BitmapImageROIResizeJoystickStateEnum.XMinYMin |
        BitmapImageROIResizeJoystickStateEnum.XCenterYMin |
        BitmapImageROIResizeJoystickStateEnum.XMaxYMin,
        dragMoveTypeEnum: BitmapImageROIDragMoveTypeEnum.X,
        false,
        cancellationToken: cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task EditBottomAnchorROIAsync(CancellationToken cancellationToken) => EditROIAsync(
        BitmapImageROIResizeJoystickStateEnum.XCenterYMin,
        dragMoveTypeEnum: BitmapImageROIDragMoveTypeEnum.None,
        false,
        cancellationToken: cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task EditFixedAspectRatioROIAsync(CancellationToken cancellationToken) => EditROIAsync(
        BitmapImageROIResizeJoystickStateEnum.All,
        dragMoveTypeEnum: BitmapImageROIDragMoveTypeEnum.All,
        true,
        cancellationToken: cancellationToken,
        isAspectRatioFixed: true);

    [RelayCommand]
    private void CancelEditROI()
    {
        EditAllAnchorROICancelCommand.Execute(null);
        EditThreeBottomAnchorROICancelCommand.Execute(null);
        EditBottomAnchorROICancelCommand.Execute(null);
        EditFixedAspectRatioROICancelCommand.Execute(null);
    }

    private async Task EditROIAsync(
        BitmapImageROIResizeJoystickStateEnum resizeJoystickStateEnum,
        BitmapImageROIDragMoveTypeEnum dragMoveTypeEnum,
        bool isDeleteEnabled,
        CancellationToken cancellationToken,
        bool isAspectRatioFixed = false)
    {
        var roiCount = isAspectRatioFixed ? 3 : ROICount;
        if (roiCount <= 0) return;

        var isOutsideImageAllowed = IsOutsideImageAllowed;

        await Task.Run(async () =>
        {
            var bitmapImage = BitmapImageDrawable.BitmapImage;
            if (bitmapImage?.IsEmpty != false) return;

            var imageWidth = (double)bitmapImage.Width;
            var imageHeight = (double)bitmapImage.Height;
            var roiWidth = imageWidth / (roiCount * 2d - 1d);
            var roiHeight = imageHeight / 2d;

            var roiY = BitmapImageDrawable.Point.Y + imageHeight - roiHeight;

            Document.RunDesign(() =>
            {
                Document.ROIModel.Clear();

                for (var i = 0; i < roiCount; i++)
                {
                    var roiX = BitmapImageDrawable.Point.X + i * roiWidth * 2d;
                    var width = roiWidth;
                    var height = roiHeight;
                    if (isAspectRatioFixed)
                    {
                        var aspectRatio = i switch { 0 => 1d, 1 => 2d, _ => 0.5 };
                        height = Math.Min(roiHeight, roiWidth / aspectRatio);
                        width = height * aspectRatio;
                    }

                    Document.ROIModel.Add(new BitmapImageROIDrawable(BitmapImageDrawable)
                    {
                        Rect = new Rect(roiX, roiY, width, height),
                        IsFixed = isAspectRatioFixed == false && Random.Shared.NextDouble() > 0.5,
                        Text = $"{i + 1}",
                        ResizeJoystickStateEnum = isAspectRatioFixed
                            ? resizeJoystickStateEnum
                            : resizeJoystickStateEnum & (BitmapImageROIResizeJoystickStateEnum)Random.Shared.Next((int)BitmapImageROIResizeJoystickStateEnum.All + 1)
                    });
                }
            });

            BitmapImageROIDrawables = [.. Document.ROIModel];

            var options = new ModifyBitmapImageROIDrawableInputOptions(BitmapImageDrawable)
            {
                BitmapImageROIDragMoveTypeEnum = dragMoveTypeEnum,
                IsDeleteEnabled = isDeleteEnabled,
                IsAspectRatioFixed = isAspectRatioFixed,
                IsOutsideImageAllowed = isOutsideImageAllowed,
                CancellationToken = cancellationToken
            };

            await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, options);

            BitmapImageROIDrawables = [.. Document.ROIModel];
        }, cancellationToken).ConfigureAwait(false);
    }
}