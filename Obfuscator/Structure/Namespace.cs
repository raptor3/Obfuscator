using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Obfuscator.Structure
{
	public class Namespace
	{
		private Project project;
		private Assembly assembly;
		private bool renamed;
		private string changes;
		private string name;
		private Dictionary<string, Type> types = new Dictionary<string, Type>();

		public string Changes { get { return changes; } }

		public Namespace(Project project, Assembly assembly, string name)
		{
			this.project = project;
			this.assembly = assembly;
			this.name = name;
		}

		private Type GetOrAddType(TypeReference type)
		{
			if (!types.TryGetValue(type.Name, out Type tpe))
			{
				tpe = new Type(project, assembly);

				types.Add(type.Name, tpe);
			}
			return tpe;
		}

		public void RegisterReference(TypeReference type)
		{
			GetOrAddType(type).RegisterReference(type);
		}

		public void RegisterReference(FieldReference field)
		{
			GetOrAddType(field.DeclaringType).RegisterReference(field);
		}

		public void RegisterReference(PropertyReference propRef)
		{
			GetOrAddType(propRef.DeclaringType).RegisterReference(propRef);
		}

		public void RegisterReference(MethodReference methodRef)
		{
			GetOrAddType(methodRef.DeclaringType).RegisterReference(methodRef);
		}

		public void Resolve(TypeDefinition type)
		{
			GetOrAddType(type).Resolve(type);
		}

		public Method GetMethod(MethodReference methodRef)
		{
			if (!types.TryGetValue(methodRef.DeclaringType.Name, out Type tpe))
			{
				return null;
			}

			return tpe.GetMethod(methodRef);
		}

		public bool ChangeName(string newName)
		{
			changes = name;

			if (assembly.SkipNamespaces.Any(r => r.IsNamespaceSkip(name)))
			{
				return false;
			}

			foreach (var type in types.Values)
			{
				type.ChangeNamespace(newName);
			}

			changes += " -> " + newName;

			return true;
		}

		public string RunRules()
		{
			var nameIterator = project.NameIteratorFabric.GetIterator();

			var skippedTypes = new StringBuilder("SkippedTypes : {");
			var renamedTypes = new StringBuilder("RenamedTypes : {");
			skippedTypes.AppendLine();
			renamedTypes.AppendLine();

			foreach (var type in types.Values)
			{
				string typeR = type.RunRules();

				if (type.ChangeName(nameIterator.Next()))
				{
					renamedTypes.AppendLine(type.Changes);
					renamedTypes.AppendLine(typeR);
				}
				else
				{
					skippedTypes.AppendLine(type.Changes);
					skippedTypes.AppendLine(typeR);
				}
			}

			skippedTypes.AppendLine("}");
			renamedTypes.AppendLine("}");

			var result = new StringBuilder();
			result.AppendLine(skippedTypes.ToString());
			result.AppendLine(renamedTypes.ToString());

			return result.ToString();
		}

		public void FindOverrides()
		{
			foreach (var type in types.Values)
			{
				type.FindOverrides();
			}
		}

	    public void HideStrings()
	    {
            foreach (var type in types.Values)
            {
                type.HideStrings();
            }
        }
	}


}
