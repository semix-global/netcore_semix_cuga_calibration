using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Editors.Getters;
using Net.Utilities.Graphics.Primitives.Enums.Editors;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.OpticsFourierImageViewer.WPF;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Editors;
using Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;
using Net.Utilities.WPF.Enums;

namespace Core.Models.Models.Fourier.CenterChannelFlexibleAperture;

/// <summary>
/// 中心通道挡杆或推杆的两次位置采样结果.
/// </summary>
public sealed partial class FourierCenterChannelFlexibleApertureMotionItem : ObservableObject, IDisposable
{
    private readonly BitmapImageDrawable _firstBitmapImageDrawable = new();
    private readonly BitmapImageDrawable _secondBitmapImageDrawable = new();
    private readonly BitmapImageROIDrawable _firstBitmapImageROIDrawable;
    private readonly BitmapImageROIDrawable _secondBitmapImageROIDrawable;

    public CenterChannelMotionKind Kind { get; }

    public double? NominalAngle { get; }

    [ObservableProperty]
    public partial double? ActualAngle { get; set; }

    [ObservableProperty]
    public partial double? FirstSampleAngle { get; private set; }

    [ObservableProperty]
    public partial double FirstPosition { get; set; }

    [ObservableProperty]
    public partial double FirstSamplePosition { get; private set; }

    [ObservableProperty]
    public partial double SecondPosition { get; set; }

    [ObservableProperty]
    public partial string FirstImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SecondImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Rect FirstROI { get; set; }

    [ObservableProperty]
    public partial Rect SecondROI { get; set; }

    [ObservableProperty]
    public partial double PixelSize { get; private set; }

    [ObservableProperty]
    public partial double PixelOffset { get; private set; }

    [Newtonsoft.Json.JsonIgnore]
    public OpticsFourierImageDocument Document { get; } = new();

    [Newtonsoft.Json.JsonIgnore]
    public bool IsFirstCompleted => string.IsNullOrWhiteSpace(FirstImageFilePath) == false && FirstROI is { Width: > 0d, Height: > 0d };

    [Newtonsoft.Json.JsonIgnore]
    public bool IsSecondCompleted => IsFirstCompleted
                                     && string.IsNullOrWhiteSpace(SecondImageFilePath) == false
                                     && SecondROI is { Width: > 0d, Height: > 0d }
                                     && double.IsFinite(PixelSize)
                                     && Math.Abs(PixelSize) > double.Epsilon;

    [Newtonsoft.Json.JsonIgnore]
    public bool IsPush => Kind == CenterChannelMotionKind.Push;

    public FourierCenterChannelFlexibleApertureMotionItem(CenterChannelMotionKind kind, double? nominalAngle)
    {
        if (kind == CenterChannelMotionKind.Push && nominalAngle is not null) throw new ArgumentException("Push item cannot have an angle.", nameof(nominalAngle));
        if (kind != CenterChannelMotionKind.Push && nominalAngle is null) throw new ArgumentNullException(nameof(nominalAngle));

        Kind = kind;
        NominalAngle = nominalAngle;
        ActualAngle = nominalAngle;

        _firstBitmapImageROIDrawable = new BitmapImageROIDrawable(_firstBitmapImageDrawable);
        _secondBitmapImageROIDrawable = new BitmapImageROIDrawable(_secondBitmapImageDrawable);

        Document.RunDesign(() =>
        {
            Document.ImageModel.AddRange([_firstBitmapImageDrawable, _secondBitmapImageDrawable]);
            Document.ROIModel.AddRange([_firstBitmapImageROIDrawable, _secondBitmapImageROIDrawable]);
        });

        Document.Reset();
    }

    [Newtonsoft.Json.JsonConstructor]
    private FourierCenterChannelFlexibleApertureMotionItem(
        CenterChannelMotionKind kind,
        double? nominalAngle,
        double? actualAngle,
        double? firstSampleAngle,
        double firstPosition,
        double firstSamplePosition,
        double secondPosition,
        string firstImageFilePath,
        string secondImageFilePath,
        Rect firstROI,
        Rect secondROI,
        double pixelSize,
        double pixelOffset)
        : this(kind, nominalAngle)
    {
        ActualAngle = actualAngle;
        FirstSampleAngle = firstSampleAngle;
        FirstPosition = firstPosition;
        FirstSamplePosition = firstSamplePosition;
        SecondPosition = secondPosition;
        FirstImageFilePath = firstImageFilePath;
        SecondImageFilePath = secondImageFilePath;
        FirstROI = firstROI;
        SecondROI = secondROI;
        PixelSize = pixelSize;
        PixelOffset = pixelOffset;
    }

    /// <summary>
    /// 第一次采样: 允许按方向调整矩形尺寸和位置.
    /// </summary>
    public async Task FirstAsync(string imageFilePath, double? actualAngle, double firstPosition, CancellationToken cancellationToken)
    {
        Guard.IsNotNullOrWhiteSpace(imageFilePath);
        if (Kind == CenterChannelMotionKind.Push)
        {
            Guard.IsNull(actualAngle);
        }
        else
        {
            Guard.IsNotNull(actualAngle);
            Guard.IsGreaterThan(actualAngle.Value, 0d);
        }

        ResetSecond();
        ActualAngle = actualAngle;
        FirstPosition = firstPosition;
        FirstSampleAngle = actualAngle;
        FirstSamplePosition = firstPosition;
        FirstImageFilePath = imageFilePath;
        _firstBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(imageFilePath);
        _firstBitmapImageROIDrawable.Rect = CreateInitialROI(_firstBitmapImageDrawable);
        ConfigureFirstEditor();

        await EditAsync(_firstBitmapImageDrawable, _firstBitmapImageROIDrawable, "Edit first position ROI.", cancellationToken).ConfigureAwait(false);
        FirstROI = _firstBitmapImageDrawable.CartesianCoordinateToImageCoordinate(_firstBitmapImageROIDrawable.Rect);
        _firstBitmapImageROIDrawable.IsFixed = true;
        Document.View.ZoomToFit();
    }

    /// <summary>
    /// 第二次采样: 保持角度和矩形尺寸, 只按测量方向移动 ROI.
    /// </summary>
    public async Task SecondAsync(string imageFilePath, double secondPosition, CancellationToken cancellationToken)
    {
        Guard.IsTrue(IsFirstCompleted);
        Guard.IsNotNullOrWhiteSpace(imageFilePath);
        Guard.IsFalse(Math.Abs(secondPosition - FirstSamplePosition) < double.Epsilon);

        ActualAngle = FirstSampleAngle;
        FirstPosition = FirstSamplePosition;
        SecondPosition = secondPosition;
        SecondImageFilePath = imageFilePath;
        _secondBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(imageFilePath);
        _secondBitmapImageROIDrawable.Rect = _secondBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(FirstROI);
        _secondBitmapImageROIDrawable.ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.None;
        _secondBitmapImageROIDrawable.IsFixed = false;

        await EditAsync(_secondBitmapImageDrawable, _secondBitmapImageROIDrawable, "Move second position ROI.", cancellationToken).ConfigureAwait(false);
        SecondROI = _secondBitmapImageDrawable.CartesianCoordinateToImageCoordinate(_secondBitmapImageROIDrawable.Rect);
        _secondBitmapImageROIDrawable.IsFixed = true;

        var pixelOffset = Kind == CenterChannelMotionKind.Y
            ? SecondROI.Y - FirstROI.Y
            : SecondROI.X - FirstROI.X;
        var motorOffset = secondPosition - FirstSamplePosition;
        Guard.IsTrue(double.IsFinite(pixelOffset));
        Guard.IsTrue(double.IsFinite(motorOffset));
        Guard.IsFalse(Math.Abs(pixelOffset) < double.Epsilon);
        Guard.IsFalse(Math.Abs(motorOffset) < double.Epsilon);

        PixelOffset = pixelOffset;
        PixelSize = Math.Abs(pixelOffset / motorOffset);
        Guard.IsTrue(double.IsFinite(PixelSize));
        Document.View.ZoomToFit();
    }

    public void Reset()
    {
        FirstImageFilePath = string.Empty;
        SecondImageFilePath = string.Empty;
        FirstROI = Rect.Empty;
        SecondROI = Rect.Empty;
        FirstPosition = 0d;
        FirstSampleAngle = null;
        FirstSamplePosition = 0d;
        SecondPosition = 0d;
        ActualAngle = NominalAngle;
        PixelOffset = 0d;
        PixelSize = 0d;
        Document.Reset();
    }

    public FourierCenterChannelFlexibleApertureMotionItem Clone() => new(
        Kind,
        NominalAngle,
        ActualAngle,
        FirstSampleAngle,
        FirstPosition,
        FirstSamplePosition,
        SecondPosition,
        FirstImageFilePath,
        SecondImageFilePath,
        FirstROI,
        SecondROI,
        PixelSize,
        PixelOffset);

    public void Review()
    {
        Document.Reset();
        if (string.IsNullOrWhiteSpace(FirstImageFilePath) == false)
        {
            _firstBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(FirstImageFilePath);
            _firstBitmapImageROIDrawable.Rect = _firstBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(FirstROI);
            _firstBitmapImageROIDrawable.IsFixed = true;
        }

        if (string.IsNullOrWhiteSpace(SecondImageFilePath) == false)
        {
            _secondBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(SecondImageFilePath);
            _secondBitmapImageROIDrawable.Rect = _secondBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(SecondROI);
            _secondBitmapImageROIDrawable.IsFixed = true;
        }

        Document.View.ZoomToFit();
    }

    public object ToHtmlAnonymous() => new
    {
        Kind,
        NominalAngle,
        ActualAngle,
        FirstSampleAngle,
        FirstPosition,
        FirstSamplePosition,
        SecondPosition,
        FirstROI,
        SecondROI,
        PixelOffset,
        PixelSize,
        FirstImage = string.IsNullOrWhiteSpace(FirstImageFilePath) ? null : new HtmlImage(FirstImageFilePath),
        SecondImage = string.IsNullOrWhiteSpace(SecondImageFilePath) ? null : new HtmlImage(SecondImageFilePath)
    };

    public void Dispose()
    {
        _firstBitmapImageDrawable.Dispose();
        _secondBitmapImageDrawable.Dispose();
    }

    private Rect CreateInitialROI(BitmapImageDrawable bitmapImageDrawable)
    {
        var imageSize = bitmapImageDrawable.BitmapImage?.Size ?? throw new InvalidOperationException("The calibration image is empty.");
        return Kind == CenterChannelMotionKind.Y
            ? bitmapImageDrawable.ImageCoordinateToCartesianCoordinate(new Rect(0d, imageSize.Height / 2d - imageSize.Height / 8d, imageSize.Width, imageSize.Height / 4d)).ImageCoordinateRound()
            : bitmapImageDrawable.ImageCoordinateToCartesianCoordinate(new Rect(imageSize.Width / 2d - imageSize.Width / 8d, 0d, imageSize.Width / 4d, imageSize.Height)).ImageCoordinateRound();
    }

    private void ConfigureFirstEditor()
    {
        _firstBitmapImageROIDrawable.ResizeJoystickStateEnum = Kind == CenterChannelMotionKind.Y
            ? BitmapImageROIResizeJoystickStateEnum.XCenterYMin | BitmapImageROIResizeJoystickStateEnum.XCenterYMax
            : BitmapImageROIResizeJoystickStateEnum.XMinYCenter | BitmapImageROIResizeJoystickStateEnum.XMaxYCenter;
        _firstBitmapImageROIDrawable.IsFixed = false;
    }

    private Task<OutputResult<Unit>> EditAsync(BitmapImageDrawable bitmapImageDrawable, BitmapImageROIDrawable bitmapImageROIDrawable, string title, CancellationToken cancellationToken)
    {
        return EditCoreAsync();

        async Task<OutputResult<Unit>> EditCoreAsync()
        {
            Document.View.ZoomToFit();
            var output = await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, new ModifyBitmapImageROIDrawableInputOptions(bitmapImageDrawable)
            {
                Title = title,
                BitmapImageROIDragMoveTypeEnum = Kind == CenterChannelMotionKind.Y ? BitmapImageROIDragMoveTypeEnum.Y : BitmapImageROIDragMoveTypeEnum.X,
                IsDeleteEnabled = false,
                CancellationToken = cancellationToken
            }).ConfigureAwait(false);

            Guard.IsTrue(output.OutputResultModeEnum == OutputResultModeEnum.Ok);
            Guard.IsTrue(bitmapImageROIDrawable.IsVisible);
            Guard.IsTrue(bitmapImageROIDrawable.Rect is { Width: > 0d, Height: > 0d });

            return output;
        }
    }

    private void ResetSecond()
    {
        SecondImageFilePath = string.Empty;
        SecondROI = Rect.Empty;
        SecondPosition = 0d;
        PixelOffset = 0d;
        PixelSize = 0d;
        _secondBitmapImageDrawable.BitmapImage = null;
        _secondBitmapImageROIDrawable.Rect = Rect.Empty;
        _secondBitmapImageROIDrawable.IsFixed = false;
    }
}