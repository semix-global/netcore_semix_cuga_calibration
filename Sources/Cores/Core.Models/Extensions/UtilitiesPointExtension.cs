using Cuga.Data.DataStruct.Stage;
using Net.Utilities.Models;

#if NET
using Semix.GRPC.DTO;

#else
using Semix.WcfTransfer.DTO;

#endif

namespace Core.Models.Extensions;

public static class UtilitiesPointExtension
{
    #region CgPoint

    public static CgPoint ToCgPoint(this Point point) => new(point.X, point.Y);

    public static Point ToPoint(this CgPoint? cgPoint) => cgPoint is null ? Point.Empty : new Point(cgPoint.X, cgPoint.Y);

    #endregion CgPoint

    #region SxPointD

    public static SxPointD ToSxPointD(this Point point) => new(point.X, point.Y);

    public static Point ToPoint(this SxPointD? sxPointD) => sxPointD is null ? Point.Empty : new Point(sxPointD.X, sxPointD.Y);

    #endregion SxPointD

    #region System.Drawing.Size

    public static System.Drawing.Point ToSystemDrawingPoint(this Point point)
    {
        var (x, y) = point;
        return new System.Drawing.Point(x, y);
    }

    public static Point ToPoint(this System.Drawing.Point point) => new(point.X, point.Y);

    #endregion System.Drawing.Size
}