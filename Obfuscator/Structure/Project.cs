﻿using Mono.Cecil;
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
	public partial class Project
	{
		private DefaultAssemblyResolver _resolver;

		/// <remarks/>
		[XmlElement("Module")]
		public Assembly[] Assemblies { get; set; }

		public void Load(DefaultAssemblyResolver resolver)
		{
			_resolver = resolver;

			foreach (var module in Assemblies)
			{
				module.LoadAssemblies(resolver, this);
			}
		}

		public void Resolve()
		{
			foreach (var module in Assemblies)
			{
				module.Resolve();
			}
		}

		public void RegistrateReference(TypeReference typeRef)
		{
			var assemblyToObfuscate = Assemblies.SingleOrDefault(a => a.HasType(typeRef));

			if (assemblyToObfuscate != null)
			{
				assemblyToObfuscate.RegistrateReference(typeRef);
			}
		}

		public void RegistrateReference(FieldReference fieldRef)
		{
			var assemblyToObfuscate = Assemblies.SingleOrDefault(a => a.HasField(fieldRef));

			if (assemblyToObfuscate != null)
			{
				assemblyToObfuscate.RegistrateReference(fieldRef);
			}
		}

		public void RegistrateReference(PropertyReference propRef)
		{
			var assemblyToObfuscate = Assemblies.SingleOrDefault(a => a.HasProperty(propRef));

			if (assemblyToObfuscate != null)
			{
				assemblyToObfuscate.RegistrateReference(propRef);
			}
		}

		public void RegistrateReference(MethodReference methRef)
		{
			var assemblyToObfuscate = Assemblies.SingleOrDefault(a => a.HasMethod(methRef));

			if (assemblyToObfuscate != null)
			{
				assemblyToObfuscate.RegistrateReference(methRef);
			}
		}

		public Method GetMethod(MethodReference methRef)
		{
			var assemblyToObfuscate = Assemblies.SingleOrDefault(a => a.HasMethod(methRef));

			if (assemblyToObfuscate != null)
			{
				return assemblyToObfuscate.GetMethod(methRef);
			}

			return null;
		}


		public void RunRules(INameIterator nameIterator)
		{
			foreach (var assembly in Assemblies)
			{
				assembly.RunRules(nameIterator);
			}
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
