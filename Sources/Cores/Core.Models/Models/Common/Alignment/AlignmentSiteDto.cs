using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Setting;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

#if NET
using Semix.GRPC.DTO;

#else
using Semix.WcfTransfer.DTO;

#endif

namespace Core.Models.Models.Common.Alignment;

public sealed partial class AlignmentSiteDto :
    ObservableObject,
    ICloneable<AlignmentSiteDto>,
    IAdaptTo<C2MSiteDTO>,
    IAdaptIn<C2MSiteDTO, AlignmentSiteDto>
{
    [ObservableProperty]
    public partial Point Location { get; set; }

    [ObservableProperty]
    public partial AlignmentTemplateDto? Template { get; set; } = new();

    [ObservableProperty]
    public partial TemplateMatchSetting TemplateMatchSetting { get; set; } = new();

    [ObservableProperty]
    public partial double TemplateMatchScoreThreshold { get; set; } = 0.8;

    public void UpdateTemplateMatchScoreThreshold(CalibrationSetting calibrationSetting)
    {
        // alignment 底层固定 Halcon 引擎：阈值按 Halcon 模板类型（Ncc/Sharpe）分发，不随全局引擎切换
        TemplateMatchScoreThreshold = TemplateMatchSetting.Halcon.TemplateType.ToTemplateMatchScoreThreshold(calibrationSetting);
    }

    public AlignmentSiteDto DegreeAngleByXy(double degree)
    {
        var result = Clone();
        result.Location = Location.DegreeAngleByXy(degree);
        return result;
    }

    #region Mapper

    public AlignmentSiteDto Clone() => new()
    {
        Location = Location,
        Template = Template?.Clone(),
        TemplateMatchSetting = TemplateMatchSetting.Clone(),
        TemplateMatchScoreThreshold = TemplateMatchScoreThreshold
    };

    public C2MSiteDTO AdaptTo() => new()
    {
        Location = Location.ToSxPointD(),
        Template = Template?.AdaptTo(),
        // 协议收口点：底层仅支持 Halcon 匹配，模板类型经 Halcon 子对象编码（底层替换为 algo 时仅需修改此处）
        Algo = TemplateMatchSetting.Halcon.TemplateType.ToAlgorithmTemplateType(),
        Threshold = TemplateMatchScoreThreshold
    };

    public AlignmentSiteDto AdaptIn(C2MSiteDTO obj)
    {
        Location = obj.Location.ToPoint();
        Template = obj.Template is not null ? new AlignmentTemplateDto().AdaptIn(obj.Template) : null;
        // 协议收口点：远端模板类型解码回 Halcon 子对象（底层替换为 algo 时仅需修改此处）
        TemplateMatchSetting = new TemplateMatchSetting { Halcon = new HalconTemplateMatchSetting { TemplateType = obj.Algo.ToAlgorithmTemplateTypeEnum() } };
        TemplateMatchScoreThreshold = obj.Threshold;

        return this;
    }

    #endregion Mapper
}