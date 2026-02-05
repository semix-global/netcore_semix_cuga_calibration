using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

#if NET
using Semix.GRPC.DTO;
#else
using Semix.WcfTransfer.DTO;

#endif

namespace Core.Models.Models.Common.Alignment;

public sealed partial class AlignmentResultDto : ObservableObject, ICloneable<AlignmentResultDto>, IAdaptTo<C2MAlignResult>, IAdaptIn<C2MAlignResult, AlignmentResultDto>
{
    /// <summary>
    /// 对准旋转的角度
    /// </summary>
    [ObservableProperty]
    private double _degrees;

    /// <summary>
    /// mark点1坐标
    /// </summary>
    [ObservableProperty]
    private Point _markPoint1;

    /// <summary>
    /// mark点2坐标
    /// </summary>
    [ObservableProperty]
    private Point _markPoint2;

    #region Mapper

    public AlignmentResultDto Clone() => new()
    {
        Degrees = Degrees,
        MarkPoint1 = MarkPoint1,
        MarkPoint2 = MarkPoint2
    };

    public C2MAlignResult AdaptTo() => new()
    {
        Degrees = Degrees,
        EndPoint1 = MarkPoint1.ToSxPointD(),
        EndPoint2 = MarkPoint2.ToSxPointD()
    };

    public AlignmentResultDto AdaptIn(C2MAlignResult obj)
    {
        Guard.IsNotNull(obj);

        Degrees = obj.Degrees;
        MarkPoint1 = obj.EndPoint1.ToPoint();
        MarkPoint2 = obj.EndPoint2.ToPoint();

        return this;
    }

    #endregion Mapper

    public object ToHtmlAnonymous() => new
    {
        Degrees,
        MarkPoint1,
        MarkPoint2
    };
}