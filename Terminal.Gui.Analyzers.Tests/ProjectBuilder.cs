using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using Microsoft.CodeAnalysis.CodeActions;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Document = Microsoft.CodeAnalysis.Document;
using Formatter = Microsoft.CodeAnalysis.Formatting.Formatter;
using System.Reflection;
using JetBrains.Annotations;

public sealed class ProjectBuilder
{
    private string _sourceCode;
    private string _expectedFixedCode;
    private DiagnosticAnalyzer _analyzer;
    private CodeFixProvider _codeFix;

    public ProjectBuilder WithSourceCode (string source)
    {
        _sourceCode = source;
        return this;
    }

    public ProjectBuilder ShouldFixCodeWith (string expected)
    {
        _expectedFixedCode = expected;
        return this;
    }

    public ProjectBuilder WithAnalyzer (DiagnosticAnalyzer analyzer)
    {
        _analyzer = analyzer;
        return this;
    }

    public ProjectBuilder WithCodeFix (CodeFixProvider codeFix)
    {
        _codeFix = codeFix;
        return this;
    }

    public async Task ValidateAsync ()
    {
        if (_sourceCode == null)
        {
            throw new InvalidOperationException ("Source code not set.");
        }

        if (_analyzer == null)
        {
            throw new InvalidOperationException ("Analyzer not set.");
        }

        // Parse original document
        var document = CreateDocument (_sourceCode);
        var compilation = await document.Project.GetCompilationAsync ();

        var diagnostics = compilation.GetDiagnostics ();
        var errors = diagnostics.Where (d => d.Severity == DiagnosticSeverity.Error);

        if (errors.Any ())
        {
            var errorMessages = string.Join (Environment.NewLine, errors.Select (e => e.ToString ()));
            throw new Exception ("Compilation failed with errors:" + Environment.NewLine + errorMessages);
        }

        // Run analyzer
        var analyzerDiagnostics = await GetAnalyzerDiagnosticsAsync (compilation, _analyzer);

        Assert.NotEmpty (analyzerDiagnostics);

        if (_expectedFixedCode != null)
        {
            if (_codeFix == null)
            {
                throw new InvalidOperationException ("Expected code fix but none was set.");
            }

            var fixedDocument = await ApplyCodeFixAsync (document, analyzerDiagnostics.First (), _codeFix);

            var formattedDocument = await Formatter.FormatAsync (fixedDocument);
            var fixedSource = (await formattedDocument.GetTextAsync ()).ToString ();

            Assert.Equal (_expectedFixedCode, fixedSource);
        }
    }

    private static Document CreateDocument (string source)
    {
        var dd = typeof (Enumerable).GetTypeInfo ().Assembly.Location;
        var coreDir = Directory.GetParent (dd) ?? throw new Exception ($"Could not find parent directory of dotnet sdk.  Sdk directory was {dd}");

        var workspace = new AdhocWorkspace ();
        var projectId = ProjectId.CreateNewId ();
        var documentId = DocumentId.CreateNewId (projectId);

        var references = new List<MetadataReference> ()
        {
            MetadataReference.CreateFromFile(typeof(Button).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(View).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.IO.FileSystemInfo).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(MarshalByValueComponent).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ObservableCollection<string>).Assembly.Location),

            // New assemblies required by Terminal.Gui version 2
            MetadataReference.CreateFromFile(typeof(Size).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CanBeNullAttribute).Assembly.Location),


            MetadataReference.CreateFromFile(Path.Combine(coreDir.FullName, "mscorlib.dll")),
            MetadataReference.CreateFromFile(Path.Combine(coreDir.FullName, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(coreDir.FullName, "System.Collections.dll")),
            MetadataReference.CreateFromFile(Path.Combine(coreDir.FullName, "System.Data.Common.dll")),
            // Add more as necessary
        };


        var projectInfo = ProjectInfo.Create (
                                              projectId,
                                              VersionStamp.Create (),
                                              "TestProject",
                                              "TestAssembly",
                                              LanguageNames.CSharp,
                                              compilationOptions: new CSharpCompilationOptions (OutputKind.DynamicallyLinkedLibrary),
                                              metadataReferences: references);

        var solution = workspace.CurrentSolution
                                .AddProject (projectInfo)
                                .AddDocument (documentId, "Test.cs", SourceText.From (source));

        return solution.GetDocument (documentId)!;
    }

    private static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync (Compilation compilation, DiagnosticAnalyzer analyzer)
    {
        var compilationWithAnalyzers = compilation.WithAnalyzers (ImmutableArray.Create (analyzer));
        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync ();
    }

    private static async Task<Document> ApplyCodeFixAsync (Document document, Diagnostic diagnostic, CodeFixProvider codeFix)
    {
        CodeAction _codeAction = null;
        var context = new CodeFixContext ((TextDocument)document, diagnostic, (action, _) => _codeAction = action, CancellationToken.None);

        await codeFix.RegisterCodeFixesAsync (context);

        if (_codeAction == null)
        {
            throw new InvalidOperationException ("Code fix did not register a fix.");
        }

        var operations = await _codeAction.GetOperationsAsync (CancellationToken.None);
        var solution = operations.OfType<ApplyChangesOperation> ().First ().ChangedSolution;
        return solution.GetDocument (document.Id);
    }
}
