using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;

namespace Local.SQL.DB.Providers.Models.Entities.DTO;

public sealed partial class SysRecipeInformationDto : SysBaseDto, ICloneable<SysRecipeInformationDto>, IAdaptTo<SysRecipeInformation>, IAdaptIn<SysRecipeInformation, SysRecipeInformationDto>
{
    [ObservableProperty]
    private string _recipeDbName = "Default";

    [ObservableProperty]
    private string _describeInformation = "Default";

    [ObservableProperty]
    private string _recipeNosqlRecipeDbDataSource = string.Empty;

    #region Mapper

    public SysRecipeInformationDto AdaptIn(SysRecipeInformation obj)
    {
        Guard.IsNotNull(obj, nameof(obj));

        RecipeDbName = obj.RecipeDbName;
        DescribeInformation = obj.DescribeInformation;
        RecipeNosqlRecipeDbDataSource = obj.RecipeNosqlRecipeDbDataSource;
        Id = obj.Id;
        CreatedUserId = obj.CreatedUserId;
        CreatedUserName = obj.CreatedUserName;
        CreatedTime = obj.CreatedTime;
        ModifiedUserId = obj.ModifiedUserId;
        ModifiedUserName = obj.ModifiedUserName;
        ModifiedTime = obj.ModifiedTime;
        Remark = obj.Remark;
        IsDeleted = obj.IsDeleted;

        return this;
    }

    public SysRecipeInformation AdaptTo() => new()
    {
        RecipeDbName = RecipeDbName,
        DescribeInformation = DescribeInformation,
        RecipeNosqlRecipeDbDataSource = RecipeNosqlRecipeDbDataSource,
        Id = Id,
        CreatedUserId = CreatedUserId,
        CreatedUserName = CreatedUserName,
        CreatedTime = CreatedTime,
        ModifiedUserId = ModifiedUserId,
        ModifiedUserName = ModifiedUserName,
        ModifiedTime = ModifiedTime,
        Remark = Remark,
        IsDeleted = IsDeleted
    };

    public SysRecipeInformationDto Clone()
    {
        return new SysRecipeInformationDto()
        {
            RecipeDbName = RecipeDbName,
            DescribeInformation = DescribeInformation,
            RecipeNosqlRecipeDbDataSource = RecipeNosqlRecipeDbDataSource,
            Id = Id,
            CreatedUserId = CreatedUserId,
            CreatedUserName = CreatedUserName,
            CreatedTime = CreatedTime,
            ModifiedUserId = ModifiedUserId,
            ModifiedUserName = ModifiedUserName,
            ModifiedTime = ModifiedTime,
            Remark = Remark,
            IsDeleted = IsDeleted
        };
    }

    #endregion Mapper
}