using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
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

public sealed partial class AlignmentSiteDto : ObservableCacheBase, ICloneable<AlignmentSiteDto>, IAdaptTo<C2MSiteDTO>, IAdaptIn<C2MSiteDTO, AlignmentSiteDto>
{
    [ObservableProperty]
    private Point _location;

    [ObservableProperty]
    private AlignmentTemplateDto? _template = new();

    [ObservableProperty]
    private AlgorithmTemplateTypeEnum _algorithmTemplateTypeEnum;

    [ObservableProperty]
    private double _templateMatchScoreThreshold = 0.8;

    public void UpdateTemplateMatchScoreThreshold(Setting.CalibrationSetting calibrationSetting)
    {
        TemplateMatchScoreThreshold = AlgorithmTemplateTypeEnum.ToTemplateMatchScoreThreshold(calibrationSetting);
    }

    #region Mapper

    public AlignmentSiteDto Clone() => new()
    {
        Location = Location,
        Template = Template?.Clone(),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        TemplateMatchScoreThreshold = TemplateMatchScoreThreshold,
        Id = Id,
        Expiration = Expiration
    };

    public C2MSiteDTO AdaptTo() => new()
    {
        Location = Location.ToSxPointD(),
        Template = Template?.AdaptTo(),
        Algo = AlgorithmTemplateTypeEnum.ToAlgorithmTemplateType(),
        Threshold = TemplateMatchScoreThreshold
    };

    public AlignmentSiteDto AdaptIn(C2MSiteDTO obj)
    {
        Guard.IsNotNull(obj, nameof(obj));

        Location = obj.Location.ToPoint();
        Template = obj.Template is not null ? new AlignmentTemplateDto().AdaptIn(obj.Template) : null;
        AlgorithmTemplateTypeEnum = obj.Algo.ToAlgorithmTemplateTypeEnum();
        TemplateMatchScoreThreshold = obj.Threshold;

        return this;
    }

    #endregion Mapper
}