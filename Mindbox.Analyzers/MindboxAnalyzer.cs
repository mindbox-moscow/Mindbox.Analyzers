using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using MindboxAnalyzers.Rules;

namespace MindboxAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class MindboxAnalyzer : DiagnosticAnalyzer
	{
		private static readonly List<IAnalyzerRule> rules;
		private static readonly ImmutableArray<DiagnosticDescriptor> supportedDiagnostics;
		
		static MindboxAnalyzer()
		{
			rules = new List<IAnalyzerRule>
			{
				new LineIsTooLongRule(),
				new OnlyTabsShouldBeUsedForIndentationRule(),
				new No3AdjacentEmptyLinesRule(),
				new NoRegionsInsideMethodRule(),
				new CustomUtcDateTimeConvertingRequiredRule(),
				new TabsShouldOnlyBeUsedForIndentationRule(),
				new NoAdjacentWhitespaceAllowedRule(),
				new NoMoreThanOneLineBetweenSimilarDeclarationsRule(),
				new MultiLineMembersShouldBeSeparatedByABlankLineRule(),
				new NameOfInArgumentExceptionsRequiredRule(),
				new NameOfInColumnAttributesRequiredRule(),
				new CustomizationsCrossReferenceProhibitedRule(),
				new CacheItemProviderKeyMustBeStaticRule(),
				new NoIntegrationTestsWithoutOwnerRule()
			};

			supportedDiagnostics =
				rules
					.Select(rule => rule.DiagnosticDescriptor)
					.ToImmutableArray();
		}


		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => supportedDiagnostics;

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
		}

		private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
		{
			var allDiagnostics = new List<Diagnostic>();

			var text = context.Tree.GetText(context.CancellationToken);
			foreach (var textAnalyzer in rules.OfType<ITextAnalyzerRule>())
			{
				textAnalyzer.AnalyzeText(context.Tree, text, out var foundProblems);
				if (foundProblems != null)
					allDiagnostics.AddRange(foundProblems);
			}

			var lineAnalyzers = rules
				.OfType<ISingleLineAnalyzerRule>()
				.ToList();

			foreach (var line in text.Lines)
			{
				var lineString = line.ToString();
				foreach (var lineAnalyzer in lineAnalyzers)
				{
					lineAnalyzer.AnalyzeLine(context.Tree, line, lineString, out var foundProblems);
					if (foundProblems != null)
						allDiagnostics.AddRange(foundProblems);
				}
			}

			foreach (var treeAnalyzer in rules.OfType<ITreeAnalyzerRule>())
			{
				treeAnalyzer.AnalyzeTree(context.Tree, out var foundProblems);
				if (foundProblems != null)
					allDiagnostics.AddRange(foundProblems);
			}

			foreach (var diagnostic in allDiagnostics)
				context.ReportDiagnostic(diagnostic);
		}
	}
}
