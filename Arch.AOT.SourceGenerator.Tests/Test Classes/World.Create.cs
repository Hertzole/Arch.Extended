namespace Arch.AOT.SourceGenerator.Tests;

public static class TestClasses
{
	public static readonly string WorldCreate = """
	                                            using Arch.Core;

	                                            namespace Test;

	                                            public class WorldCreate
	                                            {
	                                            	void Main()
	                                            	{
	                                            		var world = World.Create();
	                                            		world.Create(new TestComponent1(), new TestComponent2 { x = 1.0f }, new TestComponent3(1.0f));
	                                            	}
	                                            }

	                                            public struct TestComponent1 { }

	                                            public struct TestComponent2
	                                            {
	                                            	public float x;
	                                            }
	                                            
	                                            public struct TestComponent3
	                                            {
	                                            	public float x;
	                                            	
	                                            	public TestComponent3(float x)
	                                            	{
	                                            		this.x = x;
	                                            	}
	                                            }

	                                            public struct NotUsed { }
	                                            """;

	public static readonly string WorldCreateExpected = """
	                                                    using System.Runtime.CompilerServices;
	                                                    using Arch.Core.Utils;

	                                                    namespace Arch.AOT.SourceGenerator
	                                                    {
	                                                        internal static class GeneratedComponentRegistry
	                                                        {
	                                                            [ModuleInitializer]
	                                                            public static void Initialize()
	                                                            {
	                                                                ComponentRegistry.Add(new ComponentType(ComponentRegistry.Size + 1, typeof(global::Test.TestComponent1), Unsafe.SizeOf<global::Test.TestComponent1>(), true));
	                                                                ArrayRegistry.Add<global::Test.TestComponent1>();
	                                                                ComponentRegistry.Add(new ComponentType(ComponentRegistry.Size + 1, typeof(global::Test.TestComponent2), Unsafe.SizeOf<global::Test.TestComponent2>(), false));
	                                                                ArrayRegistry.Add<global::Test.TestComponent2>();
	                                                                ComponentRegistry.Add(new ComponentType(ComponentRegistry.Size + 1, typeof(global::Test.TestComponent3), Unsafe.SizeOf<global::Test.TestComponent3>(), false));
	                                                                ArrayRegistry.Add<global::Test.TestComponent3>();
	                                                            }
	                                                        }
	                                                    }
	                                                    """;
}