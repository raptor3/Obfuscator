using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

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

			//try
			{
				Console.Write("Loading project...");
				//Obfuscator obfuscator = new Obfuscator(args[0]);


				var resolver = new DefaultAssemblyResolver();
				resolver.AddSearchDirectory(Path.GetDirectoryName(args[0]));
				resolver.AddSearchDirectory(Path.GetDirectoryName(args[1]));
				AssemblyDefinition myAssembly = AssemblyDefinition.ReadAssembly(args[0], new ReaderParameters
				{
					ReadingMode = Mono.Cecil.ReadingMode.Immediate,
					ReadSymbols = false,
					AssemblyResolver = resolver
				});
				AssemblyDefinition myAssembly1 = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters
				{
					ReadingMode = Mono.Cecil.ReadingMode.Immediate,
					ReadSymbols = false,
					AssemblyResolver = resolver
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
			/*catch (Exception e)
			{
				Console.WriteLine();
				Console.Error.WriteLine("An error occurred during processing:");
				Console.Error.WriteLine(e.Message);
				if (e.InnerException != null)
					Console.Error.WriteLine(e.InnerException.Message);
				return 1;
			}*/

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
			foreach (var assembly in assemblies)
			{
				foreach (var type in assembly.MainModule.GetTypes())
				{
					if (type.FullName != "<Module>")
					{
						RegisterNamespace(type);
						RegisterType(type);

						foreach (var method in type.Methods)
						{
							foreach (var inter in type.Interfaces)
							{
								var s = MetadataResolver.GetMethod(inter.Resolve().Methods, method);
								Console.WriteLine(s);
							}
							
							//MethodDefinitionRocks.GetMatching
							if (!method.IsGetter && !method.IsConstructor && !method.IsSetter)

								if (method.IsVirtual && !method.IsNewSlot)
								{
									var baseM = GetBaseMethod(method);
									if (baseM != null )
									{
										RegisterWithBaseMethod(baseM, method);
									}
								}
								else
								{
									RegisterMethod(method);
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
										var method1 = methodReference.Resolve();
										if (!method1.IsGetter && !method1.IsConstructor && !method1.IsSetter)

											if (method1.IsVirtual && !method1.IsNewSlot)
											{
												var baseM = GetBaseMethod(method1);
												if (baseM != null)
												{
													RegisterWithBaseMethod(baseM, methodReference);
												}
											}
											else
											{
												RegisterMethod(methodReference);
											}
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

		private MethodDefinition GetBaseMethod(MethodDefinition method)
		{
			return method.GetOriginalBaseMethod();
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

		private void RegisterWithBaseMethod(MethodReference baseM, MethodReference method)
		{
			if (!assemblies.Contains(baseM.Module.Assembly))
			{
				return;
			}
			Action<string> action = delegate (string s) { method.Name = s; };
			IList<Action<string>> list;
			if (methods.TryGetValue(baseM.FullName, out list))
			{
				list.Add(action);
			}
			else
			{
				methods.Add(baseM.FullName, new List<Action<string>> { action });
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