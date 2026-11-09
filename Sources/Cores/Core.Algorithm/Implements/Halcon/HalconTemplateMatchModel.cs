using Core.Algorithm.Interfaces.TemplateMatch;
using Core.Models.Enums.Algorithm;
using Core.Models.Extensions;
using HalconDotNet;
using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Implements.Halcon;

internal sealed class HalconTemplateMatchModel(
    AlgorithmTemplateTypeEnum templateType,
    HTuple templateId,
    HAlgorithm.Algorithm algorithm,
    Point centerOffset = default) : ITemplateMatchModel
{
    public AlgorithmTemplateTypeEnum TemplateType { get; } = templateType;

    public HTuple TemplateId { get; } = templateId;

    public Point CenterOffset { get; } = centerOffset;

    public void Dispose() => algorithm.HClearModel(TemplateType.ToAlgorithmTemplateType(), TemplateId);
}