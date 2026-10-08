using Core.Algorithm.Interfaces.TemplateMatch;
using Core.Models.Enums.Algorithm;
using Core.Models.Extensions;
using HalconDotNet;

namespace Core.Algorithm.Implements.Halcon;

internal sealed class HalconTemplateMatchModel(
    AlgorithmTemplateTypeEnum templateType,
    HTuple templateId,
    HAlgorithm.Algorithm algorithm) : ITemplateMatchModel
{
    public AlgorithmTemplateTypeEnum TemplateType { get; } = templateType;

    public HTuple TemplateId { get; } = templateId;

    public void Dispose() => algorithm.HClearModel(TemplateType.ToAlgorithmTemplateType(), TemplateId);
}
