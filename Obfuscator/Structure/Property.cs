using Mono.Cecil;
using Obfuscator.SkipRules;
using System.Collections.Generic;
using System.Linq;

namespace Obfuscator.Structure
{
	public class Property
	{
		private Project project;
		private Assembly assembly;
		private PropertyDefinition definition;
		private string changes;
		private List<PropertyReference> references = new List<PropertyReference>();

		public string Changes
		{
			get { return changes; }
		}

		public Property(Project project, Assembly assembly)
		{
			this.project = project;
			this.assembly = assembly;
		}

		public void RegisterReference(PropertyReference propRef)
		{
			references.Add(propRef);
		}

		public bool ChangeName(string name, params ISkipProperty[] skipProperties)
		{
			changes = definition.Name;
			if (skipProperties.Any( r=> r.IsPropertySkip(definition)))
			{
				return false;
			}

			foreach (var prop in references)
			{
				prop.Name = name;
			}

			changes += " -> " + name;

			return true;
		}

		public void Resolve(PropertyDefinition prop)
		{
			references.Add(prop);
			definition = prop;
		}
	}
}
