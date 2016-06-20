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
		Dictionary<string, Namespace> namespaces = new Dictionary<string, Namespace>();

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

		public void RunRules(INameIterator nameIterator)
		{
			foreach (var nmspace in namespaces.Values)
			{
				nmspace.RunRules(nameIterator, SkipNamespaces, SkipTypes, SkipMethods, SkipFields, SkipProperties);
			}

			nameIterator.Reset();

			foreach (var nmspace in namespaces.Values)
			{
				nmspace.ChangeName(nameIterator.Next(), SkipNamespaces.ToArray());
			}
			
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
