using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;

/// <summary>
/// BitmapImageDrawable 图片坐标系 (原点左上, Y 向下) 与笛卡尔坐标系 (原点左下, Y 向上) 的转换扩展.
/// 两个 Round 方法的名字约定为输入点所在的坐标系, 均取整到所在像素的左上角;
/// 坐标为整数格点且 BitmapImageDrawable.Point 为整数时, 两个方向转换往返恒等.
/// </summary>
public static class BitmapImageDrawableExtensions
{
    extension(Point @this)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [TargetedPatchingOptOut(Constants.TargetedPatchingOptOutReason)]
        public Point CartesianCoordinateRound()
        {
            // 笛卡尔坐标系 (Y 向上) 输入: 取整到所在像素的左上角
            return new Point(Math.Floor(@this.X), Math.Ceiling(@this.Y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [TargetedPatchingOptOut(Constants.TargetedPatchingOptOutReason)]
        public Point ImageCoordinateRound()
        {
            // 图片坐标系 (Y 向下) 输入: 取整到所在像素的左上角
            return new Point(Math.Floor(@this.X), Math.Floor(@this.Y));
        }
    }

    extension(Rect @this)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [TargetedPatchingOptOut(Constants.TargetedPatchingOptOutReason)]
        public Rect CartesianCoordinateRound()
        {
            var extents = new Extents();
            extents.Add(@this.XMinYMin.CartesianCoordinateRound());
            extents.Add(@this.XMaxYMax.CartesianCoordinateRound());

            return (Rect)extents;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [TargetedPatchingOptOut(Constants.TargetedPatchingOptOutReason)]
        public Rect ImageCoordinateRound()
        {
            var extents = new Extents();
            extents.Add(@this.XMinYMin.ImageCoordinateRound());
            extents.Add(@this.XMaxYMax.ImageCoordinateRound());

            return (Rect)extents;
        }
    }

    extension(BitmapImageDrawable @this)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [TargetedPatchingOptOut(Constants.TargetedPatchingOptOutReason)]
        public Point CartesianCoordinateToImageCoordinate(Point cartesianCoordinatePoint)
        {
            if (@this.BitmapImage?.IsEmpty != false) return Point.Origin;

            var vector = cartesianCoordinatePoint.CartesianCoordinateRound() - @this.Point;

            return new Point(vector.X, @this.BitmapImage.Height - vector.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [TargetedPatchingOptOut(Constants.TargetedPatchingOptOutReason)]
        public Point ImageCoordinateToCartesianCoordinate(Point imageCoordinatePoint)
        {
            if (@this.BitmapImage?.IsEmpty != false) return Point.Origin;

            var cartesianCoordinateRound = imageCoordinatePoint.ImageCoordinateRound();
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