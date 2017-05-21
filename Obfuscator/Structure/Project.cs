using Mono.Cecil;
using Obfuscator.Iterator;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Obfuscator.Structure
{
	[Serializable()]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true)]
	[XmlRoot("Obfuscator", Namespace = "", IsNullable = false)]
	public class Project
	{
		private DefaultAssemblyResolver _resolver;
		[XmlIgnore]
		public INameIteratorFabric NameIteratorFabric { get; set; }

		/// <remarks/>
		[XmlElement("Module")]
		public List<Assembly> Assemblies { get; set; }
		[XmlIgnore]
		public List<Assembly> AssembliesReferences { get; set; }

		public void Load(DefaultAssemblyResolver resolver)
		{
			AssembliesReferences = new List<Assembly>();
			_resolver = resolver;

			foreach (var module in Assemblies)
			{
				module.LoadAssemblies(resolver, this);
			}
			foreach (var module in Assemblies)
			{
				module.LoadAssemblieReferences();
			}
		}

		public void AddAssembly(AssemblyNameReference ass)
		{
			var assembly = Assemblies.SingleOrDefault(a => a.Name == ass.FullName) ?? AssembliesReferences.SingleOrDefault(a => a.Name == ass.FullName);
			if (assembly == null)
			{
				AssembliesReferences.Add(
					new Assembly(this, _resolver.Resolve(ass, new ReaderParameters
					{
						ReadingMode = ReadingMode.Immediate,
						ReadSymbols = false,
						AssemblyResolver = _resolver
					}))
				);
			}
		}

		public void Resolve()
		{
			foreach (var module in Assemblies)
			{
				module.Resolve();
			}
			foreach (var module in AssembliesReferences)
			{
				module.Resolve();
			}
			foreach (var module in Assemblies)
			{
				module.FindOverrides();
			}
		}

		public void RegistrateReference(TypeReference typeRef)
		{
			var assemblyToObfuscate = Assemblies.SingleOrDefault(a => a.HasType(typeRef)) ?? AssembliesReferences.SingleOrDefault(a => a.HasType(typeRef));

			if (assemblyToObfuscate != null)
			{
				assemblyToObfuscate.RegistrateReference(typeRef);
			}
		}

		public void RegistrateReference(FieldReference fieldRef)
		{
			var assemblyToObfuscate = Assemblies.SingleOrDefault(a => a.HasField(fieldRef)) ?? AssembliesReferences.SingleOrDefault(a => a.HasField(fieldRef));

			if (assemblyToObfuscate != null)
			{
				assemblyToObfuscate.RegistrateReference(fieldRef);
			}
		}

		public void RegistrateReference(PropertyReference propRef)
		{
			var assemblyToObfuscate = Assemblies.SingleOrDefault(a => a.HasProperty(propRef)) ?? AssembliesReferences.SingleOrDefault(a => a.HasProperty(propRef));

			if (assemblyToObfuscate != null)
			{
				assemblyToObfuscate.RegistrateReference(propRef);
			}
		}

		public void RegistrateReference(MethodReference methRef)
		{
			var assemblyToObfuscate = Assemblies.SingleOrDefault(a => a.HasMethod(methRef)) ?? AssembliesReferences.SingleOrDefault(a => a.HasMethod(methRef));

			if (assemblyToObfuscate != null)
			{
				assemblyToObfuscate.RegistrateReference(methRef);
			}
		}

		public Method GetMethod(MethodReference methRef)
		{
			var assemblyToObfuscate = Assemblies.SingleOrDefault(a => a.HasMethod(methRef)) ?? AssembliesReferences.SingleOrDefault(a => a.HasMethod(methRef));

			if (assemblyToObfuscate != null)
			{
				return assemblyToObfuscate.GetMethod(methRef);
			}

			return null;
		}


		public string RunRules()
		{
			var result = new StringBuilder();

			foreach (var assembly in Assemblies)
			{
				result.AppendLine(assembly.Name);
				result.AppendLine(assembly.RunRules());
			}

			return result.ToString();
		}

		public void SaveAssemblies(string output)
		{
			foreach (var assembly in Assemblies)
			{
				assembly.Save(output);
			}
		}
	}
}
