using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using System.Text.Json;

namespace BranchRegistration;


public class BranchRegister
{
    private static BranchRegister? instance;
    private static readonly Lock _lock = new();
    private readonly Type branches;
    private readonly Dictionary<string, Dictionary<string, string>> register;


    private BranchRegister()
    {
        register = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>
            (File.ReadAllText("./WebServer/Parts/partBranchRegister.json"))!;
        string code = BuildCode();
        branches = BranchCompiler.CompileBranches(code);
    }

    private static BranchRegister Create()
    {
        if (instance == null)
        {
            lock (_lock)
            {
                instance ??= new BranchRegister();
            }
        }
        return instance;
    }

    public static BranchRegister Get()
    {
        instance ??= Create();
        return instance;
    }

    public string Lookup(string branch)
    {
        string branchCase = (string)branches.GetMethod(branch)!.Invoke(Activator.CreateInstance(branches), null)!;
        return register[branch][branchCase];
    }

    public static void Refresh()
    {
        instance = new BranchRegister();
    }

    private string BuildCode()
    {
        string code = """
public class CaseFunctions
{

""";
        foreach (var entry in register)
        {
            if (entry.Key == "__reference__") continue;
            code += $$"""
    public string {{entry.Key}}()
    {
        return {{entry.Value["__case__"]}};
    }

""";
        }

        code += '}';
        return code;
    }

    private static class BranchCompiler
    {
        public static Type CompileBranches(string code)
        {
            using var intermediateLang = new MemoryStream();
            CSharpCompilation compilation = Compile(code);
            EmitResult result = compilation.Emit(intermediateLang);

            if (CheckForErrors(result)) throw new InvalidProgramException($"Unable to compile branches");

            intermediateLang.Seek(0, SeekOrigin.Begin);
            Assembly compiledCode = Assembly.Load(intermediateLang.ToArray());
            Type Branches = compiledCode.GetType("CaseFunctions")
                ?? throw new InvalidProgramException("Can't get the correct class from the assembly");

            return Branches;
            // Use:
            // Branches.GetMethod("Method").Invoke(Activator.CreateInstance(Branches), null);
        }

        private static CSharpCompilation Compile(string code)
        {
            string assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
            string appAssemblyPath = Path.GetDirectoryName(typeof(BranchCompiler).Assembly.Location)!;
            string assemblyName = Path.GetRandomFileName();

            CSharpCompilationOptions defaultCompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOverflowChecks(true)
                .WithOptimizationLevel(OptimizationLevel.Release);

            IEnumerable<MetadataReference> defaultReferences =
            [
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Core.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")),
                MetadataReference.CreateFromFile(Path.Combine(appAssemblyPath, "app.dll")),
                MetadataReference.CreateFromFile(Path.Combine(appAssemblyPath, "DataEngine.dll")),
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location)
            ];

            return CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: [
                    CSharpSyntaxTree.ParseText(code)
                ],
                references: defaultReferences,
                options: defaultCompilationOptions);
        }

        private static bool CheckForErrors(EmitResult result)
        {
            if (result.Success) return false;

            IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                diagnostic.IsWarningAsError ||
                diagnostic.Severity == DiagnosticSeverity.Error);
            foreach (Diagnostic diagnostic in failures)
                Console.WriteLine(diagnostic.GetMessage());

            return true;
        }
    }
}