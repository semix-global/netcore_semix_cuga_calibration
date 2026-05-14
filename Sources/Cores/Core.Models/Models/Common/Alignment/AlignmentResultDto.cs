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

public sealed partial class AlignmentResultDto : ObservableObject, ICloneable<AlignmentResultDto>, IAdaptTo<C2MAlignResult>, IAdaptIn<C2MAlignResult, AlignmentResultDto>, IAdaptIn<AlignmentResultDto, AlignmentResultDto>
{
    /// <summary>
    /// 对准旋转的角度
    /// </summary>
    [ObservableProperty]
    public partial double Degrees { get; set; }

    /// <summary>
    /// mark点1坐标
    /// </summary>
    [ObservableProperty]
    public partial Point MarkPoint1 { get; set; }

    /// <summary>
    /// mark点2坐标
    /// </summary>
    [ObservableProperty]
    public partial Point MarkPoint2 { get; set; }

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
        Degrees = obj.Degrees;
        MarkPoint1 = obj.EndPoint1.ToPoint();
        MarkPoint2 = obj.EndPoint2.ToPoint();

        return this;
    }

    public AlignmentResultDto AdaptIn(AlignmentResultDto obj)
    {
        Degrees = obj.Degrees;
        MarkPoint1 = obj.MarkPoint1;
        MarkPoint2 = obj.MarkPoint2;

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