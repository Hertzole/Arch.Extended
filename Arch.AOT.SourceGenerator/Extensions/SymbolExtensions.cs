using Microsoft.CodeAnalysis;

namespace Arch.AOT.SourceGenerator.Extensions;

public static class SymbolExtensions
{
	public static bool IsSymbol(this ITypeSymbol symbol, string symbolName)
	{
		return string.Equals(symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), symbolName, StringComparison.Ordinal);
	}

	public static bool IsMethod(this IMethodSymbol symbol, string methodName)
	{
		return string.Equals(symbol.Name, methodName, StringComparison.Ordinal);
	}

	public static bool HasZeroFields(this ITypeSymbol symbol)
	{
		var members = symbol.GetMembers();

		// No members means no fields.
		if (members.Length == 0)
		{
			return true;
		}
		
		foreach (ISymbol member in members)
		{
			if (member is not IFieldSymbol fieldSymbol)
			{
				continue;
			}
			
			// If the field is not static, it is not a zero field.
			if (!fieldSymbol.IsStatic)
			{
				return false;
			}
		}
		
		return true;
	}
}