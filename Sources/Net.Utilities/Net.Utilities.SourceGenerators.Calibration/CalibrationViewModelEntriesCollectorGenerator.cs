using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Net.Utilities.SourceGenerators.Calibration;

[Generator(LanguageNames.CSharp)]
public sealed class CalibrationViewModelEntriesCollectorGenerator : IIncrementalGenerator
{
    private const string CalibrationViewModelBaseName = "CugaCalibration.ViewModels.CalibrationViewModelBase";
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
                "SourceGenerators.CalibrationViewModelEntriesCollector.g.cs",
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

    private static (string ViewModel, string Cache, string ShortCache) GetRecipe(GeneratorAttributeSyntaxContext context)
    {
        var (containing, fieldOrPropertyType) = GetTargetSymbols(context);
        if (containing is null || fieldOrPropertyType is null) return (string.Empty, string.Empty, string.Empty);

        return (containing.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            fieldOrPropertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            fieldOrPropertyType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
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
        else
        {
            var baseType = context.SemanticModel.Compilation.GetTypeByMetadataName(CalibrationViewModelBaseName);
            if (baseType is null || IsDerivedFrom(containing, baseType) == false) containing = null;
        }

        return (containing, fieldOrPropertyType);

        static bool IsDerivedFrom(ITypeSymbol? type, INamedTypeSymbol baseType)
        {
            while (type is not null)
            {
                if (SymbolEqualityComparer.Default.Equals(type, baseType)) return true;

                type = type.BaseType;
            }

            return false;
        }
    }

    private static string GenerateSource(
        ImmutableArray<(string ViewModel, string DTO, string? AdaptToCUGA, bool IsArray)> defaults,
        ImmutableArray<(string ViewModel, string Cache, string ShortCache)> recipes)
    {
        var recipeDictionary = recipes
            .GroupBy(r => r.ViewModel, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        var matchedItems = defaults
            .Where(d => recipeDictionary.ContainsKey(d.ViewModel))
            .GroupBy(d => d.ViewModel, StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderBy(d => d.ViewModel, StringComparer.Ordinal)
            .ToList();

        var resultClassesContent = string.Join("\n\n", matchedItems.Select(d =>
        {
            var resultClassName = GetResultClassName(recipeDictionary[d.ViewModel].ShortCache);

            return d.IsArray
                ? $$"""
                        public sealed class {{resultClassName}} : global::CommunityToolkit.Mvvm.ComponentModel.ObservableObject, global::Core.Models.Models.Common.Cookies.ICalibrationViewModelCookie<{{recipeDictionary[d.ViewModel].Cache}}, {{d.DTO}}>
                        {
                            public bool IsArray { get; } = true;
                            public {{recipeDictionary[d.ViewModel].Cache}} Cache { get; set => SetProperty(ref field, value); } = {{recipeDictionary[d.ViewModel].Cache}}.Default;
                            public {{d.DTO}} Calibration
                            {
                                get => global::CommunityToolkit.Diagnostics.ThrowHelper.ThrowNotSupportedException<{{d.DTO}}>();
                                set => global::CommunityToolkit.Diagnostics.ThrowHelper.ThrowNotSupportedException();
                            }
                            public {{d.DTO}}[] Calibrations { get; set => SetProperty(ref field, value); } = {{d.DTO}}.Defaults;
                        }
                    """
                : $$"""
                        public sealed class {{resultClassName}} : global::CommunityToolkit.Mvvm.ComponentModel.ObservableObject, global::Core.Models.Models.Common.Cookies.ICalibrationViewModelCookie<{{recipeDictionary[d.ViewModel].Cache}}, {{d.DTO}}>
                        {
                            public bool IsArray { get; } = false;
                            public {{recipeDictionary[d.ViewModel].Cache}} Cache { get; set => SetProperty(ref field, value); } = {{recipeDictionary[d.ViewModel].Cache}}.Default;
                            public {{d.DTO}} Calibration { get; set => SetProperty(ref field, value); } = {{d.DTO}}.Default;
                            public {{d.DTO}}[] Calibrations
                            {
                                get => global::CommunityToolkit.Diagnostics.ThrowHelper.ThrowNotSupportedException<{{d.DTO}}[]>();
                                set => global::CommunityToolkit.Diagnostics.ThrowHelper.ThrowNotSupportedException();
                            }
                        }
                    """;
        }));

        var mapContent = string.Join("\n", matchedItems.Select(d =>
        {
            var resultClassName = GetResultClassName(recipeDictionary[d.ViewModel].ShortCache);

            return $"""
                                global::Core.Models.Models.Common.Cookies.ApplicationCookie.CalibrationViewModelEntries[typeof({d.ViewModel})] = new(
                                    typeof({d.ViewModel}),
                                    typeof({recipeDictionary[d.ViewModel].Cache}),
                                    typeof({d.DTO}),
                                    {(d.AdaptToCUGA is null ? "null" : $"typeof({d.AdaptToCUGA})")},
                                    {d.IsArray.ToString().ToLower()},
                                    new global::Net.Utilities.SourceGenerators.Calibration.{resultClassName}());
                    """;
        }));

        return $$"""
                 // <auto-generated />
                 #nullable enable

                 namespace Net.Utilities.SourceGenerators.Calibration
                 {
                 {{resultClassesContent}}
                 }

                 namespace Net.Utilities.SourceGenerators.Calibration
                 {
                     public static class CalibrationViewModelEntriesCollector
                     {
                         public static void Init()
                         {
                 {{mapContent}}
                         }
                     }
                 }
                 """;
    }

    private static string GetResultClassName(string shortCache)
    {
        const string cache = "Cache";
        if (shortCache.EndsWith(cache)) shortCache = shortCache.Substring(0, shortCache.Length - cache.Length);

        return shortCache + "Cookie";
    }
}