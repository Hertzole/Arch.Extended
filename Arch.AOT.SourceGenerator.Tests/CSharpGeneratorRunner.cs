using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Arch.Core;
using CommunityToolkit.HighPerformance;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Arch.AOT.SourceGenerator.Tests;

public static class CSharpGeneratorRunner
{
	private static Compilation baseCompilation = default!;

	[ModuleInitializer]
	public static void InitializeCompilation()
	{
		// running .NET Core system assemblies dir path
		string baseAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
		IEnumerable<string> systemAssemblies = Directory.GetFiles(baseAssemblyPath)
		                                                .Where(x =>
		                                                {
			                                                string fileName = Path.GetFileName(x);
			                                                if (fileName.EndsWith("Native.dll"))
			                                                {
				                                                return false;
			                                                }

			                                                return fileName.StartsWith("System") || fileName is "mscorlib.dll" or "netstandard.dll";
		                                                });

		PortableExecutableReference[] references = systemAssemblies
		                                           // .Append(typeof(QueryAttribute).Assembly.Location)
		                                           .Append(typeof(World).Assembly.Location)
		                                           .Append(typeof(ArrayExtensions).Assembly.Location)
		                                           .Select(x => MetadataReference.CreateFromFile(x))
		                                           .ToArray();

		CSharpCompilation compilation = CSharpCompilation.Create("generatortest",
			references: references,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		baseCompilation = compilation;
	}

	public static GeneratorDriverRunResult RunGenerator<T>(string source) where T : IIncrementalGenerator, new()
	{
		CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new T());

		Compilation compilation = baseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(source));

		GeneratorDriver x = driver.RunGenerators(compilation);
		GeneratorDriverRunResult result = x.GetRunResult();

		return result;
	}
}