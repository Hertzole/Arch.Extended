using Microsoft.CodeAnalysis;

namespace Arch.AOT.SourceGenerator.Extensions;

public static class SyntaxExtensions
{
	public static bool HasParentOfType<T>(this SyntaxNode node, out T? parentNode) where T : SyntaxNode
	{
		SyntaxNode? parent = node.Parent;
		while (parent != null)
		{
			if (parent is T tNode)
			{
				parentNode = tNode;
				return true;
			}

			parent = parent.Parent;
		}

		parentNode = null;
		return false;
	}
}