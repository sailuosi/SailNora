using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SailNora.Generators;

/// <summary>
/// 扫描 [Scoped]/[Singleton]/[Transient]（含基类继承），生成本程序集 Add*Services 扩展。
/// </summary>
[Generator]
public sealed class DiRegistrationGenerator : IIncrementalGenerator
{
    internal const string ScopedAttributeName = "SailNora.Foundation.DI.ScopedAttribute";
    internal const string SingletonAttributeName = "SailNora.Foundation.DI.SingletonAttribute";
    internal const string TransientAttributeName = "SailNora.Foundation.DI.TransientAttribute";

    static readonly DiagnosticDescriptor MultipleLifetimes = new(
        id: "SAILNORADI001",
        title: "Multiple DI lifetime attributes",
        messageFormat: "Type '{0}' has more than one of [Scoped]/[Singleton]/[Transient]",
        category: "SailNora.DI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classInfos = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => TryGetServiceInfo(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!);

        context.RegisterSourceOutput(
            classInfos.Collect().Combine(context.CompilationProvider),
            static (spc, pair) => Execute(pair.Left, pair.Right, spc));
    }

    static ServiceInfo? TryGetServiceInfo(GeneratorSyntaxContext context, System.Threading.CancellationToken ct)
    {
        if (context.Node is not ClassDeclarationSyntax cds)
            return null;

        if (context.SemanticModel.GetDeclaredSymbol(cds, ct) is not INamedTypeSymbol type)
            return null;

        if (type.TypeKind != TypeKind.Class || type.IsAbstract || type.IsStatic)
            return null;

        // 仅注册本程序集中声明的类型（避免跨引用程序集重复）
        if (!SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, context.SemanticModel.Compilation.Assembly))
            return null;

        var lifetimes = CollectLifetimes(type);
        if (lifetimes.Count == 0)
            return null;

        var location = cds.Identifier.GetLocation();
        if (lifetimes.Count > 1)
        {
            return new ServiceInfo(
                type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                type.Name,
                LifetimeKind.Scoped,
                ImmutableArray<string>.Empty,
                location,
                hasConflict: true);
        }

        var lifetime = lifetimes[0];
        var interfaces = CollectInfrastructureInterfaces(type);
        return new ServiceInfo(
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            type.Name,
            lifetime,
            interfaces,
            location,
            hasConflict: false);
    }

    static List<LifetimeKind> CollectLifetimes(INamedTypeSymbol type)
    {
        var found = new List<LifetimeKind>();
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            foreach (var attr in current.GetAttributes())
            {
                var name = attr.AttributeClass?.ToDisplayString();
                if (name == ScopedAttributeName)
                    AddUnique(found, LifetimeKind.Scoped);
                else if (name == SingletonAttributeName)
                    AddUnique(found, LifetimeKind.Singleton);
                else if (name == TransientAttributeName)
                    AddUnique(found, LifetimeKind.Transient);
            }
        }

        return found;
    }

    static void AddUnique(List<LifetimeKind> list, LifetimeKind kind)
    {
        if (!list.Contains(kind))
            list.Add(kind);
    }

    static ImmutableArray<string> CollectInfrastructureInterfaces(INamedTypeSymbol type)
    {
        var builder = ImmutableArray.CreateBuilder<string>();
        foreach (var iface in type.AllInterfaces)
        {
            if (!IsInfrastructureInterface(iface))
                continue;
            builder.Add(iface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// 与旧 ServiceRegistrationExtensions.IsInfrastructureInterface 对齐：
    /// 注册 IXxx 基础设施契约；排除 IXxxService 业务接口对；排除 System.* BCL 接口（如 IDisposable）。
    /// </summary>
    static bool IsInfrastructureInterface(INamedTypeSymbol interfaceType)
    {
        if (interfaceType.TypeKind != TypeKind.Interface)
            return false;

        var name = interfaceType.Name;
        if (!name.StartsWith("I", StringComparison.Ordinal) || name.Length < 2)
            return false;

        if (name.EndsWith("Service", StringComparison.Ordinal))
            return false;

        var ns = interfaceType.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        if (ns.Length == 0 || ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal))
            return false;

        return true;
    }

    static void Execute(
        ImmutableArray<ServiceInfo> services,
        Compilation compilation,
        SourceProductionContext context)
    {
        foreach (var svc in services.Where(s => s.HasConflict))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                MultipleLifetimes,
                svc.Location,
                svc.TypeName));
        }

        var valid = services
            .Where(s => !s.HasConflict)
            .GroupBy(s => s.FullyQualifiedType, StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderBy(s => s.FullyQualifiedType, StringComparer.Ordinal)
            .ToList();

        var assemblyName = compilation.AssemblyName ?? "Assembly";
        var methodName = "Add" + assemblyName.Replace(".", string.Empty) + "Services";
        var ns = assemblyName;

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.Append("namespace ").Append(ns).AppendLine(";");
        sb.AppendLine();
        sb.AppendLine("/// <summary>源生成：本程序集 [Scoped]/[Singleton]/[Transient] 注册。</summary>");
        sb.AppendLine("public static class GeneratedDiRegistration");
        sb.AppendLine("{");
        sb.Append("    public static IServiceCollection ").Append(methodName).AppendLine("(this IServiceCollection services)");
        sb.AppendLine("    {");

        if (valid.Count == 0)
        {
            sb.AppendLine("        return services;");
        }
        else
        {
            foreach (var svc in valid)
            {
                var addMethod = svc.Lifetime switch
                {
                    LifetimeKind.Singleton => "AddSingleton",
                    LifetimeKind.Transient => "AddTransient",
                    _ => "AddScoped"
                };

                sb.Append("        services.").Append(addMethod).Append('<')
                    .Append(svc.FullyQualifiedType).AppendLine(">();");

                foreach (var iface in svc.Interfaces.OrderBy(i => i, StringComparer.Ordinal))
                {
                    sb.Append("        services.").Append(addMethod).Append('<')
                        .Append(iface).Append(", ").Append(svc.FullyQualifiedType).AppendLine(">();");
                }
            }

            sb.AppendLine("        return services;");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("DiRegistration.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    enum LifetimeKind
    {
        Scoped,
        Singleton,
        Transient
    }

    sealed class ServiceInfo
    {
        public ServiceInfo(
            string fullyQualifiedType,
            string typeName,
            LifetimeKind lifetime,
            ImmutableArray<string> interfaces,
            Location? location,
            bool hasConflict)
        {
            FullyQualifiedType = fullyQualifiedType;
            TypeName = typeName;
            Lifetime = lifetime;
            Interfaces = interfaces;
            Location = location;
            HasConflict = hasConflict;
        }

        public string FullyQualifiedType { get; }
        public string TypeName { get; }
        public LifetimeKind Lifetime { get; }
        public ImmutableArray<string> Interfaces { get; }
        public Location? Location { get; }
        public bool HasConflict { get; }
    }
}
