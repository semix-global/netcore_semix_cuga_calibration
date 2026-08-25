using System.Runtime;
using System.Runtime.CompilerServices;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using BitmapImageDrawable = Net.Utilities.OpticsFourierImageViewer.WPF.Drawables.BitmapImageDrawable;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;

public static class BitmapImageDrawableExtensions
{
    extension(Point point)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [TargetedPatchingOptOut(Constants.TargetedPatchingOptOutReason)]
        public Point ImageCoordinateRound()
        {
            // 向左上角收敛 (图片坐标系左上角origin点)
            return new Point(Math.Floor(point.X), Math.Ceiling(point.Y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [TargetedPatchingOptOut(Constants.TargetedPatchingOptOutReason)]
        public Point CartesianCoordinateRound()
        {
            // 向左下角收敛 (图片坐标系左下角origin点)
            return new Point(Math.Floor(point.X), Math.Floor(point.Y));
        }
    }

    extension(BitmapImageDrawable @this)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [TargetedPatchingOptOut(Constants.TargetedPatchingOptOutReason)]
        public Point CartesianCoordinateToImageCoordinate(Point cartesianCoordinatePoint)
        {
            if (@this.BitmapImage?.IsEmpty != false) return Point.Origin;

            var vector = cartesianCoordinatePoint.ImageCoordinateRound() - @this.Point;

            return new Point(vector.X, @this.BitmapImage.Height - vector.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [TargetedPatchingOptOut(Constants.TargetedPatchingOptOutReason)]
        public Point ImageCoordinateToCartesianCoordinate(Point imageCoordinatePoint)
        {
            if (@this.BitmapImage?.IsEmpty != false) return Point.Origin;

            var cartesianCoordinateRound = imageCoordinatePoint.CartesianCoordinateRound();
            var point = new Point(cartesianCoordinateRound.X, @this.BitmapImage.Height - cartesianCoordinateRound.Y);

            return point + (Vector)@this.Point;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [TargetedPatchingOptOut(Constants.TargetedPatchingOptOutReason)]
        public Rect CartesianCoordinateToImageCoordinate(Rect cartesianCoordinateRect)
        {
            if (@this.BitmapImage?.IsEmpty != false) return Rect.Empty;

            var startPoint = @this.CartesianCoordinateToImageCoordinate(cartesianCoordinateRect.XMinYMin);
            var endPoint = @this.CartesianCoordinateToImageCoordinate(cartesianCoordinateRect.XMaxYMax);

            var rect = (Rect)new Extents(startPoint, endPoint);

            return rect;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [TargetedPatchingOptOut(Constants.TargetedPatchingOptOutReason)]
        public Rect ImageCoordinateToCartesianCoordinate(Rect imageCoordinateRect)
        {
            if (@this.BitmapImage?.IsEmpty != false) return Rect.Empty;

            var startPoint = @this.ImageCoordinateToCartesianCoordinate(imageCoordinateRect.XMinYMin);
            var endPoint = @this.ImageCoordinateToCartesianCoordinate(imageCoordinateRect.XMaxYMax);

            return (Rect)new Extents(startPoint, endPoint);
        }
    }
}