using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Arch.AOT.SourceGenerator.Tests;

public class ComponentRegistryGeneratorUsageTests
{
	[Fact]
	public void FindUsageFromWorldCreate()
	{
		AssertGeneratedFile(TestClasses.WorldCreate, TestClasses.WorldCreateExpected);
	}

	private static void AssertGeneratedFile(string source, string expected)
	{
		// Run generators and retrieve all results.
		var runResult = CSharpGeneratorRunner.RunGenerator<ComponentRegistryGenerator>(source);

		// Assert that no diagnostics were produced.
		Assert.Empty(runResult.Diagnostics);
		
		// Retrieve the generated file syntax tree.
		SyntaxTree generatedFileSyntax = runResult.GeneratedTrees.Single(t => t.FilePath.EndsWith("GeneratedComponentRegistry.g.cs"));
		
		// Assert that the generated file has the expected content.
		Assert.Equal(expected, generatedFileSyntax.GetText().ToString(), ignoreLineEndingDifferences: true);
	}
}