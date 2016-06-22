using Mono.Cecil;
using Obfuscator.Iterator;
using Obfuscator.SkipRules;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Obfuscator.Structure
{
	public partial class Assembly
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
		public List<SkipNamespace> SkipNamespaces { get; set; }

		[XmlElement("SkipField", typeof(SkipField))]
		public List<SkipField> SkipFields { get; set; }

		[XmlElement("SkipMethod", typeof(SkipMethod))]
		public List<SkipMethod> SkipMethods { get; set; }

		[XmlElement("SkipProperty", typeof(SkipProperty))]
		public List<SkipProperty> SkipProperties { get; set; }

		[XmlElement("SkipType", typeof(SkipType))]
		public List<SkipType> SkipTypes { get; set; }

		public void Resolve()
		{
			foreach (var type in assembly.MainModule.Types)
			{
				if (type.FullName == "<Module>")
				{
					continue;
				}

				Namespace nmspace;
				if (!namespaces.TryGetValue(type.Namespace, out nmspace))
				{
					nmspace = new Namespace(project, type.Namespace);
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

		public string RunRules(INameIterator nameIterator)
		{
			nameIterator.Reset();

			var skippedNamespace = new StringBuilder("SkippedNamespaces");
			var renamedNamespace = new StringBuilder("RenamedNamespaces");
			skippedNamespace.AppendLine();
			renamedNamespace.AppendLine();

			foreach (var nmspace in namespaces.Values)
			{
				string nmsR = nmspace.RunRules(nameIterator, SkipNamespaces, SkipTypes, SkipMethods, SkipFields, SkipProperties);

				if (nmspace.ChangeName(nameIterator.Next(), SkipNamespaces.ToArray()))
				{
					renamedNamespace.AppendLine(nmspace.Changes);
					renamedNamespace.AppendLine(nmsR);
				} else
				{
					skippedNamespace.AppendLine(nmspace.Changes);
					skippedNamespace.AppendLine(nmsR);
				}
			}

			var result = new StringBuilder();
			result.AppendLine(skippedNamespace.ToString());
			result.AppendLine(renamedNamespace.ToString());
			return result.ToString();
		}

		public void RegistrateReference(TypeReference typeRef)
		{
			Namespace nmspace;
			if (!namespaces.TryGetValue(typeRef.Namespace, out nmspace))
			{
				nmspace = new Namespace(project, typeRef.Namespace);
				namespaces.Add(typeRef.Namespace, nmspace);
			}

			nmspace.RegisterReference(typeRef);
		}

		public void RegistrateReference(FieldReference fieldRef)
		{
			Namespace nmspace;
			if (!namespaces.TryGetValue(fieldRef.DeclaringType.Namespace, out nmspace))
			{
				nmspace = new Namespace(project, fieldRef.DeclaringType.Namespace);
				namespaces.Add(fieldRef.DeclaringType.Namespace, nmspace);
			}

			nmspace.RegisterReference(fieldRef);
		}

		public void RegistrateReference(PropertyReference propRef)
		{
			Namespace nmspace;
			if (!namespaces.TryGetValue(propRef.DeclaringType.Namespace, out nmspace))
			{
				nmspace = new Namespace(project, propRef.DeclaringType.Namespace);
				namespaces.Add(propRef.DeclaringType.Namespace, nmspace);
			}

			nmspace.RegisterReference(propRef);
		}

		public void RegistrateReference(MethodReference methRef)
		{
			Namespace nmspace;
			if (!namespaces.TryGetValue(methRef.DeclaringType.Namespace, out nmspace))
			{
				nmspace = new Namespace(project, methRef.DeclaringType.Namespace);
				namespaces.Add(methRef.DeclaringType.Namespace, nmspace);
			}

			nmspace.RegisterReference(methRef);
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
	}
}
