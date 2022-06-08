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
		
		/*
		[TestMethod]
		public void TabsFormattedBySpaces()
		{
			var test = @"      var nikita = 1;";
			var expected = new DiagnosticResult
			{
				Id = MindboxAnalyzer.OnlyTabsShouldBeUsedForIndentationRuleId,
				Message = "Indentation with spaces is prohibited",
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
				Message = "Regions shouldn't be used inside a method",
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