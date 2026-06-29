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

public sealed partial class AlignmentTemplateDto :
    ObservableObject,
    ICloneable<AlignmentTemplateDto>,
    IAdaptTo<C2MTemplateDTO>,
    IAdaptIn<C2MTemplateDTO, AlignmentTemplateDto>
{
    /// <summary>
    /// 模板路径
    /// </summary>
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模板图片
    /// </summary>
    [ObservableProperty]
    public partial byte[] Thumb { get; set; } = [];

    /// <summary>
    /// 模板大小
    /// </summary>
    [ObservableProperty]
    public partial Size Size { get; set; }

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
        Name = obj.Name ?? string.Empty;
        Thumb = obj.Thumb is not null ? [.. obj.Thumb] : [];
        Size = obj.Size.ToSize();

        return this;
    }

    #endregion Mapper
}