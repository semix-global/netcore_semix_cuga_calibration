using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SourceGenerator.AssemblyMetadata;
using System.Collections.Immutable;
using System.Text;

namespace SourceGenerator.InjectHostDI;

/// <summary>
/// Inject Host DI生成器
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class InjectHostDIGenerator : IIncrementalGenerator
{
    #region Attribute Const Value

    private const string AttributeFullClassName = "Net.Utilities.Attributes.IOCAppServiceAttribute";
    private const string AttributePropertyNameIocLifetimeEnum = "IOCLifetimeEnum";
    private const string AttributePropertyNameIocEnvironmentEnum = "IOCEnvironmentEnum";
    private const string AttributePropertyNameServiceType = "ServiceType";
    private const string AttributePropertyNameIsGetInterfaceServiceType = "IsGetInterfaceServiceType";

    private const string IocLifetimeEnumSingletonValue = "global::Net.Utilities.Enums.IOCLifeTimeEnum.Singleton";
    private const string IocLifetimeEnumScopedValue = "global::Net.Utilities.Enums.IOCLifeTimeEnum.Scoped";
    private const string IocLifetimeEnumTransientValue = "global::Net.Utilities.Enums.IOCLifeTimeEnum.Transient";

    private const string IocEnvironmentEnumDevelopmentValue = "global::Net.Utilities.Enums.IOCEnvironmentEnum.Development";
    private const string IocEnvironmentEnumStagingValue = "global::Net.Utilities.Enums.IOCEnvironmentEnum.Staging";
    private const string IocEnvironmentEnumProductionValue = "global::Net.Utilities.Enums.IOCEnvironmentEnum.Production";

    private const string IsGetInterfaceServiceTypeTrueValue = "true";
    private const string IsGetInterfaceServiceTypeFalseValue = "false";

    private const string IocLifetimeEnumDefaultValue = IocLifetimeEnumSingletonValue;
    private const string IocEnvironmentEnumDefaultValue = $"{IocEnvironmentEnumDevelopmentValue} | {IocEnvironmentEnumStagingValue} | {IocEnvironmentEnumProductionValue}";
    private const string IsGetInterfaceServiceTypeDefaultValue = IsGetInterfaceServiceTypeFalseValue;

    #endregion Attribute Const Value

    private const string ServiceCollectionServiceExtensionsTransientMethodName = "global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient";
    private const string ServiceCollectionServiceExtensionsScopedMethodName = "global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddScoped";
    private const string ServiceCollectionServiceExtensionsSingletonMethodName = "global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var serviceRegistrations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: AttributeFullClassName,
                predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax
                {
                    AttributeLists.Count: 1
                } classDeclaration
                                                     && classDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword) == false
                                                     && classDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword) == false,
                transform: SemanticTransform
            )
            .Where(static context => context is not null)
            .Collect();
        var assemblyName = context.CompilationProvider
            .Select(static (c, _) => c.Assembly.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        context.RegisterSourceOutput(serviceRegistrations.Combine(assemblyName), GenerateOutput);
    }

    private static ServiceRegistration? SemanticTransform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not INamedTypeSymbol namedTypeSymbol) return null;

        var attributes = namedTypeSymbol.GetAttributes();
        if (attributes.Length != 1) return null;
        var attribute = attributes.First();

        var type = namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var interfaceType = namedTypeSymbol.AllInterfaces.FirstOrDefault()?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var iocLifetimeEnum = attribute.NamedArguments
                                  .Where(t => t.Key == AttributePropertyNameIocLifetimeEnum)
                                  .Cast<KeyValuePair<string, TypedConstant>?>()
                                  .SingleOrDefault()?.Value.ToCSharpString()
                              ?? IocLifetimeEnumDefaultValue;
        iocLifetimeEnum = iocLifetimeEnum.StartsWith("global::") == false ? $"global::{iocLifetimeEnum}" : iocLifetimeEnum;
        var iocEnvironmentEnum = attribute.NamedArguments
                                     .Where(t => t.Key == AttributePropertyNameIocEnvironmentEnum)
                                     .Cast<KeyValuePair<string, TypedConstant>?>().SingleOrDefault()?.Value.ToCSharpString()
                                 ?? IocEnvironmentEnumDefaultValue;
        var iocEnvironmentEnums = iocEnvironmentEnum.Split(" | ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(env => env.StartsWith("global::") == false ? $"global::{env}" : env).ToArray();
        var isGetInterfaceServiceType = attribute.NamedArguments
                                            .Where(t => t.Key == AttributePropertyNameIsGetInterfaceServiceType)
                                            .Cast<KeyValuePair<string, TypedConstant>?>()
                                            .SingleOrDefault()?.Value.ToCSharpString()
                                        ?? IsGetInterfaceServiceTypeDefaultValue;
        var serviceType = attribute.NamedArguments
                              .Where(t => t.Key == AttributePropertyNameServiceType)
                              .Cast<KeyValuePair<string, TypedConstant>?>()
                              .SingleOrDefault()?.Value.Value?.ToString()
                          ?? (isGetInterfaceServiceType == IsGetInterfaceServiceTypeTrueValue
                              ? interfaceType ?? type
                              : type);
        serviceType = serviceType.StartsWith("global::") == false ? $"global::{serviceType}" : serviceType;

        return new ServiceRegistration(type, serviceType, IocLifetimeEnumToServiceCollectionServiceExtensionsMethodName(iocLifetimeEnum), iocEnvironmentEnums);
    }

    private static void GenerateOutput(SourceProductionContext context, (ImmutableArray<ServiceRegistration?> ServiceRegistrations, string AssemblyName) parameters)
    {
        var (serviceRegistrations, assemblyName) = parameters;
        var pascalAssemblyName = SourceGeneratorHelper.ToPascalCaseName(assemblyName);

        var developments = GetInjectSource(IocEnvironmentEnumDevelopmentValue);
        var stagings = GetInjectSource(IocEnvironmentEnumStagingValue);
        var productions = GetInjectSource(IocEnvironmentEnumProductionValue);
        var result = $$"""
                       // <auto-generated/>

                       #nullable enable
                       namespace SourceGenerator.InjectHostDI;

                       /// <summary>
                       /// Extension methods for discovered service registrations
                       /// </summary>
                       [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                       [global::System.CodeDom.Compiler.GeneratedCodeAttribute("{{nameof(InjectHostDIGenerator)}}", "{{SourceGeneratorInjectHostDIAssemblyMetadata.Version}}")]
                       public static class {{pascalAssemblyName}}{{nameof(InjectHostDI)}}Extensions
                       {
                           /// <summary>
                           /// Adds discovered services from {{assemblyName}} to the specified service collection
                           /// </summary>
                           /// <param name="serviceCollection">The service collection.</param>
                           /// <param name="hostEnvironment">host runtime environment.</param>
                           /// <returns>The service collection</returns>
                           [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                           [global::System.CodeDom.Compiler.GeneratedCodeAttribute("{{nameof(InjectHostDIGenerator)}}", "{{SourceGeneratorInjectHostDIAssemblyMetadata.Version}}")]
                           public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection Add{{pascalAssemblyName}}{{nameof(InjectHostDI)}}(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection serviceCollection, global::Microsoft.Extensions.Hosting.IHostEnvironment hostEnvironment)
                           {
                               switch (hostEnvironment.EnvironmentName)
                               {
                                   case { } s when s == global::Microsoft.Extensions.Hosting.Environments.Development:
                       {{developments}}
                                       break;
                       
                                   case { } s when s == global::Microsoft.Extensions.Hosting.Environments.Staging:
                       {{stagings}}
                                       break;
                       
                                   case { } s when s == global::Microsoft.Extensions.Hosting.Environments.Production:
                       {{productions}}
                                       break;
                               }
                       
                               return serviceCollection;
                           }
                       }
                       """;

        context.AddSource($"{pascalAssemblyName}{nameof(InjectHostDI)}.g.cs", SourceText.From(result, Encoding.UTF8));

        return;

        string GetInjectSource(string iocEnvironmentEnums) => string.Join(
            "\r\n",
            from pipeline in serviceRegistrations
            where pipeline.IocEnvironmentEnums.Contains(iocEnvironmentEnums)
            select $"                {pipeline.ServiceCollectionServiceExtensionsMethodName}<{pipeline.ServiceType}, {pipeline.Type}>(serviceCollection);"
        );
    }

    private static string IocLifetimeEnumToServiceCollectionServiceExtensionsMethodName(string iocLifetimeEnum) => iocLifetimeEnum switch
    {
        IocLifetimeEnumTransientValue => ServiceCollectionServiceExtensionsTransientMethodName,
        IocLifetimeEnumScopedValue => ServiceCollectionServiceExtensionsScopedMethodName,
        IocLifetimeEnumSingletonValue => ServiceCollectionServiceExtensionsSingletonMethodName,
        _ => ServiceCollectionServiceExtensionsTransientMethodName
    };

    private sealed record ServiceRegistration(
        string Type,
        string ServiceType,
        string ServiceCollectionServiceExtensionsMethodName,
        string[] IocEnvironmentEnums
    );
}