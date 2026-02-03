using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

#if NET
using Semix.GRPC.DTO;
#else
using Semix.WcfTransfer.DTO;

#endif

namespace Core.Models.Models.Common.Alignment;

public sealed partial class AlignmentTemplateDto : ObservableObject, ICloneable<AlignmentTemplateDto>, IAdaptTo<C2MTemplateDTO>, IAdaptIn<C2MTemplateDTO, AlignmentTemplateDto>
{
    /// <summary>
    /// 模板路径
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// 模板图片
    /// </summary>
    [ObservableProperty]
    private byte[] _thumb = [];

    /// <summary>
    /// 模板大小
    /// </summary>
    [ObservableProperty]
    private Size _size;

    #region Mapper

    public AlignmentTemplateDto Clone() => new()
    {
        Name = Name,
        Thumb = [.. Thumb],
        Size = Size
    };

    public C2MTemplateDTO AdaptTo() => new()
    {
        Name = Name,
        Thumb = [.. Thumb],
        Size = Size.ToSystemDrawingSize()
    };

    public AlignmentTemplateDto AdaptIn(C2MTemplateDTO obj)
    {
        Guard.IsNotNull(obj);

        Name = obj.Name ?? string.Empty;
        Thumb = obj.Thumb is not null ? [.. obj.Thumb] : [];
        Size = obj.Size.ToSize();

        return this;
    }

    #endregion Mapper
}