using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Reflection;
using System.Resources;
using System.Runtime.Versioning;
using System.Text;

namespace SourceGenerator.AssemblyMetadata;

/// <summary>
/// Assembly元数据生成器
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class AssemblyMetadataGenerator : IIncrementalGenerator
{
    private static readonly HashSet<string> Attributes =
    [
        nameof(AssemblyCompanyAttribute),
        nameof(AssemblyConfigurationAttribute),
        nameof(AssemblyCopyrightAttribute),
        nameof(AssemblyCultureAttribute),
        nameof(AssemblyDelaySignAttribute),
        nameof(AssemblyDescriptionAttribute),
        nameof(AssemblyFileVersionAttribute),
        nameof(AssemblyInformationalVersionAttribute),
        nameof(AssemblyKeyFileAttribute),
        nameof(AssemblyKeyNameAttribute),
        nameof(AssemblyMetadataAttribute),
        nameof(AssemblyProductAttribute),
        nameof(AssemblySignatureKeyAttribute),
        nameof(AssemblyTitleAttribute),
        nameof(AssemblyTrademarkAttribute),
        nameof(AssemblyVersionAttribute),
        nameof(NeutralResourcesLanguageAttribute),
        nameof(TargetFrameworkAttribute),
        "UserSecretsIdAttribute"
    ];

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var constants = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: "System.Reflection.AssemblyVersionAttribute",
                predicate: static (syntaxNode, _) => syntaxNode is CompilationUnitSyntax,
                transform: SemanticTransform
            )
            .Where(static context => context is not null && context.Count > 0);
        var assemblyName = context.CompilationProvider
            .Select(static (c, _) => c.Assembly.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        var globalOptions = context.AnalyzerConfigOptionsProvider
            .Select(static (c, _) =>
            {
                c.GlobalOptions.TryGetValue("build_property.DefineConstants", out var defineConstants);
                c.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);

                var globalOptions = new GlobalOptions(
                    DefineConstants: defineConstants,
                    RootNamespace: rootNamespace);
                return globalOptions;
            });

        var options = assemblyName.Combine(globalOptions);

        context.RegisterSourceOutput(constants.Combine(options), GenerateOutput);
    }

    private static List<AssemblyConstant> SemanticTransform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var attributes = context.TargetSymbol.GetAttributes(); // 从目标中获取所有的assembly特性, eg: [assembly: AssemblyVersion("2.6.8.11")]
        var constantList = new List<AssemblyConstant>();

        foreach (var attribute in attributes)
        {
            var name = attribute.AttributeClass?.Name;
            if (name is null || Attributes.Contains(name) == false) continue;

            if (attribute.ConstructorArguments.Length == 1)
            {
                if (name.Length > nameof(Assembly).Length && name.StartsWith(nameof(Assembly))) name = name[nameof(Assembly).Length..]; // remove Assembly
                if (name.Length > nameof(Attribute).Length) name = name[..^nameof(Attribute).Length]; // remove Attribute
                name = SourceGeneratorHelper.ToPascalCaseName(name);

                var argument = attribute.ConstructorArguments.FirstOrDefault();
                var value = argument.ToCSharpString(); // 字符串中包含引号

                if (string.IsNullOrWhiteSpace(name)) continue;
                if (constantList.Any(c => c.Name == name)) continue; // prevent duplicates

                var constant = new AssemblyConstant(name, value);
                constantList.Add(constant);
            }
            else if (name == nameof(AssemblyMetadataAttribute) && attribute.ConstructorArguments.Length == 2)
            {
                var nameArgument = attribute.ConstructorArguments[0];
                var key = nameArgument.Value?.ToString() ?? string.Empty;
                key = SourceGeneratorHelper.ToPascalCaseName(key);

                var valueArgument = attribute.ConstructorArguments[1];
                var value = valueArgument.ToCSharpString();

                if (string.IsNullOrWhiteSpace(key)) continue;
                if (constantList.Any(c => c.Name == key)) continue; // prevent duplicates

                var constant = new AssemblyConstant(key, value);
                constantList.Add(constant);
            }
        }

        return constantList;
    }

    private static void GenerateOutput(SourceProductionContext context, (List<AssemblyConstant> Constants, (string AssemblyName, GlobalOptions GlobalOptions) Options) parameters)
    {
        var (assemblyConstants, (assemblyName, (defineConstants, rootNamespace))) = parameters;
        var pascalAssemblyName = SourceGeneratorHelper.ToPascalCaseName(assemblyName);
        defineConstants ??= string.Empty;
        rootNamespace ??= string.Empty;

        var constants = new List<AssemblyConstant>(assemblyConstants);

        if (constants.Any(p => p.Name == SourceGeneratorHelper.ToPascalCaseName(nameof(assemblyName))) == false) constants.Add(new AssemblyConstant(SourceGeneratorHelper.ToPascalCaseName(nameof(assemblyName)), $"\"{assemblyName}\""));
        if (constants.Any(p => p.Name == nameof(GlobalOptions.DefineConstants)) == false) constants.Add(new AssemblyConstant(nameof(GlobalOptions.DefineConstants), $"\"{defineConstants}\""));
        if (constants.Any(p => p.Name == nameof(GlobalOptions.RootNamespace)) == false) constants.Add(new AssemblyConstant(nameof(GlobalOptions.RootNamespace), $"\"{rootNamespace}\""));

        var properties = string.Join(
            "\r\n\r\n",
            from constant in constants
            orderby constant.Name
            select $$"""
                         /// <summary>
                         /// Assembly Metadata: {{constant.Name}}
                         /// </summary>
                         public static string {{constant.Name}} { get; } = {{constant.Value}};
                     """
        );

        var result = $$"""
                       // <auto-generated/>

                       #nullable enable
                       namespace SourceGenerator.AssemblyMetadata;

                       /// <summary>
                       /// Assembly attributes exposed as public property
                       /// </summary>
                       [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                       [global::System.CodeDom.Compiler.GeneratedCodeAttribute("{{nameof(AssemblyMetadataGenerator)}}", "{{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty}}")]
                       public static partial class {{pascalAssemblyName}}{{nameof(AssemblyMetadata)}}
                       {
                       {{properties}}
                       }
                       """;

        context.AddSource($"{pascalAssemblyName}{nameof(AssemblyMetadata)}.g.cs", SourceText.From(result, Encoding.UTF8));
    }

    private sealed record AssemblyConstant(
        string Name,
        string Value
    );

    private sealed record GlobalOptions(
        string? DefineConstants,
        string? RootNamespace
    );
}