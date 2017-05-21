using Mono.Cecil;
using Obfuscator.Iterator;
using Obfuscator.SkipRules;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Obfuscator.Structure
{
	public class Assembly
	{
		private Project project;
		private AssemblyDefinition assembly;
		private Dictionary<string, Namespace> namespaces = new Dictionary<string, Namespace>();

		public string Name { get { return assembly.FullName; } }

		[XmlAttribute("file")]
		public string File { get; set; }

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

		public void Resolve()
		{
			SkipNamespaces = OnlySkipNamespaces.Select(s => s as ISkipNamespace).ToList();
			SkipTypes = OnlySkipTypes.Select(s => s as ISkipType).ToList();
			SkipFields = OnlySkipFields.Select(s => s as ISkipField).ToList();
			SkipMethods = OnlySkipMethods.Select(s => s as ISkipMethod).ToList();
			SkipProperties = OnlySkipProperties.Select(s => s as ISkipProperty).ToList();

			SkipTypes.AddRange(SkipNamespaces.Select(s => s as ISkipType));
			SkipFields.AddRange(SkipTypes.Select(s=>s as ISkipField));
			SkipMethods.AddRange(SkipTypes.Select(s => s as ISkipMethod));
			SkipProperties.AddRange(SkipTypes.Select(s => s as ISkipProperty));

			foreach (var type in assembly.MainModule.Types)
			{
				if (type.FullName == "<Module>")
				{
					continue;
				}

				Namespace nmspace;
				if (!namespaces.TryGetValue(type.Namespace, out nmspace))
				{
					nmspace = new Namespace(project, this, type.Namespace);
					namespaces.Add(type.Namespace, nmspace);
				}
				
				nmspace.Resolve(type);
			}
		}

		public bool HasType(TypeReference typeRef)
		{
			return assembly.FullName == typeRef.Resolve().Module.Assembly.FullName;
		}

		public bool HasField(FieldReference fieldRef)
		{
			return assembly.FullName == fieldRef.Resolve().Module.Assembly.FullName;
		}

		public void Save(string output)
		{
			assembly.Write(Path.Combine(output, Path.GetFileName(File)));
		}

		public bool HasProperty(PropertyReference propRef)
		{
			return assembly.FullName == propRef.Resolve().Module.Assembly.FullName;
		}

		public bool HasMethod(MethodReference methRef)
		{
			return assembly.FullName == methRef.Resolve().Module.Assembly.FullName;
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
				} else
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
			Namespace nmspace;
			if (!namespaces.TryGetValue(methRef.DeclaringType.Namespace, out nmspace))
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
			Namespace nmspace;
			if (!namespaces.TryGetValue(typeRef.Namespace, out nmspace))
			{
				nmspace = new Namespace(project, this, typeRef.Namespace);
				namespaces.Add(typeRef.Namespace, nmspace);
			}
			return nmspace;
		}
	}
}
