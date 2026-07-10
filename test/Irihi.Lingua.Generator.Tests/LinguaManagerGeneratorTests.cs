using System.Text.RegularExpressions;
using Xunit;

namespace Irihi.Lingua.Generator.Tests;

public class LinguaManagerGeneratorTests : LinguaManagerGeneratorTestBase
{
    // ── Sample resx content ──────────────────────────────────────────────────

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
        using Irihi.Lingua;

        namespace TestApp;

        [LinguaManager("./Resources/Strings.resx")]
        public partial class LanguageManager { }
        """;

    // ── Basic generation ─────────────────────────────────────────────────────

    [Fact]
    public void Generator_WithDefaultResx_ProducesOneSourceFile()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        Assert.Empty(result.Diagnostics);
        Assert.Single(result.GeneratedSources);
    }

    [Fact]
    public void Generator_WithDefaultResx_GeneratesCorrectFileName()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        Assert.Equal("LanguageManager.LinguaManager.g.cs",
            result.GeneratedSources[0].HintName);
    }

    [Fact]
    public void Generator_WithDefaultResx_GeneratedSourceContainsClassName()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("public partial class LanguageManager", source);
    }

    [Fact]
    public void Generator_WithDefaultResx_GeneratedSourceContainsNamespace()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("namespace TestApp;", source);
    }

    [Fact]
    public void Generator_WithDefaultResx_ImplementsILinguaManager()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains(": global::Irihi.Lingua.ILinguaManager", source);
    }

    [Fact]
    public void Generator_WithDefaultResx_ContainsInstanceSingleton()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("public static readonly LanguageManager Instance", source);
    }

    [Fact]
    public void Generator_WithDefaultResx_ContainsKeysNestedClass()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("public static class Keys", source);
    }

    [Fact]
    public void Generator_WithDefaultResx_KeysContainsExpectedMembers()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("public static readonly global::Irihi.Lingua.LinguaKey App_Title", source);
        Assert.Contains("public static readonly global::Irihi.Lingua.LinguaKey Greeting_Message", source);
    }

    [Fact]
    public void Generator_WithDefaultResx_ContainsObservableProperties()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("public global::System.IObservable<string?> App_Title", source);
        Assert.Contains("public global::System.IObservable<string?> Greeting_Message", source);
    }

    [Fact]
    public void Generator_WithDefaultResx_ContainsUpdateCultureMethod()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("public void UpdateCulture(global::System.Globalization.CultureInfo culture)", source);
    }

    [Fact]
    public void Generator_WithDefaultResx_ContainsGetObservableMethod()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("public global::System.IObservable<string?>? GetObservable(string key)", source);
    }

    [Fact]
    public void Generator_WithDefaultResx_ContainsAddResourcesMethod()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains(
            "public void AddResources(global::System.Globalization.CultureInfo culture, global::System.Collections.Generic.IReadOnlyDictionary<string, string> resources)",
            source);
    }

    [Fact]
    public void Generator_WithDefaultResx_AddResourcesDelegatesToRuntimeField()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("_lingua_runtime.Add(culture, resources)", source);
    }

    [Fact]
    public void Generator_WithDefaultResx_ContainsRuntimeResourcesField()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("global::Irihi.Lingua.LinguaRuntimeResources _lingua_runtime", source);
    }

    [Fact]
    public void Generator_WithDefaultResx_DefaultValuesEmbeddedInSource()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("My Application", source);
        Assert.Contains("Hello, World!", source);
    }

    // ── CultureChanges ────────────────────────────────────────────────────────

    [Fact]
    public void Generator_WithDefaultResx_ContainsCultureChangesProperty()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains(
            "public global::System.IObservable<global::System.Globalization.CultureInfo> CultureChanges",
            source);
    }

    [Fact]
    public void Generator_WithDefaultResx_ContainsCultureChangesField()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains(
            "private readonly global::Irihi.Lingua.LinguaObservable<global::System.Globalization.CultureInfo> _cultureChanges",
            source);
    }

    [Fact]
    public void Generator_WithDefaultResx_UpdateCultureNotifiesCultureChanges()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("_cultureChanges.OnNext(culture)", source);
    }

    [Fact]
    public void Generator_WithDefaultResx_CultureChangesInitialisedWithInvariantCulture()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains(
            "new global::Irihi.Lingua.LinguaObservable<global::System.Globalization.CultureInfo>(string.Empty, global::System.Globalization.CultureInfo.InvariantCulture)",
            source);
    }

    // ── Multiple cultures ────────────────────────────────────────────────────

    [Fact]
    public void Generator_WithCultureVariant_IncludesAllCulturesInResources()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent),
            ("Strings.zh-Hans.resx", ZhHansResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        // Both culture keys appear in _lingua_resources via Add calls
        Assert.Contains("_lingua_r.Add(global::System.Globalization.CultureInfo.InvariantCulture,", source); // default culture
        Assert.Contains("_lingua_r.Add(new global::System.Globalization.CultureInfo(\"zh-Hans\"),", source);  // zh-Hans culture
    }

    [Fact]
    public void Generator_WithCultureVariant_LocalizedValuesEmbedded()
    {
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent),
            ("Strings.zh-Hans.resx", ZhHansResxContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("我的应用程序", source);
        Assert.Contains("你好，世界！", source);
    }

    [Fact]
    public void Generator_CultureVariantOnly_UsesVariantKeysForGeneration()
    {
        // When there is only a culture-specific file (no default), the generator
        // should still pick up the keys from that file.
        var result = RunGenerator(InputSource,
            ("Strings.zh-Hans.resx", ZhHansResxContent));

        Assert.Single(result.GeneratedSources);
        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("App_Title", source);
        Assert.Contains("Greeting_Message", source);
    }

    // ── No / empty / invalid resx ────────────────────────────────────────────

    [Fact]
    public void Generator_NoResxFiles_ProducesNoOutput()
    {
        var result = RunGenerator(InputSource);

        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void Generator_ResxFileWithNoDataElements_ProducesNoOutput()
    {
        const string emptyResx = """
            <?xml version="1.0" encoding="utf-8"?>
            <root />
            """;

        var result = RunGenerator(InputSource,
            ("Strings.resx", emptyResx));

        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void Generator_InvalidXml_ProducesNoOutput()
    {
        const string invalidResx = "this is not xml at all <<<";

        var result = RunGenerator(InputSource,
            ("Strings.resx", invalidResx));

        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void Generator_UnrelatedResxFile_IsIgnored()
    {
        // Providing a resx whose base name does not match the configured path
        // should produce no output.
        var result = RunGenerator(InputSource,
            ("OtherFile.resx", DefaultResxContent));

        Assert.Empty(result.GeneratedSources);
    }

    // ── Identifier sanitization ──────────────────────────────────────────────

    [Fact]
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
        Assert.Contains("some_dotted_key", source);
    }

    [Fact]
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
        Assert.Contains("_1_key", source);
    }

    // ── No namespace ─────────────────────────────────────────────────────────

    [Fact]
    public void Generator_NoNamespace_OmitsNamespaceDeclaration()
    {
        const string source = """
            using Irihi.Lingua;

            [LinguaManager("./Resources/Strings.resx")]
            public partial class LanguageManager { }
            """;

        var result = RunGenerator(source, ("Strings.resx", DefaultResxContent));
        var generatedSource = result.GeneratedSources[0].SourceText.ToString();

        Assert.False(generatedSource.Contains("namespace "),
            "Expected no namespace declaration for a global namespace class.");
    }

    // ── Access level matching ─────────────────────────────────────────────────

    [Fact]
    public void Generator_InternalClass_GeneratesInternalPartialClass()
    {
        const string source = """
            using Irihi.Lingua;

            namespace TestApp;

            [LinguaManager("./Resources/Strings.resx")]
            internal partial class LanguageManager { }
            """;

        var result = RunGenerator(source, ("Strings.resx", DefaultResxContent));
        var generatedSource = result.GeneratedSources[0].SourceText.ToString();

        Assert.Contains("internal partial class LanguageManager", generatedSource);
    }

    [Fact]
    public void Generator_PublicClass_GeneratesPublicPartialClass()
    {
        var result = RunGenerator(InputSource, ("Strings.resx", DefaultResxContent));
        var generatedSource = result.GeneratedSources[0].SourceText.ToString();

        Assert.Contains("public partial class LanguageManager", generatedSource);
    }

    // ── Compilation verification ─────────────────────────────────────────────

    [Fact]
    public void Generator_GeneratedSource_CompilesWithoutErrors()
    {
        var (outputCompilation, _) = RunGeneratorWithCompilation(InputSource,
            ("Strings.resx", DefaultResxContent));

        // Filter only errors (ignore warnings)
        var errors = outputCompilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(d => d.Severity == global::Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToList();

        Assert.Empty(errors);
    }

    // ── Run result structure ─────────────────────────────────────────────────

    [Fact]
    public void Generator_RunResult_HasNoException()
    {
        var (driver, _) = RunGeneratorFull(InputSource,
            ("Strings.resx", DefaultResxContent));

        var runResult = driver.GetRunResult();
        Assert.Single(runResult.Results);
        Assert.Null(runResult.Results[0].Exception);
    }

    [Fact]
    public void Generator_RunResult_GeneratorDiagnosticsAreEmpty()
    {
        var (driver, _) = RunGeneratorFull(InputSource,
            ("Strings.resx", DefaultResxContent));

        var runResult = driver.GetRunResult();
        Assert.Empty(runResult.Diagnostics);
    }

    // ── Duplicate keys and reachable edge cases ──────────────────────────────

    [Fact]
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
        Assert.Single(result.GeneratedSources);
        var source = result.GeneratedSources[0].SourceText.ToString();
        var count = Regex
            .Matches(source, @"public static readonly global::Irihi\.Lingua\.LinguaKey key_foo")
            .Count;
        Assert.Equal(1, count);
    }

    [Fact]
    public void Generator_EmptyCultureTag_IsIgnored()
    {
        // A file named "Strings..resx" would produce an empty culture tag after
        // stripping the base name prefix. It should be silently skipped.
        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent),
            ("Strings..resx", ZhHansResxContent));

        Assert.Single(result.GeneratedSources);
        var source = result.GeneratedSources[0].SourceText.ToString();
        // Only the default culture key should appear in resources
        // If the empty-culture file were accidentally included, its key would
        // appear as ["."]. Verify it does not.
        const string spuriousEmptyCultureKey = "[\".\"]\"";        Assert.False(source.Contains(spuriousEmptyCultureKey),
            "The spurious '.' culture key must not appear in the generated code.");
    }

    [Fact]
    public void Generator_CultureVariantWithInvalidXml_GeneratesOutputUsingDefaultCulture()
    {
        // When a culture-variant file has malformed XML, ParseValues catches the
        // XmlException and returns an empty dictionary. The generator should still
        // produce output based on the valid default-culture file.
        const string invalidCultureResx = "<<<not xml>>>";

        var result = RunGenerator(InputSource,
            ("Strings.resx", DefaultResxContent),
            ("Strings.zh-Hans.resx", invalidCultureResx));

        Assert.Single(result.GeneratedSources);
        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("App_Title", source);
    }

    // ── Cross-contamination with JSON ────────────────────────────────────────

    [Fact]
    public void Generator_ResxDoesNotPickUpJsonFilesWithSameBaseName()
    {
        // When using .resx path, .json files with the same base name must be ignored.
        const string someJson = """
            { "greeting": "Hello" }
            """;

        var result = RunGenerator(InputSource,
            ("Strings.json", someJson));

        Assert.Empty(result.GeneratedSources);
    }
}
