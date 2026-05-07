using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Core.Utilities.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public sealed class ViewModelCacheCollectorGenerator : IIncrementalGenerator
{
    private const string AdaptToInterfaceMetadataName = "Net.Utilities.Mapper.Interfaces.IAdaptTo`1";
    private const string AdaptToTargetNamespace = "Core.Wcf.Models";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var defaults = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: CacheSourceGenerator.DefaultCacheAttributeFullName,
                predicate: static (node, _) => node is PropertyDeclarationSyntax or VariableDeclaratorSyntax,
                transform: static (ctx, _) => GetDefault(ctx))
            .Where(static item => string.IsNullOrEmpty(item.ViewModel) == false)
            .Collect();

        var recipes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: CacheSourceGenerator.RecipeCacheAttributeFullName,
                predicate: static (node, _) => node is PropertyDeclarationSyntax or VariableDeclaratorSyntax,
                transform: static (ctx, _) => GetRecipe(ctx))
            .Where(static item => string.IsNullOrEmpty(item.ViewModel) == false)
            .Collect();

        context.RegisterSourceOutput(defaults.Combine(recipes), static (ctx, source) =>
            ctx.AddSource(
                "SourceGenerators.ViewModelCacheCollector.g.cs",
                SourceText.From(GenerateSource(source.Left, source.Right), Encoding.UTF8)));
    }

    private static (string ViewModel, string DTO, string? AdaptToCUGA, bool IsArray) GetDefault(GeneratorAttributeSyntaxContext context)
    {
        var (containing, fieldOrPropertyType) = GetTargetSymbols(context);
        if (containing is null || fieldOrPropertyType is null) return (string.Empty, string.Empty, null, false);

        var isArray = fieldOrPropertyType.Kind == SymbolKind.ArrayType;
        var dto = isArray && fieldOrPropertyType is IArrayTypeSymbol arraySymbol
            ? arraySymbol.ElementType
            : fieldOrPropertyType;

        var adaptToOpen = context.SemanticModel.Compilation.GetTypeByMetadataName(AdaptToInterfaceMetadataName); // 按元数据全名查找一个实现接口的第一个参数作为AdaptTo类型
        var adaptToCUGA = adaptToOpen is null
            ? null
            : dto.AllInterfaces
                .Where(t => t.IsGenericType && SymbolEqualityComparer.Default.Equals(t.OriginalDefinition, adaptToOpen))
                .Select(t => t.TypeArguments[0])
                .FirstOrDefault(t => t.ContainingNamespace?.ToDisplayString() is { } ns && ns.StartsWith(AdaptToTargetNamespace));

        return (
            containing.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            dto.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            adaptToCUGA?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            isArray);
    }

    private static (string ViewModel, string Cache) GetRecipe(GeneratorAttributeSyntaxContext context)
    {
        var (containing, fieldOrPropertyType) = GetTargetSymbols(context);
        if (containing is null || fieldOrPropertyType is null) return (string.Empty, string.Empty);

        return (containing.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            fieldOrPropertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    private static (ITypeSymbol? Containing, ITypeSymbol? FieldOrPropertyType) GetTargetSymbols(GeneratorAttributeSyntaxContext context)
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
        ImmutableArray<(string ViewModel, string DTO, string? AdaptToCUGA, bool IsArray)> defaults,
        ImmutableArray<(string ViewModel, string Cache)> recipes)
    {
        var recipeDictionary = recipes
            .GroupBy(r => r.ViewModel, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Cache, StringComparer.Ordinal);

        var content = string.Join("\n", defaults
            .Where(d => recipeDictionary.ContainsKey(d.ViewModel))
            .GroupBy(d => d.ViewModel, StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderBy(d => d.ViewModel, StringComparer.Ordinal)
            .Select(d => $$"""
                                       [typeof({{d.ViewModel}})] = new()
                                       {
                                           CacheType = typeof({{recipeDictionary[d.ViewModel]}}),
                                           DTOType = typeof({{d.DTO}}),
                                           AdaptToCUGAType = {{(d.AdaptToCUGA is null ? "null" : $"typeof({d.AdaptToCUGA})")}},
                                           IsArray = {{d.IsArray.ToString().ToLower()}}
                                       },
                           """));

        return $$"""
                 // <auto-generated />
                 #nullable enable

                 namespace Core.Utilities
                 {
                     public sealed class CalibrationCacheEntry : global::CommunityToolkit.Mvvm.ComponentModel.ObservableObject
                     {
                         public global::System.Type CacheType { get; set => SetProperty(ref field, value); } = typeof(object);
                         public global::System.Type DTOType { get; set => SetProperty(ref field, value); } = typeof(object);
                         public global::System.Type? AdaptToCUGAType { get; set => SetProperty(ref field, value); }
                         public bool IsArray { get; set => SetProperty(ref field, value); }
                     }

                     public static class ViewModelCacheCollector
                     {
                         public static readonly global::System.Collections.Generic.IReadOnlyDictionary<global::System.Type, CalibrationCacheEntry> Map = new global::System.Collections.Generic.Dictionary<global::System.Type, CalibrationCacheEntry>
                         {
                 {{content}}
                         };
                     }
                 }
                 """;
    }
}