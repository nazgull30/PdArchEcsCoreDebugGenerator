namespace EcsCodeGen.Debugger;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator]
public class ComponentJsonConvertersGenerator : IIncrementalGenerator
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
        foreach (var (ctx, semanticModel) in structs)
        {
            var (componentName, code) = CreateJsonConverterTemplate.Generate(ctx, semanticModel);

            var formattedCode = code.FormatCode();

            context.AddSource($"EcsCodeGen.Debugger.Components/{componentName}JsonConverter.g.cs", formattedCode);
        }
    }
}
