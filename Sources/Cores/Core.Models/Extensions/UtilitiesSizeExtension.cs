using Cuga.Data.DataStruct.Stage;
using Net.Utilities.Models.Geometries;

#if NET
using Semix.GRPC.DTO;

#else
using Semix.WcfTransfer.DTO;

#endif

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

#if NET

    #region SxSizeD

    public static Size ToSize(this SxSizeD sxSizeD) => new((int)sxSizeD.Width, (int)sxSizeD.Height);

    public static SxSizeD ToSxSizeD(this Size size) => new(size.Width, size.Height);

    #endregion SxSizeD

#endif
}