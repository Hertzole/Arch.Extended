namespace Arch.AOT.SourceGenerator;

/// <summary>
///     The struct <see cref="ComponentType" />
///     represents an Component (Their type with meta data) for use in the generated code.
/// </summary>
public readonly struct ComponentType : IEquatable<ComponentType>
{
	/// <summary>
	///     The type name of the component.
	/// </summary>
	public string TypeName { get; }
	/// <summary>
	///     If the component has zero fields.
	/// </summary>
	public bool IsZeroSize { get; }
	/// <summary>
	///     If the component is a value type.
	/// </summary>
	public bool IsValueType { get; }

	/// <summary>
	///     Creates a new instance of the <see cref="ComponentType" />.
	/// </summary>
	/// <param name="typeName">The type name.</param>
	/// <param name="isZeroSize">If its zero sized.</param>
	/// <param name="isValueType">If its a value type.</param>
	public ComponentType(string typeName, bool isZeroSize, bool isValueType)
	{
		TypeName = typeName;
		IsZeroSize = isZeroSize;
		IsValueType = isValueType;
	}

	public bool Equals(ComponentType other)
	{
		return IsZeroSize == other.IsZeroSize && IsValueType == other.IsValueType && string.Equals(TypeName, other.TypeName, StringComparison.Ordinal);
	}

	public override bool Equals(object? obj)
	{
		return obj is ComponentType other && Equals(other);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			int hashCode = TypeName.GetHashCode();
			hashCode = (hashCode * 397) ^ IsZeroSize.GetHashCode();
			hashCode = (hashCode * 397) ^ IsValueType.GetHashCode();
			return hashCode;
		}
	}

	public static bool operator ==(ComponentType left, ComponentType right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ComponentType left, ComponentType right)
	{
		return !left.Equals(right);
	}
}