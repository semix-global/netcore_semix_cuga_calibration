using Local.SQL.DB.Providers.Models.Entities.DTO;

namespace Core.Recipe.Services.Interfaces.Factory;

public interface ISysRecipeInformationDtoFactory
{
    SysRecipeInformationDto Create(string recipeName, string? description = null);
    SysRecipeInformationDto CreateFrom(SysRecipeInformationDto source, string newName);
}
