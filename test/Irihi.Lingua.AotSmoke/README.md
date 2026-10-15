# NativeAOT smoke check

This executable verifies the generated RESX and JSON managers, all format argument
overloads, subscription lifetimes, culture switches, and compiled Avalonia XAML.
It uses Avalonia's headless platform and fails if dynamic code is available, so
running the managed build is not a substitute for publishing and running the
native executable.

The entire core assembly is rooted for AOT analysis. Trimming and AOT warnings
are errors; LINGUA002 is intentionally disabled because the partial French
resource file exercises missing-template culture changes.

From the repository root on Windows:

```powershell
dotnet publish test/Irihi.Lingua.AotSmoke/Irihi.Lingua.AotSmoke.csproj -c Release -f net8.0 -r win-x64 -o artifacts/aot-smoke
./artifacts/aot-smoke/Irihi.Lingua.AotSmoke.exe
```

For .NET 10, use `-f net10.0`. On Linux, use `-r linux-x64` and run
`./artifacts/aot-smoke/Irihi.Lingua.AotSmoke`.

`PublishAot` is set in the executable project. Avoid passing it as a global build
property when building this repository: that also applies it to the
`netstandard2.0` source-generator project, which runs during the build.

CI publishes and runs both frameworks on Windows and Ubuntu x64.
