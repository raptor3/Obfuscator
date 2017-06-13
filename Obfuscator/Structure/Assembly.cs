using Mono.Cecil;
using Obfuscator.SkipRules;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Reflection;
using System;
using Mono.Cecil.Cil;

namespace Obfuscator.Structure
{
	public class Assembly
	{
		private Project project;
		private AssemblyDefinition assembly;
		private Dictionary<string, Namespace> namespaces = new Dictionary<string, Namespace>();
		[XmlIgnore]
		public HiderClass Hider { get; private set; }
		[XmlIgnore]
		public int EntryHashCode { get; private set; }
		private Method EntryPoint { get; set; }
		public string Name { get { return assembly?.FullName; } }
		[XmlIgnore]
		public List<Instruction> EntryHash { get; private set; }
		[XmlAttribute("file")]
		public string File { get; set; }

		public Assembly()
		{
			EntryHash = new List<Instruction>();
		}

		public Assembly(Project project, AssemblyDefinition assembly)
		{
			this.project = project;
			this.assembly = assembly;
			EntryHash = new List<Instruction>();
		}

		public TypeSystem TypeSystem
		{
			get { return assembly.MainModule.TypeSystem; }
		}

		public void LoadAssemblies(DefaultAssemblyResolver resolver, Project prj)
		{
			project = prj;
			resolver.AddSearchDirectory(Path.GetDirectoryName(File));

			assembly = AssemblyDefinition.ReadAssembly
			(
				File,
				new ReaderParameters
				{
					ReadingMode = ReadingMode.Immediate,
					ReadSymbols = false,
					AssemblyResolver = resolver
				}
			);
		}

		[XmlElement("SkipNamespace", typeof(SkipNamespace))]
		public List<SkipNamespace> OnlySkipNamespaces { get; set; }

		[XmlElement("SkipField", typeof(SkipField))]
		public List<SkipField> OnlySkipFields { get; set; }

		[XmlElement("SkipMethod", typeof(SkipMethod))]
		public List<SkipMethod> OnlySkipMethods { get; set; }

		[XmlElement("SkipProperty", typeof(SkipProperty))]
		public List<SkipProperty> OnlySkipProperties { get; set; }

		[XmlElement("SkipType", typeof(SkipType))]
		public List<SkipType> OnlySkipTypes { get; set; }

		[XmlIgnore]
		public List<ISkipNamespace> SkipNamespaces { get; set; }
		[XmlIgnore]
		public List<ISkipType> SkipTypes { get; set; }
		[XmlIgnore]
		public List<ISkipMethod> SkipMethods { get; set; }
		[XmlIgnore]
		public List<ISkipField> SkipFields { get; set; }
		[XmlIgnore]
		public List<ISkipProperty> SkipProperties { get; set; }

		public MethodReference Import(MethodInfo method)
		{
			return assembly.MainModule.Import(method);
		}

		public TypeReference Import(System.Type type)
		{
			return assembly.MainModule.Import(type);
		}

		public void Resolve()
		{
			SkipNamespaces = OnlySkipNamespaces?.Select(s => s as ISkipNamespace).ToList();
			SkipTypes = OnlySkipTypes?.Select(s => s as ISkipType).ToList();
			SkipFields = OnlySkipFields?.Select(s => s as ISkipField).ToList();
			SkipMethods = OnlySkipMethods?.Select(s => s as ISkipMethod).ToList();
			SkipProperties = OnlySkipProperties?.Select(s => s as ISkipProperty).ToList();

			SkipTypes?.AddRange(SkipNamespaces.Select(s => s as ISkipType));
			SkipFields?.AddRange(SkipTypes.Select(s => s as ISkipField));
			SkipMethods?.AddRange(SkipTypes.Select(s => s as ISkipMethod));
			SkipProperties?.AddRange(SkipTypes.Select(s => s as ISkipProperty));

			foreach (var type in assembly.MainModule.Types)
			{
				if (type.FullName == "<Module>")
				{
					continue;
				}

				if (!namespaces.TryGetValue(type.Namespace, out Namespace nmspace))
				{
					nmspace = new Namespace(project, this, type.Namespace);
					namespaces.Add(type.Namespace, nmspace);
				}

				nmspace.Resolve(type);
			}

			if (assembly.EntryPoint != null)
			{
				EntryPoint = GetMethod(assembly.EntryPoint);
			}
		}

		public bool HasType(TypeReference typeRef)
		{
			return assembly.FullName == typeRef.Resolve()?.Module.Assembly.FullName;
		}

		public bool HasField(FieldReference fieldRef)
		{
			return assembly.FullName == fieldRef.Resolve()?.Module.Assembly.FullName;
		}

		public void Save(string output)
		{
			Hider?.FinalizeHiderClass();
			assembly?.Write(Path.Combine(output, Path.GetFileName(File)));
		}

		public void DoubleSave(string output)
		{
			var s = System.Reflection.Assembly.Load(Path.GetFullPath(Path.Combine(output, Path.GetFileName(File)))).EntryPoint.GetMethodBody().GetILAsByteArray();
			var hash = System.Text.Encoding.UTF8.GetString(s, 0, s.Length).GetHashCode();
			foreach (var entryHash in EntryHash)
			{
				entryHash.Operand = hash;
			}
			assembly?.Write(Path.Combine(output, Path.GetFileName(File)));
		}

		public bool HasProperty(PropertyReference propRef)
		{
			return assembly.FullName == propRef.Resolve()?.Module.Assembly.FullName;
		}

		public bool HasMethod(MethodReference methRef)
		{
			return assembly.FullName == methRef.Resolve()?.Module.Assembly.FullName;
		}

		public string RunRules()
		{
			var nameIterator = project.NameIteratorFabric.GetIterator();

			var skippedNamespace = new StringBuilder("SkippedNamespaces : {");
			var renamedNamespace = new StringBuilder("RenamedNamespaces : {");
			skippedNamespace.AppendLine();
			renamedNamespace.AppendLine();

			foreach (var nmspace in namespaces.Values)
			{
				string nmsR = nmspace.RunRules();

				if (nmspace.ChangeName(nameIterator.Next()))
				{
					renamedNamespace.AppendLine(nmspace.Changes);
					renamedNamespace.AppendLine(nmsR);
				}
				else
				{
					skippedNamespace.AppendLine(nmspace.Changes);
					skippedNamespace.AppendLine(nmsR);
				}
			}

			skippedNamespace.AppendLine("}");
			renamedNamespace.AppendLine("}");

			var result = new StringBuilder();
			result.AppendLine(skippedNamespace.ToString());
			result.AppendLine(renamedNamespace.ToString());
			return result.ToString();
		}

		public void RegistrateReference(TypeReference typeRef)
		{
			GetOrAddNamespace(typeRef).RegisterReference(typeRef);
		}

		public void RegistrateReference(FieldReference fieldRef)
		{
			GetOrAddNamespace(fieldRef.DeclaringType).RegisterReference(fieldRef);
		}

		public void RegistrateReference(PropertyReference propRef)
		{
			GetOrAddNamespace(propRef.DeclaringType).RegisterReference(propRef);
		}

		public void RegistrateReference(MethodReference methRef)
		{
			GetOrAddNamespace(methRef.DeclaringType).RegisterReference(methRef);
		}

		public Method GetMethod(MethodReference methRef)
		{
			if (!namespaces.TryGetValue(methRef.DeclaringType.Namespace, out Namespace nmspace))
			{
				return null;
			}

			return nmspace.GetMethod(methRef);
		}

		public void FindOverrides()
		{
			foreach (var nm in namespaces.Values)
			{
				nm.FindOverrides();
			}
		}

		private Namespace GetOrAddNamespace(TypeReference typeRef)
		{
			if (!namespaces.TryGetValue(typeRef.Namespace, out Namespace nmspace))
			{
				nmspace = new Namespace(project, this, typeRef.Namespace);
				namespaces.Add(typeRef.Namespace, nmspace);
			}
			return nmspace;
		}

		public void LoadAssemblieReferences()
		{
			foreach (var assRef in assembly.MainModule.AssemblyReferences)
			{
				project.AddAssembly(assRef);
			}
		}

		public void HideConstants()
		{
			var stringInstructions = namespaces.Values.SelectMany(n => n.GetStringInstructions()).ToList();
			var doubleInstructions = namespaces.Values.SelectMany(n => n.GetDoubleInstructions()).ToList();
			var longInstructions = namespaces.Values.SelectMany(n => n.GetLongInstructions()).ToList();
			var floatInstructions = namespaces.Values.SelectMany(n => n.GetFloatInstructions()).ToList();
			var intInstructions = namespaces.Values.SelectMany(n => n.GetIntInstructions()).ToList();
			var shortInstructions = namespaces.Values.SelectMany(n => n.GetShortInstructions()).ToList();
			Hider = new HiderClass(project, this);
			Hider.CreateHiderClass();
			Hider.HideStrings(stringInstructions);
			Hider.HideDoubles(doubleInstructions);
			Hider.HideLongs(longInstructions);
			Hider.HideFloats(floatInstructions);
			Hider.HideInts(intInstructions);
			Hider.HideBytes(shortInstructions);
			//Hider.FinalizeHiderClass();
		}

		public void AddSecurity()
		{
			EntryHashCode = EntryPoint.AddEntryPointSecurity();
			foreach (var nms in namespaces.Values)
			{
				nms.AddSecurity();
			}
		}

		public void AddType(TypeDefinition typeDefinition)
		{
			assembly.MainModule.Types.Add(typeDefinition);
		}

		public MethodReference Import(ConstructorInfo constructor)
		{
			return assembly.MainModule.Import(constructor);
		}
	}
}
