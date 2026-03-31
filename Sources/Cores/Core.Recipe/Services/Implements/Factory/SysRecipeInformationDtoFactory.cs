using Core.Recipe.Services.Interfaces.Factory;
using Core.Utilities;
using Local.SQL.Cache.Providers.Helpers;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using System.IO;

namespace Core.Recipe.Services.Implements.Factory;

[IOCAppService(ServiceType = typeof(ISysRecipeInformationDtoFactory), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class SysRecipeInformationDtoFactory(IOptions<ApplicationSetting> options) : ISysRecipeInformationDtoFactory
{
    private readonly ApplicationSetting _settings = options.Value;

    public SysRecipeInformationDto Create(string recipeName, string? description = null)
    {
        return new SysRecipeInformationDto
        {
            RecipeDbName = recipeName,
            DescribeInformation = description ?? recipeName,
            RecipeNosqlRecipeDbDataSource = BuildConnectionString(recipeName)
        };
    }

    public SysRecipeInformationDto CreateFrom(SysRecipeInformationDto source, string newName)
    {
        var cloned = source.Clone();
        cloned.RecipeDbName = newName;
        cloned.RecipeNosqlRecipeDbDataSource = BuildConnectionString(newName);
        return cloned;
    }

    private string BuildConnectionString(string recipeName) =>
        SQLiteHelper.GetConnectionString(
            Path.Combine(_settings.NosqlDbDataSourceDirectory, recipeName, _settings.RecipeDBName));
}
