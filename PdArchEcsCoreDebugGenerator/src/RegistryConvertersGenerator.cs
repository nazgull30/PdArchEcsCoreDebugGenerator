namespace EcsCodeGen.Debugger;

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator]
public class RegistryConvertersGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var structDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => node is StructDeclarationSyntax classDecl &&
                                        classDecl.AttributeLists.Count > 0,
                transform: (context, _) => (context.Node as StructDeclarationSyntax, context.SemanticModel))
            .Where(pair => Utilities.HasAttribute("ComponentAttribute", pair.Item1, pair.Item2) && !Utilities.HasAttribute("CustomConverterAttribute", pair.Item1, pair.Item2))
            .Collect();

        context.RegisterSourceOutput(structDeclarations, GenerateCode);
    }

    private void GenerateCode(SourceProductionContext context,
        ImmutableArray<(StructDeclarationSyntax, SemanticModel)> structs)
    {
        var statements = new StringBuilder();
        foreach (var (ctx, semanticModel) in structs)
        {
            var structSymbol = semanticModel.GetDeclaredSymbol(ctx) ?? throw new ArgumentException("structSymbol is null");

            var componentName = $"{structSymbol.Name}".Replace("Component", "");
            statements.AppendLine($"ConverterRegistry.AddConverter(new {componentName}Converter());");
        }


        var code = $$"""

namespace ArchEntityDebugger;
using CompactJson;

                     	public static class RegistryConverters
                     	{
                     		public static void Register()
                     		{
                                {{statements}}
                     		}
                     	}
""";

        var formattedCode = code.FormatCode();
        context.AddSource($"EcsCodeGen.Debugger/RegistryConverters.g.cs", formattedCode);

    }
}
