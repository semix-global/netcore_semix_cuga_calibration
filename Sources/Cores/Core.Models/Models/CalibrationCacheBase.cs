using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Local.SQL.Cache.Providers.Bases;
using Local.SQL.DB.Providers.Models.Entities.Base.Interface;

namespace Core.Models.Models;

public partial class CalibrationCacheBase : ObservableCacheBase, IEntityAdd
{
    [ObservableProperty]
    private AlgorithmTemplateTypeEnum _algorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum.Ncc;

    [ObservableProperty]
    private AlgorithmTemplateSizeEnum _algorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum.Size256;

    [ObservableProperty]
    private long _createdUserId;

    [ObservableProperty]
    private string _createdUserName = string.Empty;
}