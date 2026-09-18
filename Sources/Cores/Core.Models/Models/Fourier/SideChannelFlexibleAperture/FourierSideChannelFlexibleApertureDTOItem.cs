using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.OpticsFourierImageViewer.WPF;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

public sealed partial class FourierSideChannelFlexibleApertureDTOItem : ObservableObject, ICloneable<FourierSideChannelFlexibleApertureDTOItem>, IDisposable
{
    private readonly BitmapImageDrawable _resultBitmapImageDrawable = new();

    [Newtonsoft.Json.JsonProperty]
    private readonly int _rodTotalCount;

    public int ChannelId { get; }

    public string ChannelImageFilePath { get; }

    public Item EvenItem { get; private init; }

    public Item OddItem { get; private init; }

    [ObservableProperty]
    public partial double MinMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double MaxMotorAbsoluteValue { get; set; }

    public RodResult[] RodResults { get; }

    [Newtonsoft.Json.JsonIgnore]
    public OpticsFourierImageDocument Document { get; } = new();

    public FourierSideChannelFlexibleApertureDTOItem(int rodTotalCount, int channelId, string channelImageFilePath)
    {
        _rodTotalCount = rodTotalCount;

        ChannelId = channelId;
        ChannelImageFilePath = channelImageFilePath;
        EvenItem = new Item(rodTotalCount, true);
        OddItem = new Item(rodTotalCount, false);

        RodResults =
        [
            .. EvenItem.Step2Rods
                .Select(t => t.Index)
                .Union(OddItem.Step2Rods.Select(t => t.Index))
                .Order()
                .Select(t => new RodResult(_resultBitmapImageDrawable) { Index = t })
        ];

        Document.RunDesign(() =>
        {
            Document.ImageModel.Add(_resultBitmapImageDrawable);
            Document.ROIModel.AddRange(RodResults.Select(t => t.BitmapImageROIDrawable));
        });

        ResetDocument();
    }

    [Newtonsoft.Json.JsonConstructor]
    private FourierSideChannelFlexibleApertureDTOItem(
        [Newtonsoft.Json.JsonProperty(nameof(_rodTotalCount))]
        int rodTotalCount,
        int channelId,
        string channelImageFilePath,
        Item evenItem,
        Item oddItem,
        RodResult[] rodResults)
        : this(rodTotalCount, channelId, channelImageFilePath)
    {
        EvenItem.Dispose();
        EvenItem = evenItem;

        OddItem.Dispose();
        OddItem = oddItem;

        foreach (var target in RodResults) target.AdaptIn(rodResults.Single(t => t.Index == target.Index));
    }

    private void ResetDocument()
    {
        Document.Reset();

        foreach (var rodResult in RodResults) rodResult.BitmapImageROIDrawable.Text = $"{rodResult.Index + 1}";
    }

    #region Mapper

#pragma warning disable IDISP003

    public FourierSideChannelFlexibleApertureDTOItem Clone()
    {
        var item = new FourierSideChannelFlexibleApertureDTOItem(_rodTotalCount, ChannelId, ChannelImageFilePath)
        {
            EvenItem = EvenItem.Clone(),
            OddItem = OddItem.Clone(),
            MinMotorAbsoluteValue = MinMotorAbsoluteValue,
            MaxMotorAbsoluteValue = MaxMotorAbsoluteValue
        };

        foreach (var target in item.RodResults) target.AdaptIn(RodResults.Single(t => t.Index == target.Index));

        return item;
    }

#pragma warning restore IDISP003

    #endregion

    #region 校准

    public void Reset()
    {
        MinMotorAbsoluteValue = 0d;
        MaxMotorAbsoluteValue = 0d;

        foreach (var rodResult in RodResults) rodResult.Reset();

        ResetDocument();
    }

    public void Calibrating(double minMotorAbsoluteValue, double maxMotorAbsoluteValue, CancellationToken cancellationToken)
    {
        try
        {
            ResetDocument();

            Guard.IsNotNullOrWhiteSpace(ChannelImageFilePath);

            MinMotorAbsoluteValue = minMotorAbsoluteValue;
            MaxMotorAbsoluteValue = maxMotorAbsoluteValue;

            _resultBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ChannelImageFilePath);
            Document.View.ZoomToFit();

            CalculateRodResults(EvenItem);
            CalculateRodResults(OddItem);

            foreach (var rodResult in RodResults)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (rodResult.IsDeleted) rodResult.BitmapImageROIDrawable.Text = $"X {rodResult.BitmapImageROIDrawable.Text}";

                rodResult.BitmapImageROIDrawable.IsFixed = true;
                rodResult.BitmapImageROIDrawable.Rect = _resultBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(rodResult.MinImageROI);
                rodResult.BitmapImageROIDrawable.IsVisible = rodResult.BitmapImageROIDrawable.Rect is { Width: > 0 };
            }
        }
        finally
        {
            Document.View.ZoomToFit();
        }

        return;

        void CalculateRodResults(Item item)
        {
            foreach (var step2Rod in item.Step2Rods)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var step1Rod = item.Step1Rods.Single(t => t.Index == step2Rod.Index);
                var rodResult = RodResults.Single(t => t.Index == step2Rod.Index);

                rodResult.IsDeleted = step2Rod.IsDeleted;

                var heightDelta = step2Rod.ImageROI.Height - step1Rod.ImageROI.Height;
                var motorDelta = item.Step2MotorAbsoluteValue - item.Step0AndStep1MotorAbsoluteValue;
                Guard.IsNotEqualTo(heightDelta, 0d);

                rodResult.PixelSize = motorDelta / heightDelta;
                Guard.IsGreaterThan(rodResult.PixelSize, 0);

                rodResult.MinImageROI = new Rect(
                        step2Rod.ImageROI.Point,
                        new Size(step2Rod.ImageROI.Width, step2Rod.ImageROI.Height + (minMotorAbsoluteValue - item.Step2MotorAbsoluteValue) / rodResult.PixelSize))
                    .ImageCoordinateRound()
                    .ClampToBounds(new Rect(Point.Origin, _resultBitmapImageDrawable.BitmapImage.Size));

                rodResult.MaxImageROI = new Rect(
                        step2Rod.ImageROI.Point,
                        new Size(step2Rod.ImageROI.Width, step2Rod.ImageROI.Height + (maxMotorAbsoluteValue - item.Step2MotorAbsoluteValue) / rodResult.PixelSize))
                    .ImageCoordinateRound()
                    .ClampToBounds(new Rect(Point.Origin, _resultBitmapImageDrawable.BitmapImage.Size));
            }
        }
    }

    public void Review()
    {
        try
        {
            ResetDocument();

            if (string.IsNullOrWhiteSpace(ChannelImageFilePath)) return;

            _resultBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ChannelImageFilePath);

            foreach (var rodResult in RodResults)
            {
                if (rodResult.IsDeleted) rodResult.BitmapImageROIDrawable.Text = $"X {rodResult.BitmapImageROIDrawable.Text}";

                rodResult.BitmapImageROIDrawable.IsFixed = true;
                rodResult.BitmapImageROIDrawable.Rect = _resultBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(rodResult.MinImageROI);
                rodResult.BitmapImageROIDrawable.IsVisible = rodResult.BitmapImageROIDrawable.Rect is { Width: > 0 };
            }
        }
        finally
        {
            Document.View.ZoomToFit();
        }
    }

    #endregion

    public object ToHtmlAnonymous() => new
    {
        _rodTotalCount,
        ChannelId,
        ChannelImageFilePath,
        MinMotorAbsoluteValue,
        MaxMotorAbsoluteValue,
        RodResults = new HtmlTable([.. RodResults.Select(t => new { t.Index, t.IsDeleted, t.PixelSize, t.MinImageROI, t.MaxImageROI })]),
        MinResultImage = new HtmlImage(ChannelImageFilePath, htmlImageOverlays:
        [
            .. RodResults.Select(t => new HtmlImageRectangleOverlay(t.MinImageROI)),
            .. RodResults.Select(t => new HtmlImageTextOverlay(t.MinImageROI.Center, t.BitmapImageROIDrawable.Text))
        ]),
        MaxResultImage = new HtmlImage(ChannelImageFilePath, htmlImageOverlays:
        [
            .. RodResults.Select(t => new HtmlImageRectangleOverlay(t.MaxImageROI)),
            .. RodResults.Select(t => new HtmlImageTextOverlay(t.MaxImageROI.Center, t.BitmapImageROIDrawable.Text))
        ])
    };

    public void Dispose()
    {
        _resultBitmapImageDrawable.Dispose();
        EvenItem.Dispose();
        OddItem.Dispose();
    }
}