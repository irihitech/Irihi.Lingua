using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Irihi.Lingua.Generator.Tests;

/// <summary>
/// Shared test infrastructure for the LinguaManager source generator tests.
/// Provides helper methods to run the generator in-process and common RESX
/// sample content used by both RESX and JSON cross-contamination tests.
/// </summary>
public abstract class LinguaManagerGeneratorTestBase
{
    /// <summary>Sample default-culture .resx content used by the RESX tests
    /// and by JSON isolation tests.</summary>
    protected const string DefaultResxContent = """
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

    // ── Helpers ──────────────────────────────────────────────────────────────

    protected static GeneratorRunResult RunGenerator(
        string source,
        params (string FileName, string Content)[] resourceFiles)
    {
        var (driver, _) = RunGeneratorFull(source, resourceFiles);
        var runResult = driver.GetRunResult();
        return runResult.Results[0];
    }

    protected static (Compilation Output, ImmutableArray<Diagnostic> Diagnostics)
        RunGeneratorWithCompilation(
            string source,
            params (string FileName, string Content)[] resourceFiles)
    {
        var (_, pair) = RunGeneratorFull(source, resourceFiles);
        return pair;
    }

    protected static (GeneratorDriver Driver, (Compilation Output, ImmutableArray<Diagnostic> Diagnostics))
        RunGeneratorFull(
            string source,
            params (string FileName, string Content)[] resourceFiles)
    {
        var compilation = CreateCompilation(source);
        var additionalTexts = resourceFiles
            .Select(f => (AdditionalText)new InMemoryAdditionalText(
                Path.Combine("/fake/path", f.FileName),
                f.Content))
            .ToImmutableArray();

        var generator = new LinguaManagerGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: additionalTexts);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var outputCompilation, out var diagnostics);

        return (driver, (outputCompilation, diagnostics));
    }

    private static Compilation CreateCompilation(string source)
    {
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Collections.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Linq.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Globalization.dll")),
            MetadataReference.CreateFromFile(typeof(ILinguaManager).Assembly.Location),
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

        public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
    }
}
