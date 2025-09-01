using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Models.Common.Pattern;
using Local.NoSQL.DB.Providers.Bases;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.Recipe.Info;

public partial class CalibrationRecipeInfoDto : ObservableCacheBase, ICloneable<CalibrationRecipeInfoDto>, IAdaptTo<SysRecipeInformationDto>, IAdaptIn<SysRecipeInformationDto, CalibrationRecipeInfoDto>
{
    [ObservableProperty]
    private string _recipeName = "Default";

    [ObservableProperty]
    private string _describeName = "Default";

    public string RecipeDbName => "cache.db";

    [ObservableProperty]
    private string _recipeNosqlRecipeDbDataSource = string.Empty;

    [ObservableProperty]
    private AlgorithmWaferTypeEnum _waferTypeEnum = AlgorithmWaferTypeEnum.D300;

    [ObservableProperty]
    private NotchDirectionTypeEnum _notchDirectionEnum = NotchDirectionTypeEnum.Down;

    /// <summary>
    /// 低倍率
    /// </summary>
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLowMag = MicroscopeLensInformation.Default;

    /// <summary>
    /// 高倍率
    /// </summary>
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeHighMag =  MicroscopeLensInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    public CalibrationRecipeInfoDto Clone() => new()
    {
        RecipeName = RecipeName,
        DescribeName = DescribeName,
        NotchDirectionEnum = NotchDirectionEnum,
        MicroscopeLowMag = MicroscopeLowMag.Clone(),
        MicroscopeHighMag = MicroscopeHighMag.Clone(),
        CIBConfiguration = CIBConfiguration.Clone(),
        RecipeNosqlRecipeDbDataSource = RecipeNosqlRecipeDbDataSource,
        Id = Id
    };

    public SysRecipeInformationDto AdaptTo() => new()
    {
        RecipeDbName = RecipeName,
        DescribeInformation = DescribeName,
        RecipeNosqlRecipeDbDataSource = RecipeNosqlRecipeDbDataSource,
        Id = Id
    };

    public CalibrationRecipeInfoDto AdaptIn(SysRecipeInformationDto obj)
    {
        return new CalibrationRecipeInfoDto
        {
            RecipeName = obj.RecipeDbName,
            DescribeName = obj.DescribeInformation,
            RecipeNosqlRecipeDbDataSource = obj.RecipeNosqlRecipeDbDataSource
        };
    }
}