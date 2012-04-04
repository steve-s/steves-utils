using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ApiComparator
{
	public static class Program
	{
		private static ModuleDefinition[] modules;

		private static List<string> arguments;

		private static bool allOption = false;

		private static void Main(string[] args)
		{
			arguments = new List<string>(args);
			if (arguments.Any() && arguments[0].Trim() == "-all") {
				arguments.RemoveAt(0);
				allOption = true;
			}

			if (arguments.Count == 0) {
				Console.WriteLine("Not enough arguments. \n Usage: ");
				Console.WriteLine("To Compare two APIs: apicmp [-all] assembly1 assembly2");
				Console.WriteLine("To look for NotImplementedException methods: [-all] apicmp assembly1");
				Console.WriteLine("When -all option is NOT used, only public API is compared");
				return;
			}

			if (arguments.Count == 1) {
				FindNotImplemented();
			} else {
				CompareApi();
			}
		}

		private static void CompareApi()
		{
			modules = new ModuleDefinition[2];
			modules[0] = AssemblyDefinition.ReadAssembly(arguments[0]).MainModule;
			modules[1] = AssemblyDefinition.ReadAssembly(arguments[1]).MainModule;

			var typeComparer = new LambdaComparer<TypeDefinition>(x => x.FullName);
			var typesDiff = GetTypes(0).Union(GetTypes(1), typeComparer)
				.Except(GetTypes(0).Intersect(GetTypes(1), typeComparer));

			Console.WriteLine("-----------");
			if (typesDiff.Any()) {
				Console.WriteLine("Differences in types: ");
				foreach (var type in typesDiff) {
					Console.WriteLine("Type {0}, defined in {1}.", type.FullName, GetFileName(type.Module));
				}
			} else {
				Console.WriteLine("No differences in types found.");
			}

			var methodComparer = new LambdaComparer<MethodDefinition>(m => 
			                                                          Tuple.Create(
			                                                          	m.Name, 
			                                                          	m.Parameters.Count, 
			                                                          	m.ReturnType.FullName, 
			                                                          	string.Join(";", m.Parameters.Select(p => p.ParameterType.FullName))));
			var methodsDiff = GetTypes(0)
				.Union(GetTypes(1))
				.GroupBy(t => t.FullName)
				.Where(g => g.Count() >= 2)	// where the types are in both assemblies
				.SelectMany(g =>
				            	{
				            		var methods0 = g.First().Methods.Where(FilterMethod);
				            		var methods1 = g.ElementAt(1).Methods.Where(FilterMethod);
				            		return methods0
				            			.Union(methods1, methodComparer)
				            			.Except(methods0.Intersect(methods1, methodComparer));
				            	});

			Console.WriteLine("-----------");
			if (methodsDiff.Any()) {
				Console.WriteLine("Differences in methods: ");
				foreach (var method in methodsDiff) {
					Console.WriteLine(
						"Method {0} in type {1}, defined in {2}.", 
						method.Name, 
						method.DeclaringType.FullName,
						GetFileName(method.Module));
				}
			}
			else {
				Console.WriteLine("No differences in methods found.");
			}
		}

		private static void FindNotImplemented()
		{
			modules = new ModuleDefinition[1];
			modules[0] = AssemblyDefinition.ReadAssembly(arguments[0]).MainModule;

			var methods = GetTypes(0)
				.SelectMany(x => x.Methods)
				.Where(FilterMethod)
				.Where(OnlyThrowsNotImplemented);

			if (methods.Any()) {
				foreach (var method in methods) {
					Console.WriteLine("{0}.{1}", method.DeclaringType.FullName, method.Name);
				}
			} else {
				Console.WriteLine("No methods instantiating NotImplementedException were found");
			}
		}

		private static bool OnlyThrowsNotImplemented(MethodDefinition m)
		{
			if (!m.HasBody)
				return false;

			foreach (var instr in m.Body.Instructions) {
				if (instr.OpCode == OpCodes.Newobj &&
					((MethodReference)instr.Operand).DeclaringType.FullName == typeof(NotImplementedException).FullName) {
					return true;
				}
			}

			return false;
		}

		private static IEnumerable<TypeDefinition> GetTypes(int moduleIdx)
		{
			return modules[moduleIdx].Types.Where(x => x.IsPublic || allOption);
		}

		private static bool FilterMethod(MethodDefinition m)
		{
			return m.IsPublic || m.IsFamily || allOption;
		}

		private static string GetFileName(ModuleDefinition module)
		{
			if (module == modules[0]) {
				return arguments[0];
			} else {
				return arguments[1];
			}
		}

		private class LambdaComparer<T> : IEqualityComparer<T> {
			private readonly Func<T, object> propertySelector;

			public LambdaComparer(Func<T, object> propertySelector) {
				this.propertySelector = propertySelector;
			}

			public bool Equals(T x, T y) {
				return this.propertySelector.Invoke(x).Equals(this.propertySelector.Invoke(y));
			}

			public int GetHashCode(T obj) {
				return this.propertySelector.Invoke(obj).GetHashCode();
			}
		}
	}
}
