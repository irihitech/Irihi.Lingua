using System.Collections.Immutable;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Irihi.Lingua.Generator;

/// <summary>
/// Incremental source generator for <c>[LinguaManager]</c>-annotated classes.
/// For each such partial class the generator produces:
/// <list type="bullet">
///   <item>A private constructor and a <c>public static readonly ILinguaManager Instance</c> singleton.</item>
///   <item>A nested <c>Keys</c> static class with a <c>public static readonly LinguaKey</c> for every resource key.</item>
///   <item>A per-key <c>LinguaObservableString</c> instance backing field.</item>
///   <item>A public instance <c>IObservable&lt;string?&gt;</c> property for every resource key.</item>
///   <item>A <c>_lingua_observables</c> dictionary (key → observable) initialized in the constructor.</item>
///   <item>An instance <c>UpdateCulture(CultureInfo)</c> method implementing <c>ILinguaManager</c>.</item>
///   <item>An internal resource dictionary covering every discovered culture variant.</item>
/// </list>
/// </summary>
[Generator]
public sealed class LinguaManagerGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor ResourcePathNotFoundDescriptor = new(
        id: "LINGUA001",
        title: "Resource path not found",
        messageFormat: "The resource path '{0}' specified in [LinguaManager] does not match any .resx file in the project's AdditionalFiles",
        category: "Irihi.Lingua",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private const string AttributeFullName = "Irihi.Lingua.LinguaManagerAttribute";

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

        var accessibility = classSymbol.DeclaredAccessibility switch
        {
            Accessibility.Internal => "internal",
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            _ => "public",
        };

        // Use the class declaration location so the warning points at the annotated class.
        var location = ctx.TargetNode.GetLocation();

        return new ManagerInfo(classSymbol.Name, ns, resourcePath!, accessibility, location);
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
            spc.ReportDiagnostic(Diagnostic.Create(
                ResourcePathNotFoundDescriptor, info.AttributeLocation, info.ResourcePath));
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
        spc.AddSource($"{info.ClassName}.LinguaManager.g.cs", SourceText.From(source, Encoding.UTF8));
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

        sb.AppendLine($"{info.Accessibility} partial class {info.ClassName} : global::Irihi.Lingua.ILinguaManager");
        sb.AppendLine("{");

        var defaultValues = cultureData.TryGetValue(string.Empty, out var dv)
            ? dv
            : new Dictionary<string, string>();

        BuildConstructor(sb, info, keys);
        BuildInstanceField(sb, info);
        BuildStaticResources(sb, keys, cultureData);
        BuildKeysClass(sb, keys);
        BuildObservableMembers(sb, keys, defaultValues);
        BuildObservablesField(sb);
        BuildRuntimeResourcesField(sb);
        BuildUpdateCultureMethod(sb);
        BuildGetObservableMethod(sb);
        BuildAddResourcesMethod(sb);

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>Builds the private constructor that initializes <c>_lingua_observables</c>.</summary>
    private static void BuildConstructor(StringBuilder sb, ManagerInfo info, string[] keys)
    {
        sb.AppendLine($"    private {info.ClassName}()");
        sb.AppendLine("    {");

        // Observable collection initialised inside the constructor so that
        // instance fields (_lingua_<id>) are available.
        sb.AppendLine("        _lingua_observables = new global::System.Collections.Generic.Dictionary<string, global::Irihi.Lingua.LinguaObservableString>()");
        sb.AppendLine("        {");

        var usedIdentifiers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in keys)
        {
            var identifier = SanitizeIdentifier(key);
            if (!usedIdentifiers.Add(identifier))
            {
                continue;
            }

            sb.AppendLine($"            [\"{EscapeString(key)}\"] = _lingua_{identifier},");
        }

        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    /// <summary>Builds the <c>public static readonly Instance</c> singleton field.</summary>
    private static void BuildInstanceField(StringBuilder sb, ManagerInfo info)
    {
        sb.AppendLine("    /// <summary>Gets the singleton instance of this manager.</summary>");
        sb.AppendLine($"    public static readonly {info.ClassName} Instance = new {info.ClassName}();");
        sb.AppendLine();
    }

    /// <summary>
    /// Builds the static <c>_lingua_resources</c> field and its factory method
    /// <c>CreateStaticResources()</c>, which populates all compile-time culture dictionaries.
    /// </summary>
    private static void BuildStaticResources(
        StringBuilder sb,
        string[] keys,
        Dictionary<string, Dictionary<string, string>> cultureData)
    {
        sb.AppendLine("    private static readonly global::Irihi.Lingua.LinguaRuntimeResources _lingua_resources = CreateStaticResources();");
        sb.AppendLine();
        sb.AppendLine("    private static global::Irihi.Lingua.LinguaRuntimeResources CreateStaticResources()");
        sb.AppendLine("    {");
        sb.AppendLine("        var _lingua_r = new global::Irihi.Lingua.LinguaRuntimeResources();");

        foreach (var kvp in cultureData.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            var cultureKey = string.IsNullOrEmpty(kvp.Key)
                ? "global::System.Globalization.CultureInfo.InvariantCulture"
                : $"new global::System.Globalization.CultureInfo(\"{EscapeString(kvp.Key)}\")";

            sb.AppendLine($"        _lingua_r.Add({cultureKey}, new global::System.Collections.Generic.Dictionary<string, string>()");
            sb.AppendLine("        {");

            foreach (var key in keys)
            {
                if (kvp.Value.TryGetValue(key, out var value))
                {
                    sb.AppendLine($"            [\"{EscapeString(key)}\"] = @\"{EscapeVerbatimString(value)}\",");
                }
            }

            sb.AppendLine("        });");
        }

        sb.AppendLine("        return _lingua_r;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    /// <summary>
    /// Builds the nested <c>Keys</c> static class containing a
    /// <c>public static readonly</c> <see cref="global::Irihi.Lingua.LinguaKey"/> member per resource key.
    /// </summary>
    /// <remarks>
    /// Keys is a nested type with its own lazy static initializer.
    /// C# guarantees that the outer type's initializer (which sets <c>Instance</c>) runs
    /// before <c>Keys</c>'s initializer, because <c>Keys</c> is only initialized when first
    /// accessed, which cannot happen before the outer type is initialized.
    /// The constructor of the outer type does NOT reference <c>Keys</c>, so there is no
    /// circular initialization.
    /// </remarks>
    private static void BuildKeysClass(StringBuilder sb, string[] keys)
    {
        sb.AppendLine("    /// <summary>Provides strongly-typed <see cref=\"global::Irihi.Lingua.LinguaKey\"/> members for each resource key.</summary>");
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

            sb.AppendLine($"        /// <summary>Key for the <c>{EscapeXmlComment(key)}</c> resource.</summary>");
            sb.AppendLine($"        public static readonly global::Irihi.Lingua.LinguaKey {identifier} =");
            sb.AppendLine($"            new global::Irihi.Lingua.LinguaKey(\"{EscapeString(key)}\", Instance);");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    /// <summary>
    /// Builds the per-key <c>LinguaObservableString</c> backing fields and their
    /// corresponding public <c>IObservable&lt;string?&gt;</c> properties.
    /// </summary>
    private static void BuildObservableMembers(
        StringBuilder sb,
        string[] keys,
        Dictionary<string, string> defaultValues)
    {
        var usedIdentifiers = new HashSet<string>(StringComparer.Ordinal);

        foreach (var key in keys)
        {
            var identifier = SanitizeIdentifier(key);
            if (!usedIdentifiers.Add(identifier))
            {
                continue;
            }

            defaultValues.TryGetValue(key, out var defaultValue);
            defaultValue ??= string.Empty;

            sb.AppendLine($"    private readonly global::Irihi.Lingua.LinguaObservableString _lingua_{identifier} =");
            sb.AppendLine($"        new global::Irihi.Lingua.LinguaObservableString(\"{EscapeString(key)}\", @\"{EscapeVerbatimString(defaultValue)}\");");
            sb.AppendLine();
            sb.AppendLine($"    /// <summary>Gets an observable that emits the current value of the <c>{EscapeXmlComment(key)}</c> resource key.</summary>");
            sb.AppendLine($"    public global::System.IObservable<string?> {identifier} => _lingua_{identifier};");
            sb.AppendLine();
        }
    }

    /// <summary>Builds the <c>_lingua_observables</c> instance field declaration (assigned in constructor).</summary>
    private static void BuildObservablesField(StringBuilder sb)
    {
        sb.AppendLine("    private readonly global::System.Collections.Generic.IReadOnlyDictionary<string, global::Irihi.Lingua.LinguaObservableString> _lingua_observables;");
        sb.AppendLine();
    }

    /// <summary>Builds the <c>_lingua_runtime</c> field that holds runtime-added resource overrides.</summary>
    private static void BuildRuntimeResourcesField(StringBuilder sb)
    {
        sb.AppendLine("    private readonly global::Irihi.Lingua.LinguaRuntimeResources _lingua_runtime = new global::Irihi.Lingua.LinguaRuntimeResources();");
        sb.AppendLine();
    }

    /// <summary>Builds the <c>UpdateCulture</c> method that implements <c>ILinguaManager</c>.</summary>
    private static void BuildUpdateCultureMethod(StringBuilder sb)
    {
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Switches all observable properties to the values for <paramref name=\"culture\"/>.");
        sb.AppendLine("    /// Falls back to the parent culture, then to the default (invariant) culture.");
        sb.AppendLine("    /// Runtime resources added via <see cref=\"global::Irihi.Lingua.ILinguaManager.AddResources\"/> take precedence.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public void UpdateCulture(global::System.Globalization.CultureInfo culture)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (culture is null)");
        sb.AppendLine("        {");
        sb.AppendLine("            culture = global::System.Globalization.CultureInfo.InvariantCulture;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        var _lingua_static_dict = _lingua_resources.Resolve(culture);");
        sb.AppendLine();
        sb.AppendLine("        var _lingua_extra_dict = _lingua_runtime.Resolve(culture);");
        sb.AppendLine();
        sb.AppendLine("        if (_lingua_static_dict is null && _lingua_extra_dict is null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        foreach (var _lingua_obs in _lingua_observables.Values)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (_lingua_extra_dict is not null && _lingua_extra_dict.TryGetValue(_lingua_obs.Key, out var _lingua_ev))");
        sb.AppendLine("                _lingua_obs.OnNext(_lingua_ev);");
        sb.AppendLine("            else if (_lingua_static_dict is not null && _lingua_static_dict.TryGetValue(_lingua_obs.Key, out var _lingua_sv))");
        sb.AppendLine("                _lingua_obs.OnNext(_lingua_sv);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    /// <summary>Builds the <c>GetObservable</c> method that implements <c>ILinguaManager</c>.</summary>
    private static void BuildGetObservableMethod(StringBuilder sb)
    {
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Returns the observable for the given resource key, or <c>null</c> if the key is not found.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public global::System.IObservable<string?>? GetObservable(string key)");
        sb.AppendLine("    {");
        sb.AppendLine("        _lingua_observables.TryGetValue(key, out var _lingua_obs);");
        sb.AppendLine("        return _lingua_obs;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    /// <summary>Builds the <c>AddResources</c> method that implements <c>ILinguaManager</c>.</summary>
    private static void BuildAddResourcesMethod(StringBuilder sb)
    {
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Adds or overrides resource entries for the given culture at runtime.");
        sb.AppendLine("    /// Call <see cref=\"UpdateCulture\"/> afterwards to apply the changes to all observables.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public void AddResources(global::System.Globalization.CultureInfo culture, global::System.Collections.Generic.IReadOnlyDictionary<string, string> resources)");
        sb.AppendLine("        => _lingua_runtime.Add(culture, resources);");
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
        public string Accessibility { get; }
        public Location AttributeLocation { get; }

        public ManagerInfo(string className, string? ns, string resourcePath, string accessibility, Location attributeLocation)
        {
            ClassName = className;
            AttributeLocation = attributeLocation;
            Namespace = ns;
            ResourcePath = resourcePath;
            Accessibility = accessibility;
        }
    }
}