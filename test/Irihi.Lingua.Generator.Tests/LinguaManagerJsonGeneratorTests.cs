using Microsoft.CodeAnalysis;
using Xunit;

namespace Irihi.Lingua.Generator.Tests;

/// <summary>
/// Tests for the JSON-based language manager source generation.
/// Verifies that nested JSON objects are correctly flattened into
/// underscore-separated keys and culture variants are supported.
/// </summary>
public class LinguaManagerJsonGeneratorTests : LinguaManagerGeneratorTestBase
{
    // ── Sample JSON content ───────────────────────────────────────────────────

    private const string JsonInputSource = """
        using Irihi.Lingua;

        namespace TestApp;

        [LinguaManager("./Resources/Strings.json")]
        public partial class LanguageManager { }
        """;

    private const string SimpleJsonContent = """
        {
          "a": "Content A",
          "b": "Content B"
        }
        """;

    private const string NestedJsonContent = """
        {
          "a": "Content A",
          "b": "Content B",
          "c": {
            "x": "Content X",
            "y": "Content Y",
            "z": {
              "m": "Content M",
              "n": "Content N"
            }
          }
        }
        """;

    private const string ZhHansJsonContent = """
        {
          "a": "内容A",
          "b": "内容B",
          "c": {
            "x": "内容X",
            "y": "内容Y",
            "z": {
              "m": "内容M",
              "n": "内容N"
            }
          }
        }
        """;

    // ── Basic generation ─────────────────────────────────────────────────────

    [Fact]
    public void Generator_WithJson_ProducesOneSourceFile()
    {
        var result = RunGenerator(JsonInputSource,
            ("Strings.json", SimpleJsonContent));

        Assert.Empty(result.Diagnostics);
        Assert.Single(result.GeneratedSources);
    }

    [Fact]
    public void Generator_WithSimpleJson_GeneratesFlatKeys()
    {
        var result = RunGenerator(JsonInputSource,
            ("Strings.json", SimpleJsonContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("public global::System.IObservable<string?> a", source);
        Assert.Contains("public global::System.IObservable<string?> b", source);
    }

    [Fact]
    public void Generator_WithNestedJson_GeneratesFlattenedKeys()
    {
        var result = RunGenerator(JsonInputSource,
            ("Strings.json", NestedJsonContent));

        var source = result.GeneratedSources[0].SourceText.ToString();

        // Top-level keys
        Assert.Contains("public global::System.IObservable<string?> a", source);
        Assert.Contains("public global::System.IObservable<string?> b", source);

        // Flattened nested keys: c_x, c_y, c_z_m, c_z_n
        Assert.Contains("public global::System.IObservable<string?> c_x", source);
        Assert.Contains("public global::System.IObservable<string?> c_y", source);
        Assert.Contains("public global::System.IObservable<string?> c_z_m", source);
        Assert.Contains("public global::System.IObservable<string?> c_z_n", source);
    }

    [Fact]
    public void Generator_WithNestedJson_DefaultValuesEmbedded()
    {
        var result = RunGenerator(JsonInputSource,
            ("Strings.json", NestedJsonContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("Content A", source);
        Assert.Contains("Content M", source);
        Assert.Contains("Content N", source);
    }

    [Fact]
    public void Generator_WithJson_KeysClassContainsFlattenedMembers()
    {
        var result = RunGenerator(JsonInputSource,
            ("Strings.json", NestedJsonContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("public static readonly global::Irihi.Lingua.LinguaKey a", source);
        Assert.Contains("public static readonly global::Irihi.Lingua.LinguaKey c_x", source);
        Assert.Contains("public static readonly global::Irihi.Lingua.LinguaKey c_z_m", source);
    }

    // ── Culture variants ─────────────────────────────────────────────────────

    [Fact]
    public void Generator_WithJson_CultureVariantProduced()
    {
        var result = RunGenerator(JsonInputSource,
            ("Strings.json", NestedJsonContent),
            ("Strings.zh-Hans.json", ZhHansJsonContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("_lingua_r.Add(global::System.Globalization.CultureInfo.InvariantCulture,", source);
        Assert.Contains("_lingua_r.Add(new global::System.Globalization.CultureInfo(\"zh-Hans\"),", source);
    }

    [Fact]
    public void Generator_WithJson_CultureVariantHasLocalizedValues()
    {
        var result = RunGenerator(JsonInputSource,
            ("Strings.json", NestedJsonContent),
            ("Strings.zh-Hans.json", ZhHansJsonContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("内容A", source);
        Assert.Contains("内容M", source);
    }

    [Fact]
    public void Generator_WithJson_CultureVariantOnly_StillGeneratesKeys()
    {
        var result = RunGenerator(JsonInputSource,
            ("Strings.zh-Hans.json", ZhHansJsonContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("c_z_m", source);
        Assert.Contains("c_z_n", source);
    }

    // ── Edge cases ───────────────────────────────────────────────────────────

    [Fact]
    public void Generator_WithEmptyJson_ProducesNoOutput()
    {
        var result = RunGenerator(JsonInputSource,
            ("Strings.json", "{}"));

        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void Generator_WithInvalidJson_ProducesNoOutput()
    {
        var result = RunGenerator(JsonInputSource,
            ("Strings.json", "not json at all {{{"));

        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void Generator_WithUnrelatedJsonFile_IsIgnored()
    {
        var result = RunGenerator(JsonInputSource,
            ("OtherFile.json", NestedJsonContent));

        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void Generator_JsonDoesNotPickUpResxFilesWithSameBaseName()
    {
        // When using .json path, .resx files with the same base name must be ignored.
        var result = RunGenerator(JsonInputSource,
            ("Strings.resx", DefaultResxContent));

        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void Generator_Json_SkipsNonStringLeafValues()
    {
        const string jsonWithMixedTypes = """
            {
              "title": "Hello",
              "count": 42,
              "enabled": true,
              "data": null
            }
            """;

        var result = RunGenerator(JsonInputSource,
            ("Strings.json", jsonWithMixedTypes));

        var source = result.GeneratedSources[0].SourceText.ToString();
        // Only "title" should appear (string leaf), others are non-string
        Assert.Contains("public global::System.IObservable<string?> title", source);
        // Numbers, booleans, null are skipped
        Assert.DoesNotContain("public global::System.IObservable<string?> count", source);
        Assert.DoesNotContain("public global::System.IObservable<string?> enabled", source);
        Assert.DoesNotContain("public global::System.IObservable<string?> data", source);
    }

    // ── Generated API surface ────────────────────────────────────────────────

    [Fact]
    public void Generator_WithJson_GeneratedSourceCompilesWithoutErrors()
    {
        var (outputCompilation, _) = RunGeneratorWithCompilation(JsonInputSource,
            ("Strings.json", NestedJsonContent));

        var errors = outputCompilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void Generator_WithJson_ContainsUpdateCultureMethod()
    {
        var result = RunGenerator(JsonInputSource,
            ("Strings.json", NestedJsonContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("public void UpdateCulture(global::System.Globalization.CultureInfo culture)", source);
    }

    [Fact]
    public void Generator_WithJson_ContainsGetObservableMethod()
    {
        var result = RunGenerator(JsonInputSource,
            ("Strings.json", NestedJsonContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains("public global::System.IObservable<string?>? GetObservable(string key)", source);
    }

    [Fact]
    public void Generator_WithJson_ContainsAddResourcesMethod()
    {
        var result = RunGenerator(JsonInputSource,
            ("Strings.json", NestedJsonContent));

        var source = result.GeneratedSources[0].SourceText.ToString();
        Assert.Contains(
            "public void AddResources(global::System.Globalization.CultureInfo culture, global::System.Collections.Generic.IReadOnlyDictionary<string, string> resources)",
            source);
    }
}
