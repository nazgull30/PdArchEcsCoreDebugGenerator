namespace EcsCodeGen.Debugger;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class CreateJsonConverterTemplate
{
    public static (string, string) Generate(StructDeclarationSyntax stx, SemanticModel semanticModel)
    {
        var structSymbol = semanticModel.GetDeclaredSymbol(stx) ?? throw new ArgumentException("structSymbol is null");

        var componentName = $"{structSymbol.Name}".Replace("Component", "");

        var componentNamespace = structSymbol.ContainingNamespace.ToDisplayString();
        var namespaces = new HashSet<string> { componentNamespace };

        var namespacesBuilder = new StringBuilder();
        foreach (var ns in namespaces)
        {
            namespacesBuilder.Append($"using {ns};\n");
        }

        var properties = GetProperties(stx, semanticModel);
        var statement = properties.Count > 0 ? $"var val = \"{componentName}: \" + value.ToString();" : $"var val = \"{componentName}\";";


        var code = $$"""
namespace ArchEntityDebugger;

using CompactJson;
using EcsCodeGen;

{{namespacesBuilder}}

public class {{componentName}}Converter : ConverterBase
{
    public {{componentName}}Converter() : base(typeof({{componentName}}))
    {
    }

    public override void Write(object value, IJsonConsumer writer)
    {
        {{statement}}
        writer.String(val);
    }


}
""";
        return (componentName, code);
    }

    private static List<PropertyInfo> GetProperties(StructDeclarationSyntax stx, SemanticModel semanticModel)
    {
        var properties = new List<PropertyInfo>();
        foreach (var field in stx.Members.OfType<FieldDeclarationSyntax>())
        {
            foreach (var variable in field.Declaration.Variables)
            {
                var fieldTypeSyntax = field.Declaration.Type;
                var fieldTypeSymbol = semanticModel.GetTypeInfo(fieldTypeSyntax).Type;

                var fieldName = variable.Identifier.Text;
                var fieldType = fieldTypeSymbol.ToDisplayString();

                properties.Add(new PropertyInfo(fieldName, fieldType, fieldTypeSymbol.ContainingNamespace.ToDisplayString()));

            }
        }
        return properties;
    }

    private readonly struct PropertyInfo(string fieldName, string fieldType, string ns)
    {
        public readonly string FieldName = fieldName;
        public readonly string FieldType = fieldType;
        public readonly string Namespace = ns;
    }
}
