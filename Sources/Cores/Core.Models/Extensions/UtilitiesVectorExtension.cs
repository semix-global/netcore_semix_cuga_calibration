using Cuga.Data.DataStruct.Stage;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Extensions;

public static class UtilitiesVectorExtension
{
    #region CgPoint

    public static CgPoint ToCgPoint(this Vector point) => new(point.X, point.Y);

    public static Vector ToVector(this CgPoint? cgPoint) => cgPoint is null ? Vector.Zero : new Vector(cgPoint.X, cgPoint.Y);

    #endregion CgPoint
}