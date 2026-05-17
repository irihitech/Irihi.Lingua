using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Irihi.Lolita.Generator;

/// <summary>
/// Incremental source generator for <c>[LolitaManager]</c>-annotated classes.
/// For each such static partial class the generator produces:
/// <list type="bullet">
///   <item>A nested <c>Keys</c> static class with a <c>const string</c> for every resource key.</item>
///   <item>A per-key <see cref="Irihi.Lolita.LolitaObservableString"/> backing field.</item>
///   <item>A public <c>IObservable&lt;string&gt;</c> property for every resource key.</item>
///   <item>An <c>_lolita_observables</c> array containing all observable instances for iteration.</item>
///   <item>An <c>UpdateCulture(CultureInfo)</c> method that pushes new values to all observables.</item>
///   <item>An internal resource dictionary covering every discovered culture variant.</item>
/// </list>
/// </summary>
[Generator]
public sealed class LolitaManagerGenerator : IIncrementalGenerator
{
    private const string AttributeFullName = "Irihi.Lolita.LolitaManagerAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var managedClasses = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeFullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => ExtractManagerInfo(ctx))
            .Where(static info => info != null);

        var allResxFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
            .Collect();

        context.RegisterSourceOutput(
            managedClasses.Combine(allResxFiles),
            static (spc, pair) => Generate(spc, pair.Left!, pair.Right));
    }

    // -------------------------------------------------------------------------
    // Extraction
    // -------------------------------------------------------------------------

    private static ManagerInfo? ExtractManagerInfo(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        var attribute = ctx.Attributes.FirstOrDefault();
        if (attribute is null || attribute.ConstructorArguments.Length == 0)
        {
            return null;
        }

        var resourcePath = attribute.ConstructorArguments[0].Value as string;
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : classSymbol.ContainingNamespace.ToDisplayString();

        return new ManagerInfo(classSymbol.Name, ns, resourcePath!);
    }

    // -------------------------------------------------------------------------
    // Generation
    // -------------------------------------------------------------------------

    private static void Generate(
        SourceProductionContext spc,
        ManagerInfo info,
        ImmutableArray<AdditionalText> resxFiles)
    {
        var baseName = Path.GetFileNameWithoutExtension(info.ResourcePath);

        var cultureXml = CollectCultureResources(baseName, resxFiles, spc.CancellationToken);
        if (cultureXml.Count == 0)
        {
            return;
        }

        // Determine the key list from the default (or first available) file
        var baseXml = cultureXml.TryGetValue(string.Empty, out var bx) ? bx : cultureXml.Values.First();
        var keys = ParseKeys(baseXml);
        if (keys.Length == 0)
        {
            return;
        }

        // Parse values per culture
        var cultureData = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in cultureXml)
        {
            cultureData[kvp.Key] = ParseValues(kvp.Value);
        }

        var source = BuildSource(info, keys, cultureData);
        spc.AddSource($"{info.ClassName}.LolitaManager.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    /// <summary>
    /// Scans <paramref name="resxFiles"/> for entries whose file name matches
    /// <paramref name="baseName"/> (default culture, key <c>""</c>) or
    /// <c>BaseName.Culture.resx</c> (culture variants, key = culture tag).
    /// </summary>
    private static Dictionary<string, string> CollectCultureResources(
        string baseName,
        ImmutableArray<AdditionalText> resxFiles,
        System.Threading.CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in resxFiles)
        {
            var fileName = Path.GetFileName(file.Path);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

            string culture;
            if (string.Equals(nameWithoutExt, baseName, StringComparison.OrdinalIgnoreCase))
            {
                // Exact match → default / fallback culture
                culture = string.Empty;
            }
            else if (nameWithoutExt.StartsWith(baseName + ".", StringComparison.OrdinalIgnoreCase))
            {
                // Culture variant: BaseName.Culture.resx
                culture = nameWithoutExt.Substring(baseName.Length + 1);
                if (string.IsNullOrEmpty(culture))
                {
                    continue;
                }
            }
            else
            {
                continue;
            }

            var text = file.GetText(cancellationToken)?.ToString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                result[culture] = text!;
            }
        }

        return result;
    }

    // -------------------------------------------------------------------------
    // Resx parsing
    // -------------------------------------------------------------------------

    private static string[] ParseKeys(string xml)
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

    private static Dictionary<string, string> ParseValues(string xml)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            var document = XDocument.Parse(xml);
            foreach (var data in document.Root?.Elements("data") ?? Enumerable.Empty<XElement>())
            {
                var name = data.Attribute("name")?.Value;
                var value = data.Element("value")?.Value;
                if (!string.IsNullOrWhiteSpace(name) && value != null)
                {
                    result[name!] = value;
                }
            }
        }
        catch (XmlException)
        {
        }

        return result;
    }

    // -------------------------------------------------------------------------
    // Code generation
    // -------------------------------------------------------------------------

    private static string BuildSource(
        ManagerInfo info,
        string[] keys,
        Dictionary<string, Dictionary<string, string>> cultureData)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        if (info.Namespace != null)
        {
            sb.AppendLine($"namespace {info.Namespace};");
            sb.AppendLine();
        }

        sb.AppendLine($"public static partial class {info.ClassName}");
        sb.AppendLine("{");

        // ── Resource storage dictionary ──────────────────────────────────────
        sb.AppendLine("    private static readonly global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.Dictionary<string, string>> _lolita_resources =");
        sb.AppendLine("        new global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.Dictionary<string, string>>()");
        sb.AppendLine("        {");

        foreach (var kvp in cultureData.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            sb.AppendLine($"            [\"{EscapeString(kvp.Key)}\"] = new global::System.Collections.Generic.Dictionary<string, string>()");
            sb.AppendLine("            {");

            foreach (var key in keys)
            {
                if (kvp.Value.TryGetValue(key, out var value))
                {
                    sb.AppendLine($"                [\"{EscapeString(key)}\"] = @\"{EscapeVerbatimString(value)}\",");
                }
            }

            sb.AppendLine("            },");
        }

        sb.AppendLine("        };");
        sb.AppendLine();

        // ── Keys nested class ────────────────────────────────────────────────
        sb.AppendLine("    /// <summary>Provides strongly-typed constants for each resource key.</summary>");
        sb.AppendLine("    public static class Keys");
        sb.AppendLine("    {");

        var usedIdentifiers = new HashSet<string>(StringComparer.Ordinal);

        foreach (var key in keys)
        {
            var identifier = SanitizeIdentifier(key);
            if (!usedIdentifiers.Add(identifier))
            {
                continue;
            }

            sb.AppendLine($"        /// <summary>Resource key constant for <c>{EscapeXmlComment(key)}</c>.</summary>");
            sb.AppendLine($"        public const string {identifier} = \"{EscapeString(key)}\";");
        }

        sb.AppendLine("    }");
        sb.AppendLine();

        // ── Observable backing fields and public properties ──────────────────
        var defaultValues = cultureData.TryGetValue(string.Empty, out var dv)
            ? dv
            : new Dictionary<string, string>();

        usedIdentifiers.Clear();

        foreach (var key in keys)
        {
            var identifier = SanitizeIdentifier(key);
            if (!usedIdentifiers.Add(identifier))
            {
                continue;
            }

            defaultValues.TryGetValue(key, out var defaultValue);
            defaultValue ??= string.Empty;

            sb.AppendLine($"    private static readonly global::Irihi.Lolita.LolitaObservableString _lolita_{identifier} =");
            sb.AppendLine($"        new global::Irihi.Lolita.LolitaObservableString(Keys.{identifier}, @\"{EscapeVerbatimString(defaultValue)}\");");
            sb.AppendLine();
            sb.AppendLine($"    /// <summary>Gets an observable that emits the current value of the <c>{EscapeXmlComment(key)}</c> resource key.</summary>");
            sb.AppendLine($"    public static global::System.IObservable<string> {identifier} => _lolita_{identifier};");
            sb.AppendLine();
        }

        // ── Observable collection ────────────────────────────────────────────
        sb.AppendLine("    private static readonly global::Irihi.Lolita.LolitaObservableString[] _lolita_observables =");
        sb.AppendLine("        new global::Irihi.Lolita.LolitaObservableString[]");
        sb.AppendLine("        {");

        usedIdentifiers.Clear();
        foreach (var key in keys)
        {
            var identifier = SanitizeIdentifier(key);
            if (!usedIdentifiers.Add(identifier))
            {
                continue;
            }

            sb.AppendLine($"            _lolita_{identifier},");
        }

        sb.AppendLine("        };");
        sb.AppendLine();

        // ── UpdateCulture method ─────────────────────────────────────────────
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Switches all observable properties to the values for <paramref name=\"culture\"/>.");
        sb.AppendLine("    /// Falls back to the parent culture, then to the default (invariant) culture.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static void UpdateCulture(global::System.Globalization.CultureInfo culture)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (culture is null)");
        sb.AppendLine("        {");
        sb.AppendLine("            culture = global::System.Globalization.CultureInfo.InvariantCulture;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        global::System.Collections.Generic.Dictionary<string, string>? dict;");
        sb.AppendLine("        if (!_lolita_resources.TryGetValue(culture.Name, out dict))");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!_lolita_resources.TryGetValue(culture.TwoLetterISOLanguageName, out dict))");
        sb.AppendLine("            {");
        sb.AppendLine("                _lolita_resources.TryGetValue(\"\", out dict);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (dict is null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        foreach (var _lolita_obs in _lolita_observables)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (dict.TryGetValue(_lolita_obs.Key, out var _lolita_v))");
        sb.AppendLine("                _lolita_obs.OnNext(_lolita_v);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string SanitizeIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Value";
        }

        var builder = new StringBuilder(value.Length);

        foreach (var ch in value)
        {
            builder.Append(char.IsLetterOrDigit(ch) ? ch : '_');
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

    private static string EscapeString(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string EscapeVerbatimString(string value) =>
        value.Replace("\"", "\"\"");

    private static string EscapeXmlComment(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    // -------------------------------------------------------------------------
    // Data model
    // -------------------------------------------------------------------------

    private sealed class ManagerInfo
    {
        public string ClassName { get; }
        public string? Namespace { get; }
        public string ResourcePath { get; }

        public ManagerInfo(string className, string? ns, string resourcePath)
        {
            ClassName = className;
            Namespace = ns;
            ResourcePath = resourcePath;
        }
    }
}
