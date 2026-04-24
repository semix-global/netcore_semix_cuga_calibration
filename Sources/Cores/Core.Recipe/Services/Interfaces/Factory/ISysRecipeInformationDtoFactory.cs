using Local.SQL.DB.Providers.Models.Entities.DTO;

namespace Core.Recipe.Services.Interfaces.Factory;

public interface ISysRecipeInformationDtoFactory
{
    SysRecipeInformationDTO Create(string recipeName, string? description = null);
    SysRecipeInformationDTO CreateFrom(SysRecipeInformationDTO source, string newName);
}