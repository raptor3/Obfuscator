using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Mono.Cecil;

namespace Obfuscator
{
	public class Program
	{
		private static void ShowHelp()
		{
			Console.WriteLine("Usage:  obfuscar [projectfile]");
		}

		private static int Main(string[] args)
		{
			Console.WriteLine();

			if (args.Length != 2)
			{
				ShowHelp();
				return 1;
			}

			int start = Environment.TickCount;

			try
			{
				Console.Write("Loading project...");
				//Obfuscator obfuscator = new Obfuscator(args[0]);

				AssemblyDefinition myAssembly = AssemblyDefinition.ReadAssembly(args[0], new ReaderParameters
				{
					ReadingMode = Mono.Cecil.ReadingMode.Immediate,
					ReadSymbols = false,
					AssemblyResolver = new DefaultAssemblyResolver()
				});
				AssemblyDefinition myAssembly1 = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters
				{
					ReadingMode = Mono.Cecil.ReadingMode.Immediate,
					ReadSymbols = false,
					AssemblyResolver = new DefaultAssemblyResolver()
				});

				Obfuscator o = new Obfuscator();
				o.Load(myAssembly);
				o.Load(myAssembly1);
				o.Resolve();
				o.RunRules();
				
				string outName = Path.Combine("out", Path.GetFileName(args[0]));
				myAssembly.Write(outName);
				outName = Path.Combine("out", Path.GetFileName(args[1]));
				myAssembly1.Write(outName);
				Console.WriteLine("Done.");

				//obfuscator.RunRules();

				Console.WriteLine("Completed, {0:f2} secs.", (Environment.TickCount - start) / 1000.0);
			}
			catch (Exception e)
			{
				Console.WriteLine();
				Console.Error.WriteLine("An error occurred during processing:");
				Console.Error.WriteLine(e.Message);
				if (e.InnerException != null)
					Console.Error.WriteLine(e.InnerException.Message);
				return 1;
			}

			return 0;
		}
	}


	public class Obfuscator
	{
		List<AssemblyDefinition> assemblies = new List<AssemblyDefinition>();
		Dictionary<string, IList<Action<string>>> namespaces = new Dictionary<string, IList<Action<string>>>();
		Dictionary<string, IList<Action<string>>> types = new Dictionary<string, IList<Action<string>>>();
		Dictionary<string, IList<Action<string>>> methods = new Dictionary<string, IList<Action<string>>>();
		Dictionary<string, IList<Action<string>>> fields = new Dictionary<string, IList<Action<string>>>();
		Dictionary<string, IList<Action<string>>> properties = new Dictionary<string, IList<Action<string>>>();
		
		public Obfuscator()
		{
		}


		public void Load(AssemblyDefinition a)
		{
			assemblies.Add(a);
		}

		public void Resolve()
		{
			foreach (var assembly in assemblies) {
				foreach (var type in assembly.MainModule.GetTypes())
				{
					if (type.FullName != "<Module>")
					{
						RegisterNamespace(type);
						RegisterType(type);
						foreach (var method in type.Methods)
						{
							if (!method.IsGetter && !method.IsConstructor && !method.IsSetter)
								RegisterMethod(method);
							if (method.IsVirtual)
							{
								
							}
							if (method.HasBody)
							{ 
								foreach (var instruction in method.Body.Instructions)
								{
									var fieldReference = instruction.Operand as FieldReference;
									var typeReference = instruction.Operand as TypeReference;
									var propertyReference = instruction.Operand as PropertyReference;
									var methodReference = instruction.Operand as MethodReference;
									if (fieldReference != null)
									{
										RegisterField(fieldReference);
									}
									if (typeReference != null)
									{
										RegisterType(typeReference);
									}
									if (propertyReference != null)
									{
										RegisterProperty(propertyReference);
									}
									if (methodReference != null)
									{
										RegisterMethod(methodReference);
									}
								}
							}
						}
						foreach (var field in type.Fields)
						{
							RegisterField(field);
						}
						foreach (var proper in type.Properties)
						{
							RegisterProperty(proper);
						}
					}
				}
				

				foreach (var type in assembly.MainModule.GetTypeReferences())
				{
					
					if (assemblies.Any(a=> a.Name.FullName == ((AssemblyNameReference) type.Scope).FullName))
					{
						RegisterNamespace(type);
						RegisterType(type);
					}
				}
			}
		}

		private void RegisterNamespace(TypeReference type)
		{
			Action<string> action = delegate (string s) { type.Namespace = s; };
			IList<Action<string>> list;
			if (namespaces.TryGetValue(type.Namespace, out list))
			{
				list.Add(action);
			}
			else
			{
				namespaces.Add(type.Namespace, new List<Action<string>> { action });
			}
		}

		private void RegisterProperty(PropertyReference proper)
		{
			Action<string> action = delegate (string s) { proper.Name = s; };
			IList<Action<string>> list;
			if (properties.TryGetValue(proper.FullName, out list))
			{
				list.Add(action);
			}
			else
			{
				properties.Add(proper.FullName, new List<Action<string>> { action });
			}
		}

		private void RegisterField(FieldReference field)
		{
			Action<string> action = delegate (string s) { field.Name = s; };
			IList<Action<string>> list;
			if (fields.TryGetValue(field.FullName, out list))
			{
				list.Add(action);
			}
			else
			{
				fields.Add(field.FullName, new List<Action<string>> { action });
			}
		}

		private void RegisterMethod(MethodReference method)
		{

			Action<string> action = delegate (string s) { method.Name = s; };
			IList<Action<string>> list;
			if (methods.TryGetValue(method.FullName, out list))
			{
				list.Add(action);
			}
			else
			{
				methods.Add(method.FullName, new List<Action<string>> { action });
			}
		}

		public void RunRules()
		{
			char a = 'a';
			
			foreach (var typeLocations in namespaces.Values)
			{
				foreach (var typeLoc in typeLocations)
				{
					typeLoc.Invoke(a.ToString());
				}
				a++;
			}
			a = 'a';
			foreach (var typeLocations in types.Values)
			{
				foreach (var typeLoc in typeLocations)
				{
					typeLoc.Invoke(a.ToString());
				}
				a++;
			}
			a = 'a';
			foreach (var typeLocations in methods.Values)
			{
				foreach (var typeLoc in typeLocations)
				{
					typeLoc.Invoke(a.ToString());
				}
				a++;
			}
			a = 'a';
			foreach (var typeLocations in fields.Values)
			{
				foreach (var typeLoc in typeLocations)
				{
					typeLoc.Invoke(a.ToString());
				}
				a++;
			}
			a = 'a';
			foreach (var typeLocations in properties.Values)
			{
				foreach (var typeLoc in typeLocations)
				{
					typeLoc.Invoke(a.ToString());
				}
				a++;
			}
			
		}

		private void RegisterType(TypeReference type)
		{
			if (!assemblies.Contains(type.Module.Assembly))
			{
				return;
			}
			Action<string> action = delegate (string s) { type.Name = s; };
			IList<Action<string>> list;
			if (types.TryGetValue(type.FullName, out list))
			{
				list.Add(action);
			}
			else
			{
				types.Add(type.FullName, new List<Action<string>> { action });
			}
		}
	}

}