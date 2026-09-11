using CommunityToolkit.Diagnostics;
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
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

public partial class FourierSideChannelFlexibleApertureDTOItem
{
    public sealed partial class Item : ObservableObject, ICloneable<Item>, IDisposable
    {
        private readonly BitmapImageDrawable _step0BitmapImageDrawable = new();
        private readonly BitmapImageDrawable _step1BitmapImageDrawable = new();
        private readonly BitmapImageDrawable _step2BitmapImageDrawable = new();

        [Newtonsoft.Json.JsonProperty]
        private readonly int _rodTotalCount;

        [Newtonsoft.Json.JsonProperty]
        private readonly bool _isEven;

        [ObservableProperty]
        public partial double Step0AndStep1MotorAbsoluteValue { get; set; }

        [ObservableProperty]
        public partial double Step2MotorAbsoluteValue { get; set; }

        [ObservableProperty]
        public partial string Step0ChannelImageFilePath { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string Step1ChannelImageFilePath { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string Step2ChannelImageFilePath { get; set; } = string.Empty;

        public Rod Step0LeftRod { get; private init; }

        public Rod Step0RightRod { get; private init; }

        public Rod[] Step1Rods { get; private init; }

        public Rod[] Step2Rods { get; private init; }

        [Newtonsoft.Json.JsonIgnore]
        public OpticsFourierImageDocument Document { get; } = new();

        public Item(int rodTotalCount, bool isEven)
        {
            _rodTotalCount = rodTotalCount;
            _isEven = isEven;

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

            var joystickStateEnum = BitmapImageROIResizeJoystickStateEnum.XMinYMin |
                                    BitmapImageROIResizeJoystickStateEnum.XCenterYMin |
                                    BitmapImageROIResizeJoystickStateEnum.XMaxYMin;
            Step0LeftRod = new Rod(_step0BitmapImageDrawable) { Index = _isEven ? evenCenterRodIndexes[0] : oddCenterRodIndexes[0], BitmapImageROIDrawable = { IsVisible = true, IsFixed = false, ResizeJoystickStateEnum = joystickStateEnum } };
            Step0LeftRod.BitmapImageROIDrawable.Text = $"{Step0LeftRod.Index + 1}";

            Step0RightRod = new Rod(_step0BitmapImageDrawable) { Index = _isEven ? evenCenterRodIndexes[1] : oddCenterRodIndexes[1], BitmapImageROIDrawable = { IsVisible = true, IsFixed = false, ResizeJoystickStateEnum = joystickStateEnum } };
            Step0RightRod.BitmapImageROIDrawable.Text = $"{Step0RightRod.Index + 1}";

            Step1Rods = _isEven
                ? [.. evenRodIndexes.Select(i => new Rod(_step1BitmapImageDrawable) { Index = i, BitmapImageROIDrawable = { IsVisible = true, IsFixed = false, ResizeJoystickStateEnum = joystickStateEnum } })]
                : [.. oddRodIndexes.Select(i => new Rod(_step1BitmapImageDrawable) { Index = i, BitmapImageROIDrawable = { IsVisible = true, IsFixed = false, ResizeJoystickStateEnum = joystickStateEnum } })];
            foreach (var step1Rod in Step1Rods) step1Rod.BitmapImageROIDrawable.Text = $"{step1Rod.Index + 1}";

            Step2Rods = _isEven
                ? [.. evenRodIndexes.Select(i => new Rod(_step2BitmapImageDrawable) { Index = i, BitmapImageROIDrawable = { IsVisible = true, IsFixed = false, ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.XCenterYMin } })]
                : [.. oddRodIndexes.Select(i => new Rod(_step2BitmapImageDrawable) { Index = i, BitmapImageROIDrawable = { IsVisible = true, IsFixed = false, ResizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.XCenterYMin } })];
            foreach (var step2Rod in Step2Rods) step2Rod.BitmapImageROIDrawable.Text = $"{step2Rod.Index + 1}";

            Document.RunDesign(() =>
            {
                Document.ImageModel.AddRange([_step0BitmapImageDrawable, _step1BitmapImageDrawable, _step2BitmapImageDrawable]);
                Document.ROIModel.AddRange([
                    Step0LeftRod.BitmapImageROIDrawable,
                    Step0RightRod.BitmapImageROIDrawable,
                    .. Step1Rods.Select(t => t.BitmapImageROIDrawable),
                    .. Step2Rods.Select(t => t.BitmapImageROIDrawable)
                ]);
            });
        }

        #region Mapper

        public Item Clone()
        {
            var item = new Item(_rodTotalCount, _isEven)
            {
                Step0AndStep1MotorAbsoluteValue = Step0AndStep1MotorAbsoluteValue,
                Step2MotorAbsoluteValue = Step2MotorAbsoluteValue,
                Step0ChannelImageFilePath = Step0ChannelImageFilePath,
                Step1ChannelImageFilePath = Step1ChannelImageFilePath,
                Step2ChannelImageFilePath = Step2ChannelImageFilePath
            };

            item.Step0LeftRod.AdaptIn(Step0LeftRod);
            item.Step0RightRod.AdaptIn(Step0RightRod);
            foreach (var (target, source) in item.Step1Rods.Zip(Step1Rods)) target.AdaptIn(source);
            foreach (var (target, source) in item.Step2Rods.Zip(Step2Rods)) target.AdaptIn(source);

            return item;
        }

        #endregion

        #region 校准

        public void Reset()
        {
            Step0AndStep1MotorAbsoluteValue = 0d;
            Step2MotorAbsoluteValue = 0d;
            Step0ChannelImageFilePath = string.Empty;
            Step1ChannelImageFilePath = string.Empty;
            Step2ChannelImageFilePath = string.Empty;

            Step0LeftRod.Reset();
            Step0RightRod.Reset();
            foreach (var step1Rod in Step1Rods) step1Rod.Reset();
            foreach (var step2Rod in Step2Rods) step2Rod.Reset();

            Document.ResetEditROI();
        }

        public async Task CalibratingAsync(double step0AndStep1MotorAbsoluteValue, double step2MotorAbsoluteValue, CancellationToken cancellationToken)
        {
            try
            {
                Step0AndStep1MotorAbsoluteValue = step0AndStep1MotorAbsoluteValue;
                Step2MotorAbsoluteValue = step2MotorAbsoluteValue;

                Guard.IsNotNullOrWhiteSpace(Step0ChannelImageFilePath);
                Guard.IsNotNullOrWhiteSpace(Step1ChannelImageFilePath);
                Guard.IsNotNullOrWhiteSpace(Step2ChannelImageFilePath);

                _step0BitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step0ChannelImageFilePath);

                _step1BitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step1ChannelImageFilePath);
                _step1BitmapImageDrawable.Point = _step0BitmapImageDrawable.Point + new Vector(0d, _step0BitmapImageDrawable.BitmapImage.Height + 10d);

                _step2BitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step2ChannelImageFilePath);
                _step2BitmapImageDrawable.Point = _step1BitmapImageDrawable.Point + new Vector(_step1BitmapImageDrawable.BitmapImage.Width + 10d, 0d);

                Document.View.ZoomToFit();

                await Task.Delay(1000, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                await Step0Async();

                await Task.Delay(1000, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                await Step1Async();

                await Task.Delay(1000, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                await Step2Async();
            }
            finally
            {
                Document.View.ZoomToFit();
            }

            return;

            async Task Step0Async()
            {
                var width = _step0BitmapImageDrawable.BitmapImage.Width / (Step1Rods.Length * 2d);

                Step0LeftRod.BitmapImageROIDrawable.Rect = _step0BitmapImageDrawable.ImageCoordinateToCartesianCoordinate(new Rect(width * Step0LeftRod.Index, 0d, width, _step0BitmapImageDrawable.BitmapImage.Height)).ImageCoordinateRound();
                Step0RightRod.BitmapImageROIDrawable.Rect = _step0BitmapImageDrawable.ImageCoordinateToCartesianCoordinate(new Rect(width * Step0RightRod.Index, 0d, width, _step0BitmapImageDrawable.BitmapImage.Height)).ImageCoordinateRound();

                Document.View.SetViewBounds(_step0BitmapImageDrawable.GetExtents());

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var outputResult = await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, new ModifyBitmapImageROIDrawableInputOptions(_step0BitmapImageDrawable)
                    {
                        BitmapImageROIDragMoveTypeEnum = BitmapImageROIDragMoveTypeEnum.X,
                        IsDeleteEnabled = false,
                        CancellationToken = cancellationToken
                    });

                    switch (outputResult)
                    {
                        case { OutputResultModeEnum: OutputResultModeEnum.Ok }:
                            try
                            {
                                Guard.IsTrue(Step0LeftRod.BitmapImageROIDrawable.IsVisible);
                                Guard.IsTrue(Step0RightRod.BitmapImageROIDrawable.IsVisible);

                                Guard.IsTrue(Step0LeftRod.BitmapImageROIDrawable.Rect is { Width: > 0d, Height: > 0d });
                                Guard.IsTrue(Step0RightRod.BitmapImageROIDrawable.Rect is { Width: > 0d, Height: > 0d });

                                Guard.IsTrue(Step0LeftRod.BitmapImageROIDrawable.Rect.XMax < Step0RightRod.BitmapImageROIDrawable.Rect.XMin);

                                goto OuterLoop;
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                if (ShouldContinue(ex)) continue;

                                throw;
                            }

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

                Step0LeftRod.IsDeleted = false;
                Step0LeftRod.ImageROI = _step0BitmapImageDrawable.CartesianCoordinateToImageCoordinate(Step0LeftRod.BitmapImageROIDrawable.Rect);
                Step0LeftRod.BitmapImageROIDrawable.IsFixed = true;

                Step0RightRod.IsDeleted = false;
                Step0RightRod.ImageROI = _step0BitmapImageDrawable.CartesianCoordinateToImageCoordinate(Step0RightRod.BitmapImageROIDrawable.Rect);
                Step0RightRod.BitmapImageROIDrawable.IsFixed = true;
            }

            async Task Step1Async()
            {
                var step0LeftRod = Step1Rods.Single(t => t.Index == Step0LeftRod.Index);
                var step0RightRod = Step1Rods.Single(t => t.Index == Step0RightRod.Index);

                step0LeftRod.IsDeleted = false;
                step0LeftRod.ImageROI = Step0LeftRod.ImageROI;
                step0LeftRod.BitmapImageROIDrawable.IsFixed = true;
                step0LeftRod.BitmapImageROIDrawable.Rect = _step1BitmapImageDrawable.ImageCoordinateToCartesianCoordinate(step0LeftRod.ImageROI);

                step0RightRod.IsDeleted = false;
                step0RightRod.ImageROI = Step0RightRod.ImageROI;
                step0RightRod.BitmapImageROIDrawable.IsFixed = true;
                step0RightRod.BitmapImageROIDrawable.Rect = _step1BitmapImageDrawable.ImageCoordinateToCartesianCoordinate(step0RightRod.ImageROI);

                CalculateInvisibleRodPositions(
                    [step0LeftRod, step0RightRod],
                    [.. Step1Rods.Except([step0LeftRod, step0RightRod])],
                    false);

                Document.View.SetViewBounds(_step1BitmapImageDrawable.GetExtents());

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var outputResult = await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, new ModifyBitmapImageROIDrawableInputOptions(_step1BitmapImageDrawable)
                    {
                        BitmapImageROIDragMoveTypeEnum = BitmapImageROIDragMoveTypeEnum.X,
                        IsDeleteEnabled = true,
                        CancellationToken = cancellationToken
                    });

                    switch (outputResult)
                    {
                        case { OutputResultModeEnum: OutputResultModeEnum.Ok }:
                            try
                            {
                                Rod[] temps = [.. Step1Rods.Where(t => t.BitmapImageROIDrawable.IsVisible)];
                                Guard.IsGreaterThanOrEqualTo(temps.Length, 2);

                                foreach (var (previousRod, nextRod) in temps.Zip(temps.Skip(1)))
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    Guard.IsTrue(previousRod.BitmapImageROIDrawable.Rect is { Width: > 0d, Height: > 0d });
                                    Guard.IsTrue(nextRod.BitmapImageROIDrawable.Rect is { Width: > 0d, Height: > 0d });
                                    Guard.IsTrue(nextRod.Index == previousRod.Index + 2);
                                    Guard.IsTrue(previousRod.BitmapImageROIDrawable.Rect.XMax < nextRod.BitmapImageROIDrawable.Rect.XMin);
                                }

                                goto OuterLoop;
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                if (ShouldContinue(ex)) continue;

                                throw;
                            }

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

                foreach (var step1Rod in Step1Rods)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    step1Rod.IsDeleted = step1Rod.BitmapImageROIDrawable.IsVisible == false;
                    step1Rod.BitmapImageROIDrawable.Text = step1Rod.IsDeleted ? $"X: {step1Rod.Index + 1}" : $"{step1Rod.Index + 1}";
                    if (step1Rod.IsDeleted) continue;

                    step1Rod.BitmapImageROIDrawable.IsFixed = true;
                    step1Rod.ImageROI = _step1BitmapImageDrawable.CartesianCoordinateToImageCoordinate(step1Rod.BitmapImageROIDrawable.Rect);
                }

                CalculateInvisibleRodPositions(
                    [.. Step1Rods.Where(t => t.BitmapImageROIDrawable.IsVisible).OrderBy(t => t.Index)],
                    [.. Step1Rods.Where(t => t.BitmapImageROIDrawable.IsVisible == false)],
                    true);

                return;

                void CalculateInvisibleRodPositions(Rod[] visibleRods, Rod[] invisibleRods, bool isFixed)
                {
                    if (invisibleRods.Length == 0) return;

                    Guard.IsGreaterThanOrEqualTo(visibleRods.Length, 2);

                    var averageWidth = visibleRods.Average(t => t.ImageROI.Width);
                    var averageHeight = visibleRods.Average(t => t.ImageROI.Height);
                    var averageY = visibleRods.Average(t => t.ImageROI.Y);
                    var averageCenterXOffsetPerRod = visibleRods.Zip(visibleRods.Skip(1))
                        .Average(pair => (pair.Second.ImageROI.Center.X - pair.First.ImageROI.Center.X) / ((pair.Second.Index - pair.First.Index) / 2d));

                    Guard.IsEqualTo(averageY, 0d);

                    foreach (var invisibleRod in invisibleRods)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var nearestVisibleRod = visibleRods.OrderBy(t => Math.Abs(t.Index - invisibleRod.Index)).First();
                        var centerX = nearestVisibleRod.ImageROI.Center.X + averageCenterXOffsetPerRod * ((invisibleRod.Index - nearestVisibleRod.Index) / 2d);

                        invisibleRod.ImageROI = new Rect(centerX - averageWidth / 2d, 0d, averageWidth, averageHeight).ImageCoordinateRound();

                        invisibleRod.BitmapImageROIDrawable.IsFixed = isFixed;
                        invisibleRod.BitmapImageROIDrawable.Rect = _step1BitmapImageDrawable.ImageCoordinateToCartesianCoordinate(invisibleRod.ImageROI);
                        invisibleRod.BitmapImageROIDrawable.IsVisible = true;
                    }
                }
            }

            async Task Step2Async()
            {
                foreach (var step1Rod in Step1Rods)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var step2Rod = Step2Rods.Single(t => t.Index == step1Rod.Index);

                    step2Rod.IsDeleted = step1Rod.IsDeleted;
                    step2Rod.ImageROI = step1Rod.ImageROI;
                    step2Rod.BitmapImageROIDrawable.Rect = _step2BitmapImageDrawable.ImageCoordinateToCartesianCoordinate(step2Rod.ImageROI);
                    step2Rod.BitmapImageROIDrawable.IsVisible = step2Rod.IsDeleted == false;
                }

                Document.View.SetViewBounds(_step2BitmapImageDrawable.GetExtents());

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var outputResult = await ModifyBitmapImageROIDrawableGetterEditor.RunAsync<ModifyBitmapImageROIDrawableGetterEditor>(Document.Edit, new ModifyBitmapImageROIDrawableInputOptions(_step2BitmapImageDrawable)
                    {
                        BitmapImageROIDragMoveTypeEnum = BitmapImageROIDragMoveTypeEnum.None,
                        IsDeleteEnabled = true,
                        CancellationToken = cancellationToken
                    });

                    switch (outputResult)
                    {
                        case { OutputResultModeEnum: OutputResultModeEnum.Ok }:
                            try
                            {
                                Rod[] temps = [.. Step2Rods.Where(t => t.BitmapImageROIDrawable.IsVisible)];
                                Guard.IsGreaterThanOrEqualTo(temps.Length, 2);

                                foreach (var (previousRod, nextRod) in temps.Zip(temps.Skip(1)))
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    Guard.IsTrue(previousRod.BitmapImageROIDrawable.Rect is { Height: > 0d });
                                    Guard.IsTrue(nextRod.BitmapImageROIDrawable.Rect is { Height: > 0d });
                                    Guard.IsTrue(nextRod.Index == previousRod.Index + 2);
                                }

                                goto OuterLoop;
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                if (ShouldContinue(ex)) continue;

                                throw;
                            }

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

                foreach (var step2Rod in Step2Rods)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    step2Rod.IsDeleted = step2Rod.BitmapImageROIDrawable.IsVisible == false;
                    step2Rod.BitmapImageROIDrawable.Text = step2Rod.IsDeleted ? $"X: {step2Rod.Index + 1}" : $"{step2Rod.Index + 1}";
                    if (step2Rod.IsDeleted) continue;

                    step2Rod.BitmapImageROIDrawable.IsFixed = true;
                    step2Rod.ImageROI = _step2BitmapImageDrawable.CartesianCoordinateToImageCoordinate(step2Rod.BitmapImageROIDrawable.Rect);
                }

                Rod[] visibleRods = [.. Step2Rods.Where(t => t.BitmapImageROIDrawable.IsVisible)];
                Rod[] invisibleRods = [.. Step2Rods.Where(t => t.BitmapImageROIDrawable.IsVisible == false)];

                if (invisibleRods.Length == 0) return;

                Guard.IsGreaterThanOrEqualTo(visibleRods.Length, 1);

                var averageHeight = visibleRods.Average(t => t.ImageROI.Height);
                var averageY = visibleRods.Average(t => t.ImageROI.Y);

                Guard.IsEqualTo(averageY, 0d);

                foreach (var invisibleRod in invisibleRods)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    invisibleRod.ImageROI = new Rect(invisibleRod.ImageROI.Point, new Size(invisibleRod.ImageROI.Width, averageHeight));

                    invisibleRod.BitmapImageROIDrawable.IsFixed = true;
                    invisibleRod.BitmapImageROIDrawable.Rect = _step2BitmapImageDrawable.ImageCoordinateToCartesianCoordinate(invisibleRod.ImageROI);
                    invisibleRod.BitmapImageROIDrawable.IsVisible = true;
                }
            }

            static bool ShouldContinue(Exception ex)
            {
                var dialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();

                return dialogWindowProvider.TryShowDialog($"""
                                                           Error: {ex.Message}

                                                           Yes: continue to modify ROI.
                                                           No: abort calibration.
                                                           """, out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Warning) == true
                       && dialogResult == DialogResultEnum.Yes;
            }
        }

        public void Review()
        {
            try
            {
                Document.ResetEditROI();

                if (string.IsNullOrWhiteSpace(Step0ChannelImageFilePath)
                    || string.IsNullOrWhiteSpace(Step1ChannelImageFilePath)
                    || string.IsNullOrWhiteSpace(Step2ChannelImageFilePath)) return;

                _step0BitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step0ChannelImageFilePath);

                _step1BitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step1ChannelImageFilePath);
                _step1BitmapImageDrawable.Point = _step0BitmapImageDrawable.Point + new Vector(0d, _step0BitmapImageDrawable.BitmapImage.Height + 10d);

                _step2BitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(Step2ChannelImageFilePath);
                _step2BitmapImageDrawable.Point = _step1BitmapImageDrawable.Point + new Vector(_step1BitmapImageDrawable.BitmapImage.Width + 10d, 0d);

                RestoreRod(Step0LeftRod, _step0BitmapImageDrawable);
                RestoreRod(Step0RightRod, _step0BitmapImageDrawable);
                foreach (var step1Rod in Step1Rods) RestoreRod(step1Rod, _step1BitmapImageDrawable);
                foreach (var step2Rod in Step2Rods) RestoreRod(step2Rod, _step2BitmapImageDrawable);
            }
            finally
            {
                Document.View.ZoomToFit();
            }

            return;

            static void RestoreRod(Rod rod, BitmapImageDrawable bitmapImageDrawable)
            {
                rod.BitmapImageROIDrawable.Text = rod.IsDeleted ? $"X: {rod.Index + 1}" : $"{rod.Index + 1}";
                rod.BitmapImageROIDrawable.IsFixed = true;
                rod.BitmapImageROIDrawable.Rect = bitmapImageDrawable.ImageCoordinateToCartesianCoordinate(rod.ImageROI);
            }
        }

        #endregion

        public void Dispose()
        {
            _step0BitmapImageDrawable.Dispose();
            _step1BitmapImageDrawable.Dispose();
            _step2BitmapImageDrawable.Dispose();
        }
    }
}