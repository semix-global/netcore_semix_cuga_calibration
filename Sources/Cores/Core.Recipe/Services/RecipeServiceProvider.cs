using Core.Recipe.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SourceGenerator.InjectHostDI;

namespace Core.Recipe.Services;

public static class RecipeServiceProvider
{
    public static IServiceCollection AddRecipeService(this IServiceCollection services, IHostEnvironment hostEnvironment)
    {
        services.AddCoreRecipeInjectHostDI(hostEnvironment);

        services.AddSingleton(new RecipeCookie());

        return services;
    }
}