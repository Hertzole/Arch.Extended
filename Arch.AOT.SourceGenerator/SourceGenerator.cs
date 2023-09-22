using System.Collections.Immutable;
using System.Text;
using Arch.AOT.SourceGenerator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Arch.AOT.SourceGenerator;

/// <summary>
///     Incremental generator that generates a class that adds all components to the ComponentRegistry.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ComponentRegistryGenerator : IIncrementalGenerator
{
	private static readonly string[] _archQueryDescriptionMethods = { "WithAny", "WithAll", "WithNone", "WithExclusive" };
	/// <summary>
	///     The attribute to mark components with in order to be found by this source-gen.
	/// </summary>
	private const string AttributeTemplate = """
	                                         using System;

	                                         namespace Arch.AOT.SourceGenerator
	                                         {
	                                             [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	                                             public sealed class ComponentAttribute : Attribute { }
	                                         }
	                                         """;

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		Log.LogInfo("INITIALIZING COMPONENT REGISTRY GENERATOR");

		// Register the attribute.
		context.RegisterPostInitializationOutput(initializationContext =>
		{
			initializationContext.AddSource("Components.Attributes.g.cs", SourceText.From(AttributeTemplate, Encoding.UTF8));
		});

		IncrementalValuesProvider<ImmutableArray<ComponentType>> provider = context.SyntaxProvider.CreateSyntaxProvider(
			ShouldTypeBeRegistered,
			GetMemberDeclarationsForSourceGen).Where(t => t.foundTypes).Select((t, _) => t.Item1
		);

		context.RegisterSourceOutput(
			context.CompilationProvider.Combine(provider.Collect()), (productionContext, tuple) =>
			{
				ImmutableArray<ComponentType>.Builder builder = ImmutableArray.CreateBuilder<ComponentType>();

				foreach (ImmutableArray<ComponentType> array in tuple.Right)
				{
					foreach (ComponentType type in array)
					{
						if (!builder.Contains(type))
						{
							builder.Add(type);
						}
					}
				}

				ImmutableArray<ComponentType> result = builder.ToImmutable();
				Log.LogInfo($"Should generate code {result.Length}");

				GenerateCode(productionContext, result);
			});
	}

	/// <summary>
	///     Determines if a node should be considered for code generation.
	/// </summary>
	/// <param name="node"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	private static bool ShouldTypeBeRegistered(SyntaxNode node, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (node is InvocationExpressionSyntax invocationExpressionSyntax)
		{
			string methodCall = invocationExpressionSyntax.Expression.ToString();
            
			if (methodCall.Contains("Create") || methodCall.Contains("With") || methodCall.Contains("Query"))
			{
				return true;
			}
		}

		// if (node is ObjectCreationExpressionSyntax creationExpressionSyntax)
		// {
		// 	if (string.Equals(creationExpressionSyntax.Type.ToString(), "QueryDescription", StringComparison.Ordinal))
		// 	{
		// 		return true;
		// 	}
		// }

		return false;
	}

	/// <summary>
	///     Make sure the type is annotated with the Component attribute.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	private static (ImmutableArray<ComponentType>, bool foundTypes) GetMemberDeclarationsForSourceGen(GeneratorSyntaxContext context,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (context.Node is ExpressionSyntax invocationExpressionSyntax)
		{
			if (TryGetTypesFromInvocationExpression(invocationExpressionSyntax, ref context, cancellationToken, out ImmutableArray<ComponentType> types))
			{
				return (types, true);
			}
		}

		if (context.Node is ObjectCreationExpressionSyntax creationExpressionSyntax)
		{
			if (TryGetTypesFromCreationExpression(creationExpressionSyntax, ref context, cancellationToken, out ImmutableArray<ComponentType> types))
			{
				return (types, true);
			}
		}

		// No usage found, return false.
		return (ImmutableArray<ComponentType>.Empty, false);
	}

	private static bool TryGetTypesFromInvocationExpression(ExpressionSyntax invocationExpressionSyntax,
		ref GeneratorSyntaxContext context,
		in CancellationToken cancellationToken,
		out ImmutableArray<ComponentType> types)
	{
		types = ImmutableArray<ComponentType>.Empty;
		SymbolInfo symbol = context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax, cancellationToken);
		if (symbol.Symbol is IMethodSymbol methodSymbol)
		{
			Log.LogInfo($"METHOD SYMBOL FOUND {methodSymbol.Name} | {methodSymbol.ContainingType} | {methodSymbol.TypeArguments.Length}");
			
			if(methodSymbol.TypeArguments.Length == 0) return false;
            
			if (methodSymbol.ContainingType.IsSymbol("global::Arch.Core.World"))
			{
				if (methodSymbol.IsMethod("Create"))
				{
					goto GetTypes;
				}
				
				if(methodSymbol.Name.Contains("Query"))
				{
					goto GetTypes;
				}
			}
			else if (methodSymbol.ContainingType.IsSymbol("global::Arch.Core.QueryDescription") && IsQueryDescriptionMethod(methodSymbol.Name))
			{
				goto GetTypes;
			}
			
			goto EmptyTypes;

			GetTypes:
			foreach (ITypeSymbol typeArgument in methodSymbol.TypeArguments)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var interfaces = typeArgument.AllInterfaces;
				if (interfaces.Length > 0)
				{
					bool hasForEach = false;
					
					foreach (INamedTypeSymbol @interface in interfaces)
					{
						if (@interface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).StartsWith("global::Arch.Core.IForEach", StringComparison.Ordinal))
						{
							Log.LogInfo($"FOUND IFOREACH {typeArgument.Name}");
							hasForEach = true;
						}
					}

					if (hasForEach)
					{
						continue;
					}
				}

				types = types.Add(new ComponentType(typeArgument.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), typeArgument.HasZeroFields(),
					typeArgument.IsValueType));
			}
		}

		EmptyTypes:
		return types.Length > 0;
	}

	private static bool TryGetTypesFromCreationExpression(ObjectCreationExpressionSyntax invocationExpressionSyntax,
		ref GeneratorSyntaxContext context,
		in CancellationToken cancellationToken,
		out ImmutableArray<ComponentType> types)
	{
		return false;
	}

	private static bool IsQueryDescriptionMethod(in string methodName)
	{
		for (int i = 0; i < _archQueryDescriptionMethods.Length; i++)
		{
			if (string.Equals(methodName, _archQueryDescriptionMethods[i], StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	private static void GenerateCode(SourceProductionContext productionContext, ImmutableArray<ComponentType> typeList)
	{
		Log.LogInfo($"GENERATING COMPONENT REGISTRY FOR {typeList.Length} COMPONENTS");

		if (typeList.IsDefaultOrEmpty)
		{
			return;
		}

		StringBuilder sb = new StringBuilder();
		sb.AppendComponentTypes(typeList);
		productionContext.AddSource("GeneratedComponentRegistry.g.cs",
			CSharpSyntaxTree.ParseText(sb.ToString()).GetRoot().NormalizeWhitespace().ToFullString());

		// var sb = new StringBuilder();
		// _componentTypes.Clear();
		//
		// foreach (var type in typeList)
		// {
		// 	// Get the symbol for the type.
		// 	var symbol = ModelExtensions.GetDeclaredSymbol(compilation.GetSemanticModel(type.SyntaxTree), type);
		//
		// 	// If the symbol is not a type symbol, we can't do anything with it.
		// 	if (symbol is not ITypeSymbol typeSymbol)
		// 	{
		// 		continue;
		// 	}
		//
		// 	// Check if there are any fields in the type.
		// 	var hasZeroFields = true;
		// 	foreach (var member in typeSymbol.GetMembers())
		// 	{
		// 		if (member is not IFieldSymbol) continue;
		// 		
		// 		hasZeroFields = false;
		// 		break;
		// 	}
		//
		// 	var typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		// 	_componentTypes.Add(new ComponentType(typeName, hasZeroFields, typeSymbol.IsValueType));
		// }
		//
		// sb.AppendComponentTypes(_componentTypes);
		// productionContext.AddSource("GeneratedComponentRegistry.g.cs",CSharpSyntaxTree.ParseText(sb.ToString()).GetRoot().NormalizeWhitespace().ToFullString());
	}
}