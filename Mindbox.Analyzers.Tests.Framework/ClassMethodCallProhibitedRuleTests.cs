using System.Reflection;
using Itc.Commons.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MindboxAnalyzers.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MindboxAnalyzers.Tests;

[TestClass]
public class ClassMethodCallProhibitedRuleTests
{
    private readonly ModelApplicationHostControllerServiceLocatorProhibitedRule _mahcRule = new();
    private readonly NamedObjectModelConfigurationRegisterProhibitedRule _nomcRule = new();

    private const string NomcCodeUsings = @"using Itc.Commons; using Itc.Commons.Model;";

    private const string NamedObjectsDefinition = @"public class TestNamedObject : NamedObject
{
    public TestNamedObject(string systemName) 
        : base(systemName, null, null)
    {
    }
}

public class TestNamedObjectComponent : NamedObjectComponent<TestNamedObject>
{
    public TestNamedObjectComponent()
    {
        TestNC1 = Add(new TestNamedObject(""TestNC1""));
        TestNC2 = Add(new TestNamedObject(""TestNC2""));
        TestNC3 = Add(new TestNamedObject(""TestNC3""));
    }
    
    public TestNamedObject TestNC1 { get; }
    public TestNamedObject TestNC2 { get; }
    public TestNamedObject TestNC3 { get; }
}
";

    private static SemanticModel GetSemanticModelFromSourceCode(string filename, string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, new CSharpParseOptions().WithLanguageVersion(LanguageVersion.CSharp10), filename);
        var compilation = CSharpCompilation.Create("TestSolution")
            .AddReferences(MetadataReference.CreateFromFile(
                    typeof(ModelApplicationHostController).Assembly.Location), // Load Commons into test compilation
                MetadataReference.CreateFromFile(
                    Assembly.Load("Mindbox.I18n, Version=1.3.2.0, Culture=neutral, PublicKeyToken=null").Location), // Load Mindbox.I18n into test compilation
                MetadataReference.CreateFromFile(
                    Assembly.Load("System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location), // Load System.Private.CoreLib into test compilation
                MetadataReference.CreateFromFile(
                    typeof(Console).Assembly.Location), // Load System.Console into test compilation
                MetadataReference.CreateFromFile(
                    Assembly.Load("System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location)) // Load System.Runtime into test compilation
            .AddSyntaxTrees(syntaxTree);

        var diagnostics = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (diagnostics.Length > 0)
        {
            throw new Exception(
                $"There were {diagnostics.Length} compile errors:\n{string.Join("\n", diagnostics.Select(d => d.ToString()))}");
        }

        return compilation.GetSemanticModel(syntaxTree);
    }

    #region MAHC Single file tests

    [TestMethod]
    public void MAHC_NoCalls_DoesntChangeSourceCode()
    {
        const string givenSource = @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(""No MAHC calls!"");
    }
}";
        var semanticModel = GetSemanticModelFromSourceCode("Program.cs", givenSource);
        _mahcRule.AnalyzeModel(semanticModel, out var problems);

        var changedFiles = new DiagnosticPragmaIgnoreAdder().AddPragmasToCode(problems);
        Assert.AreEqual(0, changedFiles.Count);
    }

    [TestMethod]
    public void MAHC_OneSingleLineCall_WrapsInPragma()
    {
        const string givenSource = @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        var service = Itc.Commons.Model.ModelApplicationHostController.Instance.Get<Itc.Commons.Model.ITenantValidator>();
    }
}";
        string expectedSource = @$"using System;

public class Program
{{
    public static void Main(string[] args)
    {{
#pragma warning disable {_mahcRule.DiagnosticDescriptor.Id}
        var service = Itc.Commons.Model.ModelApplicationHostController.Instance.Get<Itc.Commons.Model.ITenantValidator>();
#pragma warning restore {_mahcRule.DiagnosticDescriptor.Id}
    }}
}}";

        const string filename = "Program.cs";

        var semanticModel = GetSemanticModelFromSourceCode(filename, givenSource);
        _mahcRule.AnalyzeModel(semanticModel, out var problems);

        var changedFiles = new DiagnosticPragmaIgnoreAdder().AddPragmasToCode(problems);
        Assert.AreEqual(1, changedFiles.Count);
        Assert.AreEqual(expectedSource, changedFiles[filename]);
    }

    [TestMethod]
    public void MAHC_TwoSeparatedSingleLineCalls_WrapsInTwoPragmas()
    {
        const string givenSource = @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        var service1 = Itc.Commons.Model.ModelApplicationHostController.Instance.Get<Itc.Commons.Model.ITenantValidator>();
        var separator = 0;
        var service2 = Itc.Commons.Model.ModelApplicationHostController.Instance.Get<Itc.Commons.Model.ITenantValidator>();
    }
}";
        string expectedSource = @$"using System;

public class Program
{{
    public static void Main(string[] args)
    {{
#pragma warning disable {_mahcRule.DiagnosticDescriptor.Id}
        var service1 = Itc.Commons.Model.ModelApplicationHostController.Instance.Get<Itc.Commons.Model.ITenantValidator>();
#pragma warning restore {_mahcRule.DiagnosticDescriptor.Id}
        var separator = 0;
#pragma warning disable {_mahcRule.DiagnosticDescriptor.Id}
        var service2 = Itc.Commons.Model.ModelApplicationHostController.Instance.Get<Itc.Commons.Model.ITenantValidator>();
#pragma warning restore {_mahcRule.DiagnosticDescriptor.Id}
    }}
}}";

        const string filename = "Program.cs";

        var semanticModel = GetSemanticModelFromSourceCode(filename, givenSource);
        _mahcRule.AnalyzeModel(semanticModel, out var problems);

        var changedFiles = new DiagnosticPragmaIgnoreAdder().AddPragmasToCode(problems);
        Assert.AreEqual(1, changedFiles.Count);
        Assert.AreEqual(expectedSource, changedFiles[filename]);
    }

    [TestMethod]
    public void MAHC_TwoConsecutiveSingleLineCalls_WrapsInOnePragma()
    {
        const string givenSource = @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        var service1 = Itc.Commons.Model.ModelApplicationHostController.Instance.Get<Itc.Commons.Model.ITenantValidator>();
        var service2 = Itc.Commons.Model.ModelApplicationHostController.Instance.Get<Itc.Commons.Model.ITenantValidator>();
    }
}";
        string expectedSource = @$"using System;

public class Program
{{
    public static void Main(string[] args)
    {{
#pragma warning disable {_mahcRule.DiagnosticDescriptor.Id}
        var service1 = Itc.Commons.Model.ModelApplicationHostController.Instance.Get<Itc.Commons.Model.ITenantValidator>();
        var service2 = Itc.Commons.Model.ModelApplicationHostController.Instance.Get<Itc.Commons.Model.ITenantValidator>();
#pragma warning restore {_mahcRule.DiagnosticDescriptor.Id}
    }}
}}";

        const string filename = "Program.cs";

        var semanticModel = GetSemanticModelFromSourceCode(filename, givenSource);
        _mahcRule.AnalyzeModel(semanticModel, out var problems);

        var changedFiles = new DiagnosticPragmaIgnoreAdder().AddPragmasToCode(problems);
        Assert.AreEqual(1, changedFiles.Count);
        Assert.AreEqual(expectedSource, changedFiles[filename]);
    }

    [TestMethod]
    public void MAHC_OneMultiLineCall_WrapsEntireCallInPragma()
    {
        const string givenSource = @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        var service = Itc.Commons.Model.ModelApplicationHostController
            .Instance
            .Get<Itc.Commons.Model.ITenantValidator>();
    }
}";
        string expectedSource = @$"using System;

public class Program
{{
    public static void Main(string[] args)
    {{
#pragma warning disable {_mahcRule.DiagnosticDescriptor.Id}
        var service = Itc.Commons.Model.ModelApplicationHostController
            .Instance
            .Get<Itc.Commons.Model.ITenantValidator>();
#pragma warning restore {_mahcRule.DiagnosticDescriptor.Id}
    }}
}}";

        const string filename = "Program.cs";

        var semanticModel = GetSemanticModelFromSourceCode(filename, givenSource);
        _mahcRule.AnalyzeModel(semanticModel, out var problems);

        var changedFiles = new DiagnosticPragmaIgnoreAdder().AddPragmasToCode(problems);
        Assert.AreEqual(1, changedFiles.Count);
        Assert.AreEqual(expectedSource, changedFiles[filename]);
    }

    [TestMethod]
    public void MAHC_TwoSeparatedMultiLineCalls_WrapsEntireCallsInTwoPragmas()
    {
        const string givenSource = @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        var service1 = Itc.Commons.Model.ModelApplicationHostController
            .Instance
            .Get<Itc.Commons.Model.ITenantValidator>();
        var separator = 0;
        var service2 = Itc.Commons.Model.ModelApplicationHostController
            .Instance
            .Get<Itc.Commons.Model.ITenantValidator>();
    }
}";
        string expectedSource = @$"using System;

public class Program
{{
    public static void Main(string[] args)
    {{
#pragma warning disable {_mahcRule.DiagnosticDescriptor.Id}
        var service1 = Itc.Commons.Model.ModelApplicationHostController
            .Instance
            .Get<Itc.Commons.Model.ITenantValidator>();
#pragma warning restore {_mahcRule.DiagnosticDescriptor.Id}
        var separator = 0;
#pragma warning disable {_mahcRule.DiagnosticDescriptor.Id}
        var service2 = Itc.Commons.Model.ModelApplicationHostController
            .Instance
            .Get<Itc.Commons.Model.ITenantValidator>();
#pragma warning restore {_mahcRule.DiagnosticDescriptor.Id}
    }}
}}";

        const string filename = "Program.cs";

        var semanticModel = GetSemanticModelFromSourceCode(filename, givenSource);
        _mahcRule.AnalyzeModel(semanticModel, out var problems);

        var changedFiles = new DiagnosticPragmaIgnoreAdder().AddPragmasToCode(problems);
        Assert.AreEqual(1, changedFiles.Count);
        Assert.AreEqual(expectedSource, changedFiles[filename]);
    }

    [TestMethod]
    public void MAHC_TwoConsecutiveMultiLineCalls_WrapsEntireCallsInOnePragma()
    {
        const string givenSource = @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        var service1 = Itc.Commons.Model.ModelApplicationHostController
            .Instance
            .Get<Itc.Commons.Model.ITenantValidator>();
        var service2 = Itc.Commons.Model.ModelApplicationHostController
            .Instance
            .Get<Itc.Commons.Model.ITenantValidator>();
    }
}";
        string expectedSource = @$"using System;

public class Program
{{
    public static void Main(string[] args)
    {{
#pragma warning disable {_mahcRule.DiagnosticDescriptor.Id}
        var service1 = Itc.Commons.Model.ModelApplicationHostController
            .Instance
            .Get<Itc.Commons.Model.ITenantValidator>();
        var service2 = Itc.Commons.Model.ModelApplicationHostController
            .Instance
            .Get<Itc.Commons.Model.ITenantValidator>();
#pragma warning restore {_mahcRule.DiagnosticDescriptor.Id}
    }}
}}";

        const string filename = "Program.cs";

        var semanticModel = GetSemanticModelFromSourceCode(filename, givenSource);
        _mahcRule.AnalyzeModel(semanticModel, out var problems);

        var changedFiles = new DiagnosticPragmaIgnoreAdder().AddPragmasToCode(problems);
        Assert.AreEqual(1, changedFiles.Count);
        Assert.AreEqual(expectedSource, changedFiles[filename]);
    }

    #endregion

    #region MAHC Multiple file tests

    [TestMethod]
    public void MAHC_MultipleFile_NoCalls_DoesntChangeSourceCode()
    {
        var files = new (string filename, string source, string expected)[]
        {
            ("Program.cs", @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(""No MAHC calls!"");
    }
}", @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(""No MAHC calls!"");
    }
}"),
            ("MyClass.cs", @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(""No MAHC calls from another method!"");
    }
}", @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(""No MAHC calls from another method!"");
    }
}"),
        };

        var allProblems = new List<Diagnostic>();

        foreach (var (filename, source, _) in files)
        {
            var semanticModel = GetSemanticModelFromSourceCode(filename, source);
            _mahcRule.AnalyzeModel(semanticModel, out var problems);

            allProblems.AddRange(problems);
        }

        var changedFiles = new DiagnosticPragmaIgnoreAdder().AddPragmasToCode(allProblems);
        Assert.AreEqual(0, changedFiles.Count);
    }

    [TestMethod]
    public void MAHC_MultipleFile_OneOutOfTwoCalls()
    {
        var files = new (string filename, string source, string expected)[]
        {
            ("Program.cs", @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        var service = Itc.Commons.Model.ModelApplicationHostController.Instance.Get<Itc.Commons.Model.ITenantValidator>();
    }
}", @$"using System;

public class Program
{{
    public static void Main(string[] args)
    {{
#pragma warning disable {_mahcRule.DiagnosticDescriptor.Id}
        var service = Itc.Commons.Model.ModelApplicationHostController.Instance.Get<Itc.Commons.Model.ITenantValidator>();
#pragma warning restore {_mahcRule.DiagnosticDescriptor.Id}
    }}
}}"),
            ("MyClass.cs", @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(""No MAHC calls from another method!"");
    }
}", @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(""No MAHC calls from another method!"");
    }
}"),
        };

        var allProblems = new List<Diagnostic>();

        foreach (var (filename, source, _) in files)
        {
            var semanticModel = GetSemanticModelFromSourceCode(filename, source);
            _mahcRule.AnalyzeModel(semanticModel, out var problems);

            allProblems.AddRange(problems);
        }

        var changedFiles = new DiagnosticPragmaIgnoreAdder().AddPragmasToCode(allProblems);
        Assert.AreEqual(1, changedFiles.Count);
        Assert.AreEqual(files[0].expected, changedFiles["Program.cs"]);
    }

    [TestMethod]
    public void MAHC_MultipleFile_AllFilesCall()
    {
        var files = new (string filename, string source, string expected)[]
        {
            ("Program.cs", @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        var service = Itc.Commons.Model.ModelApplicationHostController.Instance.Get<Itc.Commons.Model.ITenantValidator>();
    }
}", @$"using System;

public class Program
{{
    public static void Main(string[] args)
    {{
#pragma warning disable {_mahcRule.DiagnosticDescriptor.Id}
        var service = Itc.Commons.Model.ModelApplicationHostController.Instance.Get<Itc.Commons.Model.ITenantValidator>();
#pragma warning restore {_mahcRule.DiagnosticDescriptor.Id}
    }}
}}"),
            ("MyClass.cs", @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        var service = Itc.Commons.Model.ModelApplicationHostController.Instance.Get<Itc.Commons.Model.ITenantValidator>();
    }
}", @$"using System;

public class Program
{{
    public static void Main(string[] args)
    {{
#pragma warning disable {_mahcRule.DiagnosticDescriptor.Id}
        var service = Itc.Commons.Model.ModelApplicationHostController.Instance.Get<Itc.Commons.Model.ITenantValidator>();
#pragma warning restore {_mahcRule.DiagnosticDescriptor.Id}
    }}
}}"),
        };

        var allProblems = new List<Diagnostic>();

        foreach (var (filename, source, _) in files)
        {
            var semanticModel = GetSemanticModelFromSourceCode(filename, source);
            _mahcRule.AnalyzeModel(semanticModel, out var problems);

            allProblems.AddRange(problems);
        }

        var changedFiles = new DiagnosticPragmaIgnoreAdder().AddPragmasToCode(allProblems);
        Assert.AreEqual(files.Length, changedFiles.Count);
        Assert.AreEqual(files[0].expected, changedFiles["Program.cs"]);
        Assert.AreEqual(files[1].expected, changedFiles["MyClass.cs"]);
    }

    #endregion

    #region NOMC Tests

    [TestMethod]
    public void NOMC_NoCalls_DoesntChangeSourceCode()
    {
        const string givenSource = $@"{NomcCodeUsings}

bool entryPoint = false; // Program does not contain a static 'Main' method suitable for an entry point

public class TstModelPart : Module
{{
    protected override void RegisterDependencies(ModuleDependenciesConfiguration configuration)
    {{
        // No NOMC.Register calls!
    }}
}}

{NamedObjectsDefinition}";
        var semanticModel = GetSemanticModelFromSourceCode("Program.cs", givenSource);
        _nomcRule.AnalyzeModel(semanticModel, out var problems);

        var changedFiles = new DiagnosticPragmaIgnoreAdder().AddPragmasToCode(problems);
        Assert.AreEqual(0, changedFiles.Count);
    }

    [TestMethod]
    public void NOMC_OneSingleLineCall_WrapsInPragma()
    {
        const string givenSource = $@"{NomcCodeUsings}

bool entryPoint = false; // Program does not contain a static 'Main' method suitable for an entry point

public class TstModelPart : Module
{{
    protected override void RegisterDependencies(ModuleDependenciesConfiguration configuration)
    {{
        Get<INamedObjectModelConfiguration>().Register<TestNamedObject, TestNamedObjectComponent>();
    }}
}}

{NamedObjectsDefinition}";
        string expectedSource = $@"{NomcCodeUsings}

bool entryPoint = false; // Program does not contain a static 'Main' method suitable for an entry point

public class TstModelPart : Module
{{
    protected override void RegisterDependencies(ModuleDependenciesConfiguration configuration)
    {{
#pragma warning disable {_nomcRule.DiagnosticDescriptor.Id}
        Get<INamedObjectModelConfiguration>().Register<TestNamedObject, TestNamedObjectComponent>();
#pragma warning restore {_nomcRule.DiagnosticDescriptor.Id}
    }}
}}

{NamedObjectsDefinition}";

        const string filename = "Program.cs";

        var semanticModel = GetSemanticModelFromSourceCode(filename, givenSource);
        _nomcRule.AnalyzeModel(semanticModel, out var problems);

        var changedFiles = new DiagnosticPragmaIgnoreAdder().AddPragmasToCode(problems);
        Assert.AreEqual(1, changedFiles.Count);
        Assert.AreEqual(expectedSource, changedFiles[filename]);
    }

    #endregion
}