using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Core.Utilities.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public sealed class ViewModelCacheCollectorGenerator : IIncrementalGenerator
{
    private const string DefaultCacheAttributeFullName = "Core.Utilities.SourceGenerators.Attributes.DefaultCacheAttribute";
    private const string RecipeCacheAttributeFullName = "Core.Utilities.SourceGenerators.Attributes.RecipeCacheAttribute";
    private const string AdaptToInterfaceMetadataName = "Net.Utilities.Mapper.Interfaces.IAdaptTo`1";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var defaults = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: DefaultCacheAttributeFullName,
                predicate: static (node, _) => node is PropertyDeclarationSyntax or VariableDeclaratorSyntax,
                transform: static (ctx, _) => GetDefault(ctx))
            .Where(static item => string.IsNullOrEmpty(item.ViewModel) == false)
            .Collect();

        var recipes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: RecipeCacheAttributeFullName,
                predicate: static (node, _) => node is PropertyDeclarationSyntax or VariableDeclaratorSyntax,
                transform: static (ctx, _) => GetRecipe(ctx))
            .Where(static item => string.IsNullOrEmpty(item.ViewModel) == false)
            .Collect();

        context.RegisterSourceOutput(defaults.Combine(recipes), static (ctx, source) =>
            ctx.AddSource(
                "SourceGenerators.ViewModelCacheCollector.g.cs",
                SourceText.From(GenerateSource(source.Left, source.Right), Encoding.UTF8)));
    }

    private static (string ViewModel, string Dto, string? Wcf, bool IsArray) GetDefault(GeneratorAttributeSyntaxContext context)
    {
        var (containing, fieldOrPropertyType) = GetTargetSymbols(context);
        if (containing is null || fieldOrPropertyType is null) return (string.Empty, string.Empty, null, false);

        var isArray = fieldOrPropertyType.Kind == SymbolKind.ArrayType;
        var dtoType = isArray && fieldOrPropertyType is IArrayTypeSymbol arraySymbol
            ? arraySymbol.ElementType
            : fieldOrPropertyType;

        var adaptToOpen = context.SemanticModel.Compilation.GetTypeByMetadataName(AdaptToInterfaceMetadataName);
        var wcfType = adaptToOpen is null
            ? null
            : dtoType.AllInterfaces
                .FirstOrDefault(iface => iface.IsGenericType && SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, adaptToOpen))
                ?.TypeArguments[0];

        return (
            containing.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            dtoType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            wcfType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            isArray);
    }

    private static (string ViewModel, string Cache) GetRecipe(GeneratorAttributeSyntaxContext context)
    {
        var (containing, fieldOrPropertyType) = GetTargetSymbols(context);
        if (containing is null || fieldOrPropertyType is null) return (string.Empty, string.Empty);

        return (
            containing.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            fieldOrPropertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    private static (INamedTypeSymbol? Containing, ITypeSymbol? FieldOrPropertyType) GetTargetSymbols(GeneratorAttributeSyntaxContext context)
    {
        var fieldOrPropertyType = context.TargetSymbol switch
        {
            IPropertySymbol property => property.Type,
            IFieldSymbol field => field.Type,
            _ => null
        };

        var containing = context.TargetSymbol.ContainingType;
        if (containing is null || containing.IsGenericType) containing = null;

        return (containing, fieldOrPropertyType);
    }

    private static string GenerateSource(
        ImmutableArray<(string ViewModel, string Dto, string? Wcf, bool IsArray)> defaults,
        ImmutableArray<(string ViewModel, string Cache)> recipes)
    {
        var recipeByVm = recipes
            .GroupBy(r => r.ViewModel, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Cache, StringComparer.Ordinal);

        var body = string.Join("\n", defaults
            .Where(d => recipeByVm.ContainsKey(d.ViewModel))
            .GroupBy(d => d.ViewModel, StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderBy(d => d.ViewModel, StringComparer.Ordinal)
            .Select(d =>
                $"            [typeof({d.ViewModel})] = new(typeof({recipeByVm[d.ViewModel]}), typeof({d.Dto}), {(d.Wcf is null ? "null" : $"typeof({d.Wcf})")}, {d.IsArray.ToString().ToLower()}),"));

        return $$"""
                 // <auto-generated />
                 #nullable enable

                 namespace Core.Utilities
                 {
                     public static class ViewModelCacheCollector
                     {
                         public sealed record CalibrationCacheEntry(
                             global::System.Type CacheType,
                             global::System.Type DtoType,
                             global::System.Type? WcfType,
                             bool IsArray);

                         public static readonly global::System.Collections.Generic.IReadOnlyDictionary<global::System.Type, CalibrationCacheEntry> Map
                             = new global::System.Collections.Generic.Dictionary<global::System.Type, CalibrationCacheEntry>
                             {
                 {{body}}
                             };
                     }
                 }
                 """;
    }
}
