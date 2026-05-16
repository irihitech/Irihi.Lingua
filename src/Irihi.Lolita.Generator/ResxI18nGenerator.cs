using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Irihi.Lolita.Generator;

[Generator]
public sealed class ResxI18nGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var resxFiles = context.AdditionalTextsProvider.Where(static file => file.Path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase));

        context.RegisterSourceOutput(resxFiles, static (productionContext, additionalText) =>
        {
            var sourceText = additionalText.GetText(productionContext.CancellationToken);
            if (sourceText is null)
            {
                return;
            }

            var text = sourceText.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var resourceName = Path.GetFileNameWithoutExtension(additionalText.Path);
            var source = BuildSource(resourceName, text);
            productionContext.AddSource($"{resourceName}.I18n.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    private static string BuildSource(string resourceName, string xml)
    {
        var keys = ParseKeys(xml);
        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        var builder = new StringBuilder();

        builder.AppendLine("namespace Irihi.Lolita.Generated;");
        builder.AppendLine();
        builder.AppendLine($"public static partial class {SanitizeIdentifier(resourceName)}I18n");
        builder.AppendLine("{");

        foreach (var key in keys)
        {
            var identifier = SanitizeIdentifier(key);
            if (!identifiers.Add(identifier))
            {
                continue;
            }

            builder.AppendLine($"    public const string {identifier} = \"{EscapeString(key)}\";");
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static IEnumerable<string> ParseKeys(string xml)
    {
        try
        {
            var document = XDocument.Parse(xml);
            return document
                .Root?
                .Elements("data")
                .Select(x => x.Attribute("name")?.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .Cast<string>()
                .ToArray() ?? Array.Empty<string>();
        }
        catch (XmlException)
        {
            return Array.Empty<string>();
        }
    }

    private static string SanitizeIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Value";
        }

        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        if (builder.Length == 0)
        {
            return "Value";
        }

        if (!char.IsLetter(builder[0]) && builder[0] != '_')
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }

    private static string EscapeString(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
