using CommunityToolkit.Diagnostics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;

namespace Core.Utilities;

public static class Interpolator
{
    public static double Linear(Point left, Point right, double t)
    {
        if (t < left.X || right.X < t || left.X > right.X) ThrowHelper.ThrowArgumentException<double>("The point t is outside the interval.");

        var interpolation = MathNet.Numerics.Interpolate.Linear([left.X, right.X], [left.Y, right.Y]);

        return interpolation.Interpolate(t);
    }

    public static double Bilinear(Point3D leftDown, Point3D rightDown, Point3D leftUp, Point3D rightUp, Point t)
    {
        // 检查点 t 是否在矩形内部或边上
        Verify();

        // X方向
        var interpolate1 = Linear(new Point(leftDown.X, leftDown.Z), new Point(rightDown.X, rightDown.Z), t.X);
        var interpolate2 = Linear(new Point(leftUp.X, leftUp.Z), new Point(rightUp.X, rightUp.Z), t.X);

        // Y方向
        return Linear(new Point(leftDown.Y, interpolate1), new Point(leftUp.Y, interpolate2), t.Y);

        void Verify()
        {
            // 检查是否为矩形
            // 计算向量
            var v1 = Vector<double>.Build.Dense([rightDown.X - leftDown.X, rightDown.Y - leftDown.Y]);
            var v2 = Vector<double>.Build.Dense([leftUp.X - leftDown.X, leftUp.Y - leftDown.Y]);
            var v3 = Vector<double>.Build.Dense([rightUp.X - leftUp.X, rightUp.Y - leftUp.Y]);
            var v4 = Vector<double>.Build.Dense([rightUp.X - rightDown.X, rightUp.Y - rightDown.Y]);

            // 检查对边是否相等
            var isEqual = Math.Abs(v1.L2Norm() - v3.L2Norm()) < 1e6 && Math.Abs(v2.L2Norm() - v4.L2Norm()) < 1e6;

            // 检查两两是否为直角（点积为零）
            var isZero = Math.Abs(v1.DotProduct(v2)) < 1e6
                         && Math.Abs(v2.DotProduct(v3)) < 1e6
                         && Math.Abs(v3.DotProduct(v4)) < 1e6;

            if (isEqual == false || isZero == false) ThrowHelper.ThrowArgumentException<double>("The is not a rectangle.");

            // 必须内部或者边上
            if (t.X < leftDown.X || rightDown.X < t.X || t.Y < leftDown.Y || leftUp.Y < t.Y || leftDown.X > rightDown.X || leftDown.Y > leftUp.Y)
                ThrowHelper.ThrowArgumentException<double>("The point t is outside the rectangle.");
        }
    }

    public static bool TryBilinear(Point[,] idealMatrix, bool[,] valueIsOkMatrix, Point[,] valueMatrix, Point t, out Point value)
    {
        value = Point.Origin;

        var xResult = BinarySearch.TryValueIndexRange(
            [.. MatrixUtils.Row(idealMatrix, 0).Select(tt => tt.X)],
            t.X,
            out var startColumnIndex,
            out var endColumnIndex); // x方向寻找行
        var yResult = BinarySearch.TryValueIndexRange(
            [.. MatrixUtils.Column(idealMatrix, 0).Select(tt => tt.Y)],
            t.Y,
            out var startRowIndex,
            out var endRowIndex); // y方向寻找列
        if (xResult == false || yResult == false) return false;

        var leftDownIdeal = idealMatrix[startRowIndex, startColumnIndex];
        var leftDownValueIsOk = valueIsOkMatrix[startRowIndex, startColumnIndex];
        var leftDownValue = valueMatrix[startRowIndex, startColumnIndex];

        var rightDownIdeal = idealMatrix[startRowIndex, endColumnIndex];
        var rightDownValueIsOk = valueIsOkMatrix[startRowIndex, endColumnIndex];
        var rightDownValue = valueMatrix[startRowIndex, endColumnIndex];

        var leftUpIdeal = idealMatrix[endRowIndex, startColumnIndex];
        var leftUpValueIsOk = valueIsOkMatrix[endRowIndex, startColumnIndex];
        var leftUpValue = valueMatrix[endRowIndex, startColumnIndex];

        var rightUpIdeal = idealMatrix[endRowIndex, endColumnIndex];
        var rightUpValueIsOk = valueIsOkMatrix[endRowIndex, endColumnIndex];
        var rightUpValue = valueMatrix[endRowIndex, endColumnIndex];

        if (leftDownValueIsOk == false || rightDownValueIsOk == false || leftUpValueIsOk == false || rightUpValueIsOk == false) return false;

        var valueX = Bilinear(new Point3D(leftDownIdeal.X, leftDownIdeal.Y, leftDownValue.X),
            new Point3D(rightDownIdeal.X, rightDownIdeal.Y, rightDownValue.X),
            new Point3D(leftUpIdeal.X, leftUpIdeal.Y, leftUpValue.X),
            new Point3D(rightUpIdeal.X, rightUpIdeal.Y, rightUpValue.X),
            t);
        var valueY = Bilinear(new Point3D(leftDownIdeal.X, leftDownIdeal.Y, leftDownValue.Y),
            new Point3D(rightDownIdeal.X, rightDownIdeal.Y, rightDownValue.Y),
            new Point3D(leftUpIdeal.X, leftUpIdeal.Y, leftUpValue.Y),
            new Point3D(rightUpIdeal.X, rightUpIdeal.Y, rightUpValue.Y),
            t);

        value = new Point(valueX, valueY);

        return true;
    }
}