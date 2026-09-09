/*using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using MathNet.Numerics;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Enums.Editors;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Editors;
using Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

public partial class FourierSideChannelFlexibleApertureDTOItem
{
    public sealed partial class SecondStep : ObservableObject, ICloneable<FirstStep>
    {
        private readonly BitmapImageDrawable _originalBitmapImageDrawable = new();
        private readonly BitmapImageDrawable _roiBitmapImageDrawable = new();

        public Rod[] Rods { get; private init; }

        [ObservableProperty]
        public partial string ChannelImageFilePath { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string ROIChannelImageFilePath { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonIgnore]
        public OpticsFourierImageDocument Document { get; } = new();

        public SecondStep(int[] rods)
        {
            RodLeft = new Rod(_roiBitmapImageDrawable);
            RodRight = new Rod(_roiBitmapImageDrawable);
            Document.RunDesign(() =>
            {
                Document.ImageModel.AddRange([_originalBitmapImageDrawable, _roiBitmapImageDrawable]);
                Document.ROIModel.AddRange([RodLeft.BitmapImageROIDrawable, RodRight.BitmapImageROIDrawable]);
            });
        }

        #region Mapper

        public FirstStep Clone() => new()
        {
            RodLeft = RodLeft.AdaptIn(RodLeft),
            RodRight = RodRight.AdaptIn(RodRight),
            ChannelImageFilePath = ChannelImageFilePath,
            ROIChannelImageFilePath = ROIChannelImageFilePath
        };

        #endregion

        #region 校准

        public void Reset()
        {
            RodLeft.Reset();
            RodRight.Reset();

            ChannelImageFilePath = string.Empty;
            ROIChannelImageFilePath = string.Empty;

            Document.Reset();
        }

        public async Task CalibratingAsync(int rodTotalCount, bool isOdd, double motorAbsoluteValue, CancellationToken cancellationToken)
        {
            try
            {
                Document.Reset();

                Guard.IsNotNullOrWhiteSpace(ChannelImageFilePath);
                Guard.IsNotNullOrWhiteSpace(ROIChannelImageFilePath);

                _originalBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ChannelImageFilePath);
                _roiBitmapImageDrawable.Point = _originalBitmapImageDrawable.Point + new Vector(_originalBitmapImageDrawable.BitmapImage.Size.Width + 10d, 0d);
                _roiBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ROIChannelImageFilePath);

                var rodIndexes = Generate.LinearRangeInt32(0, rodTotalCount - 1);
                var oddRodIndexes = rodIndexes.Where(t => ((t + 1) & 1) == 1).ToArray();
                var evenRodIndexes = rodIndexes.Where(t => ((t + 1) & 1) == 0).ToArray();
                Guard.IsGreaterThan(oddRodIndexes.Length, 2);
                Guard.IsGreaterThan(evenRodIndexes.Length, 2);

                int[] oddCenterRodIndexes =
                [
                    oddRodIndexes[oddRodIndexes.Length / 2 - 1],
                    oddRodIndexes[oddRodIndexes.Length / 2]
                ];
                int[] evenCenterRodIndexes =
                [
                    evenRodIndexes[evenRodIndexes.Length / 2 - 1],
                    evenRodIndexes[evenRodIndexes.Length / 2]
                ];
                Rect[] rodROIs =
                [
                    .. rodIndexes
                        .Select(i =>
                        {
                            var width = _roiBitmapImageDrawable.BitmapImage.Width / rodTotalCount;

                            return _roiBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(new Rect(width * i, 0d, width, _roiBitmapImageDrawable.BitmapImage.Height));
                        })
                ];

                RodLeft.Index = isOdd ? oddCenterRodIndexes[0] : evenCenterRodIndexes[0];
                RodRight.Index = isOdd ? oddCenterRodIndexes[1] : evenCenterRodIndexes[1];
                RodLeft.MotorAbsoluteValue = motorAbsoluteValue;
                RodRight.MotorAbsoluteValue = motorAbsoluteValue;
                RodLeft.BitmapImageROIDrawable.Rect = rodROIs[RodLeft.Index];
                RodRight.BitmapImageROIDrawable.Rect = rodROIs[RodRight.Index];
                RodLeft.BitmapImageROIDrawable.Text = $"{RodLeft.Index + 1}";
                RodRight.BitmapImageROIDrawable.Text = $"{RodRight.Index + 1}";
                Document.View.SetViewBounds(_roiBitmapImageDrawable.GetExtents());

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var outputResult = await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, new ModifyBitmapImageROIDrawableInputOptions(_roiBitmapImageDrawable)
                    {
                        BitmapImageROIDragMoveTypeEnum = BitmapImageROIDragMoveTypeEnum.X,
                        CancellationToken = cancellationToken
                    });

                    switch (outputResult)
                    {
                        case { OutputResultModeEnum: OutputResultModeEnum.Ok }:
                            Guard.IsTrue(RodLeft.BitmapImageROIDrawable.Rect is { Width: > 0d, Height: > 0d });
                            Guard.IsTrue(RodRight.BitmapImageROIDrawable.Rect is { Width: > 0d, Height: > 0d });

                            Guard.IsTrue(RodLeft.BitmapImageROIDrawable.Rect.XMax <= RodRight.BitmapImageROIDrawable.Rect.XMin);

                            goto OuterLoop;

                        case { OutputResultModeEnum: OutputResultModeEnum.Cancel, CancelReason: CancelReasonEnum.Escape }:

                            continue;

                        case { OutputResultModeEnum: OutputResultModeEnum.Cancel, CancelReason: CancelReasonEnum.OperationCanceledException }:
                            ThrowHelper.ThrowOperationCanceledException(cancellationToken);

                            break;

                        default:
                            ThrowHelper.ThrowInvalidOperationException($"{nameof(outputResult.CancelReason)}: {outputResult.CancelReason}, {nameof(outputResult.ErrorMessage)}: {outputResult.ErrorMessage}");

                            break;
                    }
                }

                OuterLoop:

                RodLeft.BitmapImageROIDrawable.IsFixed = true;
                RodLeft.ImageROI = _roiBitmapImageDrawable.CartesianCoordinateToImageCoordinate(RodLeft.BitmapImageROIDrawable.Rect);
                Guard.IsEqualTo(_roiBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(RodLeft.ImageROI), RodLeft.BitmapImageROIDrawable.Rect);

                RodRight.BitmapImageROIDrawable.IsFixed = true;
                RodRight.ImageROI = _roiBitmapImageDrawable.CartesianCoordinateToImageCoordinate(RodRight.BitmapImageROIDrawable.Rect);
                Guard.IsEqualTo(_roiBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(RodRight.ImageROI), RodRight.BitmapImageROIDrawable.Rect);
            }
            finally
            {
                Document.View.ZoomToFit();
            }
        }

        public void Review()
        {
            try
            {
                Document.Reset();

                if (string.IsNullOrWhiteSpace(ChannelImageFilePath) || string.IsNullOrWhiteSpace(ROIChannelImageFilePath)) return;

                _originalBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ChannelImageFilePath);

                _roiBitmapImageDrawable.Point = _originalBitmapImageDrawable.Point + new Vector(_originalBitmapImageDrawable.BitmapImage.Size.Width + 10d, 0d);
                _roiBitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(ROIChannelImageFilePath);

                RodLeft.BitmapImageROIDrawable.IsFixed = true;
                RodLeft.BitmapImageROIDrawable.Rect = _originalBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(RodLeft.ImageROI);

                RodRight.BitmapImageROIDrawable.IsFixed = true;
                RodRight.BitmapImageROIDrawable.Rect = _originalBitmapImageDrawable.ImageCoordinateToCartesianCoordinate(RodRight.ImageROI);
            }
            finally
            {
                Document.View.ZoomToFit();
            }
        }

        #endregion
    }
}*/