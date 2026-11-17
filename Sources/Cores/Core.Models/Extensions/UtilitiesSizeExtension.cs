using Cuga.Data.DataStruct.Stage;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Extensions;

public static class UtilitiesSizeExtension
{
    #region CgSize

    public static CgSize ToCgSize(this Size size) => new(size.Width, size.Height);

    public static Size ToSize(this CgSize? cgSize) => cgSize is null ? Size.Empty : new Size(cgSize.Width, cgSize.Height);

    #endregion CgSize

    #region System.Drawing.Size

    public static System.Drawing.Size ToSystemDrawingSize(this Size size)
    {
        var (width, height) = (SizeI)size;

        return new System.Drawing.Size(width, height);
    }

    public static Size ToSize(this System.Drawing.Size size) => new(size.Width, size.Height);

    #endregion System.Drawing.Size
}