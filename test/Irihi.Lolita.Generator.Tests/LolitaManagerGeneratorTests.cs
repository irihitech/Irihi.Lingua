using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Irihi.Lolita;
using Irihi.Lolita.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Irihi.Lolita.Generator.Tests;

[TestClass]
public class LolitaManagerGeneratorTests
{
    // ── Sample resx content ──────────────────────────────────────────────────

    private const string DefaultResxContent = """
        <?xml version="1.0" encoding="utf-8"?>
        <root>
          <data name="App_Title" xml:space="preserve">
            <value>My Application</value>
          </data>
          <data name="Greeting_Message" xml:space="preserve">
            <value>Hello, World!</value>
          </data>
        </root>
        """;

    private const string ZhHansResxContent = """
        <?xml version="1.0" encoding="utf-8"?>
        <root>
          <data name="App_Title" xml:space="preserve">
            <value>我的应用程序</value>
          </data>
          <data name="Greeting_Message" xml:space="preserve">
            <value>你好，世界！</value>
          </data>
        </root>
        """;

    private const string InputSource = """
        using Irihi.Lolita;

        namespace TestApp;

        [LolitaManager("./Resources/Strings.resx")]
        public partial class LanguageManager { }
        """;

    // ── Basic generation ─────────────────────────────────────────────────────

    [TestMethod]
    public void Generator_WithDefaultResx_ProducesOneSourceFile()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        Assert.AreEqual(0, result.Diagnostics.Length,
            $"Expected no diagnostics, got: {string.Join(", ", result.Diagnostics)}");
        Assert.AreEqual(1, result.GeneratedSources.Length,
            "Expected exactly one generated source file.");
    }

    [TestMethod]
    public void Generator_WithDefaultResx_GeneratesCorrectFileName()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        Assert.AreEqual("LanguageManager.LolitaManager.g.cs",
            result.GeneratedSources[0].HintName);
    }

    [TestMethod]
    public void Generator_WithDefaultResx_GeneratedSourceContainsClassName()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        StringAssert.Contains(source, "public partial class LanguageManager");
    }

    [TestMethod]
    public void Generator_WithDefaultResx_GeneratedSourceContainsNamespace()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        StringAssert.Contains(source, "namespace TestApp;");
    }

    [TestMethod]
    public void Generator_WithDefaultResx_ImplementsILolitaManager()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        StringAssert.Contains(source, ": global::Irihi.Lolita.ILolitaManager");
    }

    [TestMethod]
    public void Generator_WithDefaultResx_ContainsInstanceSingleton()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        StringAssert.Contains(source, "public static readonly LanguageManager Instance");
    }

    [TestMethod]
    public void Generator_WithDefaultResx_ContainsKeysNestedClass()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        StringAssert.Contains(source, "public static class Keys");
    }

    [TestMethod]
    public void Generator_WithDefaultResx_KeysContainsExpectedMembers()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        StringAssert.Contains(source, "public static readonly global::Irihi.Lolita.LolitaKey App_Title");
        StringAssert.Contains(source, "public static readonly global::Irihi.Lolita.LolitaKey Greeting_Message");
    }

    [TestMethod]
    public void Generator_WithDefaultResx_ContainsObservableProperties()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        StringAssert.Contains(source, "public global::System.IObservable<string?> App_Title");
        StringAssert.Contains(source, "public global::System.IObservable<string?> Greeting_Message");
    }

    [TestMethod]
    public void Generator_WithDefaultResx_ContainsUpdateCultureMethod()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        StringAssert.Contains(source, "public void UpdateCulture(global::System.Globalization.CultureInfo culture)");
    }

    [TestMethod]
    public void Generator_WithDefaultResx_ContainsGetObservableMethod()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        StringAssert.Contains(source, "public global::System.IObservable<string?>? GetObservable(string key)");
    }

    [TestMethod]
    public void Generator_WithDefaultResx_DefaultValuesEmbeddedInSource()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        StringAssert.Contains(source, "My Application");
        StringAssert.Contains(source, "Hello, World!");
    }

    // ── Multiple cultures ────────────────────────────────────────────────────

    [TestMethod]
    public void Generator_WithCultureVariant_IncludesAllCulturesInResources()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent),
            ("Strings.zh-Hans.resx", ZhHansResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        // Both culture keys appear in _lolita_resources
        StringAssert.Contains(source, "[\"\"]");      // default culture
        StringAssert.Contains(source, "[\"zh-Hans\"]"); // zh-Hans culture
    }

    [TestMethod]
    public void Generator_WithCultureVariant_LocalizedValuesEmbedded()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent),
            ("Strings.zh-Hans.resx", ZhHansResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        StringAssert.Contains(source, "我的应用程序");
        StringAssert.Contains(source, "你好，世界！");
    }

    [TestMethod]
    public void Generator_CultureVariantOnly_UsesVariantKeysForGeneration()
    {
        // When there is only a culture-specific file (no default), the generator
        // should still pick up the keys from that file.
        var result = RunGenerator(InputSource,
            ("Strings.zh-Hans.resx", ZhHansResxContent));

        Assert.AreEqual(1, result.GeneratedSources.Length);
        var source = result.GeneratedSources[0].SourceText.ToString();
        StringAssert.Contains(source, "App_Title");
        StringAssert.Contains(source, "Greeting_Message");
    }

    // ── No / empty / invalid resx ────────────────────────────────────────────

    [TestMethod]
    public void Generator_NoResxFiles_ProducesNoOutput()
    {
        var result = RunGenerator(InputSource);

        Assert.AreEqual(0, result.GeneratedSources.Length,
            "Expected no generated files when no resx is present.");
    }

    [TestMethod]
    public void Generator_ResxFileWithNoDataElements_ProducesNoOutput()
    {
        const string emptyResx = """
            <?xml version="1.0" encoding="utf-8"?>
            <root />
            """;

        var result = RunGenerator(InputSource,
            ("Strings.resx", emptyResx));

        Assert.AreEqual(0, result.GeneratedSources.Length,
            "Expected no generated files when resx has no data elements.");
    }

    [TestMethod]
    public void Generator_InvalidXml_ProducesNoOutput()
    {
        const string invalidResx = "this is not xml at all <<<";

        var result = RunGenerator(InputSource,
            ("Strings.resx", invalidResx));

        Assert.AreEqual(0, result.GeneratedSources.Length,
            "Expected no generated files when resx XML is invalid.");
    }

    [TestMethod]
    public void Generator_UnrelatedResxFile_IsIgnored()
    {
        // Providing a resx whose base name does not match the configured path
        // should produce no output.
        var result = RunGenerator(InputSource,
            ("OtherFile.resx", DefaultResxContent));

        Assert.AreEqual(0, result.GeneratedSources.Length,
            "Expected no generated files when resx base name does not match.");
    }

    // ── Identifier sanitization ──────────────────────────────────────────────

    [TestMethod]
    public void Generator_KeyWithSpecialChars_SanitizesIdentifier()
    {
        const string resx = """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <data name="some.dotted.key" xml:space="preserve">
                <value>Value</value>
              </data>
            </root>
            """;

        var result = RunGenerator(InputSource, ("Strings.resx", resx));
        var source = result.GeneratedSources[0].SourceText.ToString();

        // Dots are converted to underscores
        StringAssert.Contains(source, "some_dotted_key");
    }

    [TestMethod]
    public void Generator_KeyStartingWithDigit_PrependedWithUnderscore()
    {
        const string resx = """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <data name="1_key" xml:space="preserve">
                <value>Value</value>
              </data>
            </root>
            """;

        var result = RunGenerator(InputSource, ("Strings.resx", resx));
        var source = result.GeneratedSources[0].SourceText.ToString();

        // Identifiers starting with a digit get an underscore prepended
        StringAssert.Contains(source, "_1_key");
    }

    // ── No namespace ─────────────────────────────────────────────────────────

    [TestMethod]
    public void Generator_NoNamespace_OmitsNamespaceDeclaration()
    {
        const string source = """
            using Irihi.Lolita;

            [LolitaManager("./Resources/Strings.resx")]
            public partial class LanguageManager { }
            """;

        var result = RunGenerator(source, ("Strings.resx", DefaultResxContent));
        var generatedSource = result.GeneratedSources[0].SourceText.ToString();

        Assert.IsFalse(generatedSource.Contains("namespace "),
            "Expected no namespace declaration for a global namespace class.");
    }

    // ── Compilation verification ─────────────────────────────────────────────

    [TestMethod]
    public void Generator_GeneratedSource_CompilesWithoutErrors()
    {
        var (outputCompilation, diagnostics) = RunGeneratorWithCompilation(InputSource,
            ("Strings.resx", DefaultResxContent));

        // Filter only errors (ignore warnings)
        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.AreEqual(0, errors.Count,
            $"Generated code has compilation errors:\n{string.Join("\n", errors.Select(e => e.ToString()))}");
    }

    // ── Run result structure ─────────────────────────────────────────────────

    [TestMethod]
    public void Generator_RunResult_HasNoException()
    {
        var (driver, _) = RunGeneratorFull(InputSource,
            ("Strings.resx", DefaultResxContent));

        var runResult = driver.GetRunResult();
        Assert.AreEqual(1, runResult.Results.Length);
        Assert.IsNull(runResult.Results[0].Exception,
            "Generator threw an unexpected exception.");
    }

    [TestMethod]
    public void Generator_RunResult_GeneratorDiagnosticsAreEmpty()
    {
        var (driver, _) = RunGeneratorFull(InputSource,
            ("Strings.resx", DefaultResxContent));

        var runResult = driver.GetRunResult();
        Assert.AreEqual(0, runResult.Diagnostics.Length,
            $"Expected no diagnostics, got: {string.Join(", ", runResult.Diagnostics)}");
    }

    // ── Duplicate keys and reachable edge cases ──────────────────────────────

    [TestMethod]
    public void Generator_DuplicateKeys_DeduplicatesIdentifiers()
    {
        // Two keys that sanitize to the same identifier (dots → underscores)
        // should appear only once in the generated output.
        const string resx = """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <data name="key.foo" xml:space="preserve">
                <value>First</value>
              </data>
              <data name="key-foo" xml:space="preserve">
                <value>Second</value>
              </data>
            </root>
            """;

        var result = RunGenerator(InputSource, ("Strings.resx", resx));
        Assert.AreEqual(1, result.GeneratedSources.Length);
        var source = result.GeneratedSources[0].SourceText.ToString();

        // Only one LolitaKey member with the sanitized identifier
        var count = Regex
            .Matches(source, @"public static readonly global::Irihi\.Lolita\.LolitaKey key_foo")
            .Count;
        Assert.AreEqual(1, count, "Duplicate sanitized identifier should appear only once in Keys class.");
    }

    [TestMethod]
    public void Generator_EmptyCultureTag_IsIgnored()
    {
        // A file named "Strings..resx" would produce an empty culture tag after
        // stripping the base name prefix. It should be silently skipped.
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent),
            ("Strings..resx", ZhHansResxContent));

        Assert.AreEqual(1, result.GeneratedSources.Length,
            "Expected exactly one generated file — the double-dot resx should be ignored.");
        var source = result.GeneratedSources[0].SourceText.ToString();
        // Only the default culture key should appear in resources
        // If the empty-culture file were accidentally included, its key would
        // appear as ["."]. Verify it does not.
        const string spuriousEmptyCultureKey = "[\".\"]\"";        Assert.IsFalse(source.Contains(spuriousEmptyCultureKey),
            "The spurious '.' culture key must not appear in the generated code.");
    }

    [TestMethod]
    public void Generator_CultureVariantWithInvalidXml_GeneratesOutputUsingDefaultCulture()
    {
        // When a culture-variant file has malformed XML, ParseValues catches the
        // XmlException and returns an empty dictionary. The generator should still
        // produce output based on the valid default-culture file.
        const string invalidCultureResx = "<<<not xml>>>";

        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent),
            ("Strings.zh-Hans.resx", invalidCultureResx));

        Assert.AreEqual(1, result.GeneratedSources.Length,
            "Expected output even when a culture variant file has invalid XML.");
        var source = result.GeneratedSources[0].SourceText.ToString();
        StringAssert.Contains(source, "App_Title");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static GeneratorRunResult RunGenerator(
        string source,
        params (string FileName, string Content)[] resxFiles)
    {
        var (driver, _) = RunGeneratorFull(source, resxFiles);
        var runResult = driver.GetRunResult();
        return runResult.Results[0];
    }

    private static (Compilation Output, ImmutableArray<Diagnostic> Diagnostics)
        RunGeneratorWithCompilation(
            string source,
            params (string FileName, string Content)[] resxFiles)
    {
        var (_, pair) = RunGeneratorFull(source, resxFiles);
        return pair;
    }

    private static (GeneratorDriver Driver, (Compilation Output, ImmutableArray<Diagnostic> Diagnostics))
        RunGeneratorFull(
            string source,
            params (string FileName, string Content)[] resxFiles)
    {
        var compilation = CreateCompilation(source);
        var additionalTexts = resxFiles
            .Select(f => (AdditionalText)new InMemoryAdditionalText(
                Path.Combine("/fake/path", f.FileName),
                f.Content))
            .ToImmutableArray();

        var generator = new LolitaManagerGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new ISourceGenerator[] { generator.AsSourceGenerator() },
            additionalTexts: additionalTexts);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var outputCompilation, out var diagnostics);

        return (driver, (outputCompilation, diagnostics));
    }

    private static Compilation CreateCompilation(string source)
    {
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        // Build a minimal but complete set of metadata references so the
        // generated code (which uses Irihi.Lolita types, CultureInfo, etc.)
        // compiles cleanly in the test compilation.
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Collections.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Linq.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Globalization.dll")),
            // Reference the Core project assembly so that LolitaManagerAttribute,
            // ILolitaManager, LolitaObservableString, and LolitaKey are available.
            MetadataReference.CreateFromFile(typeof(Irihi.Lolita.ILolitaManager).Assembly.Location),
        };

        return CSharpCompilation.Create(
            "test_compilation",
            new[] { CSharpSyntaxTree.ParseText(source) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    // ── InMemoryAdditionalText ────────────────────────────────────────────────

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly string _path;
        private readonly SourceText _text;

        public InMemoryAdditionalText(string path, string content)
        {
            _path = path;
            _text = SourceText.From(content, Encoding.UTF8);
        }

        public override string Path => _path;

        public override SourceText? GetText(CancellationToken cancellationToken = default) => _text;
    }
}
