using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;
using MindboxAnalyzers.Rules;

namespace MindboxAnalyzers.Tests
{
	[TestClass]
	public class UnitTest : CodeFixVerifier
	{
		[TestMethod]
		public void NormalLineLength()
		{
			var test = @"class Test {}";

			VerifyCSharpDiagnostic(test);
		}

		[TestMethod]
		public void TooLongLine()
		{
			var test = new string('Z', 131);

			var rule = new LineIsTooLongRule();
			var expected = new DiagnosticResult
			{
				Id = rule.DiagnosticDescriptor.Id,
				Message = rule.DiagnosticDescriptor.MessageFormat.ToString(),
				Severity = DiagnosticSeverity.Warning,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 1, 1)
				}
			};

			VerifyCSharpDiagnostic(test, expected);
		}

		[TestMethod]
		public void CacheItemProviderKeyFieldIsNotStatic()
		{
			var test = @"class Test {CacheItemProviderKey prop;}";

			var rule = new CacheItemProviderKeyMustBeStaticRule();
			var expected = new DiagnosticResult
			{
				Id = rule.DiagnosticDescriptor.Id,
				Message = rule.DiagnosticDescriptor.MessageFormat.ToString(),
				Severity = DiagnosticSeverity.Warning,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 1, 13)
				}
			};
			
			VerifyCSharpDiagnostic(test, expected);
		}

		[TestMethod]
		public void CacheItemProviderKeyPropertyIsNotStatic()
		{
			var test = @"class Test {CacheItemProviderKey prop => null;}";

			var rule = new CacheItemProviderKeyMustBeStaticRule();
			var expected = new DiagnosticResult
			{
				Id = rule.DiagnosticDescriptor.Id,
				Message = rule.DiagnosticDescriptor.MessageFormat.ToString(),
				Severity = DiagnosticSeverity.Warning,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 1, 13)
				}
			};
			
			VerifyCSharpDiagnostic(test, expected);
		}

		[TestMethod]
		public void CacheItemProviderKeyFieldAndPropertyIsStatic()
		{
			var test = @"class Test {static CacheItemProviderKey prop => null;static CacheItemProviderKey prop1;}";

			VerifyCSharpDiagnostic(test, Array.Empty<DiagnosticResult>());
		}

		[TestMethod]
		public void NotCacheItemProviderKeyFieldAndProperty()
		{
			var test = @"class Test {ItemProviderKey prop => null;ItemProviderKey prop1;}";

			VerifyCSharpDiagnostic(test, Array.Empty<DiagnosticResult>());
		}

		[TestMethod]
		public void NoIntegrationTestsWithoutOwnerRule()
		{
			var test = 
				@"class IntegrationTestBase{}

			    class IntegrationTest1:IntegrationTestBase
			    {
			    	[TestMethodAttribute]
			    	[OwnerAttribute(111)]
			    	public void TestMethod(){}

					[TestMethodAttribute]
					public void TestMethod2(){}
			    }

			    class NonIntegrationTest
				{
					public void NonIntegrationTestMethod(){}
				}";
			
			var rule = new NoIntegrationTestsWithoutOwnerRule();
			var expected = new DiagnosticResult
			{
				Id = rule.DiagnosticDescriptor.Id,
				Message = rule.DiagnosticDescriptor.MessageFormat.ToString(),
				Severity = DiagnosticSeverity.Hidden,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 8, 1)
				}
			};
			
			VerifyCSharpDiagnostic(test, expected);
		}
		
		/*
		[TestMethod]
		public void TabsFormattedBySpaces()
		{
			var test = @"      var nikita = 1;";
			var expected = new DiagnosticResult
			{
				Id = MindboxAnalyzer.OnlyTabsShouldBeUsedForIndentationRuleId,
				Message = "Для отступа используются пробелы",
				Severity = DiagnosticSeverity.Warning,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 1, 1)
				}
			};

			VerifyCSharpDiagnostic(test, expected);
		}

		[TestMethod]
		public void RegionInsideAMethod()
		{
			var test = @"public void Main() { #region var a = 5; #endregion var b = 3; return; }";
			var expected = new DiagnosticResult
			{
				Id = MindboxAnalyzer.NoRegionsInsideMethodsRuleId,
				Message = "Использование региона внутри метода",
				Severity = DiagnosticSeverity.Warning,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 1, 22)
				}
			};

			VerifyCSharpDiagnostic(test, expected);
		}
		*/
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new MindboxAnalyzer();
		}
		
	}
}