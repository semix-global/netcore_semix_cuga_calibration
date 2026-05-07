using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Local.SQL.Cache.Providers.Bases;

namespace Core.Models.Models;

public partial class CalibrationCacheBase : ObservableCacheBase
{
    [ObservableProperty]
    public partial AlgorithmTemplateTypeEnum AlgorithmTemplateTypeEnum { get; set; } = AlgorithmTemplateTypeEnum.Ncc;

    [ObservableProperty]
    public partial AlgorithmTemplateSizeEnum AlgorithmTemplateSizeEnum { get; set; } = AlgorithmTemplateSizeEnum.Size256;
}