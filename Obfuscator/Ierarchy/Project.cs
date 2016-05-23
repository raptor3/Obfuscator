using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace Obfuscator
{
	public class Project
	{
		List<AssemblyDefinition> assemblies = new List<AssemblyDefinition>();
		Dictionary<string, Namespace> namespaces = new Dictionary<string, Namespace>();

		public void RegistrateAssembly(AssemblyDefinition assembly)
		{
			assemblies.Add(assembly);
		}

		public void Resolve()
		{
			foreach (var assembly in assemblies)
			{
				foreach (var type in assembly.MainModule.GetTypes())
				{
					if (type.FullName != "<Module>")
					{
						var namespc = RegistrateNamespace(type.FullName);
						var typeMy = namespc.RegisterType(type);
						foreach (var field in type.Fields)
						{
							typeMy.RegisterField(field);
						}
						foreach (var proper in type.Properties)
						{
							//typeMy.RegisterProperty(proper);
						}
						foreach (var method in type.Methods)
						{
							if (!method.IsGetter && !method.IsConstructor && !method.IsSetter) ;
							//typeMy.RegisterMethod(method);
						}
					}
				}
			}
		}

		public Namespace RegistrateNamespace(string namespaceName)
		{
			Namespace namespc;
			if (!namespaces.TryGetValue(namespaceName, out namespc))
			{
				namespc = new Namespace();
				namespaces.Add(namespaceName, namespc);
			}
			return namespc;
		}

		
	}
}
