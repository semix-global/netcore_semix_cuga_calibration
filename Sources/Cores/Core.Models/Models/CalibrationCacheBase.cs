using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models;

public abstract partial class CalibrationCacheBase : ObservableCacheBase
{
    [ObservableProperty]
    public partial AlgorithmTemplateTypeEnum AlgorithmTemplateTypeEnum { get; set; } = AlgorithmTemplateTypeEnum.Ncc;

    [ObservableProperty]
    public partial AlgorithmTemplateSizeEnum AlgorithmTemplateSizeEnum { get; set; } = AlgorithmTemplateSizeEnum.Size256;
}

public abstract class CalibrationCacheBase<T> : CalibrationCacheBase, ICloneable<T> where T : CalibrationCacheBase<T>
{
    public abstract T Clone();
}