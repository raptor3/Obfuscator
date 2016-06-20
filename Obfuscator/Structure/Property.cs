using Mono.Cecil;
using Obfuscator.SkipRules;
using System.Collections.Generic;
using System.Linq;

namespace Obfuscator.Structure
{
	public class Property
	{
		private Project project;
		private PropertyDefinition definition;
		List<PropertyReference> references = new List<PropertyReference>();

		public Property(Project project)
		{
			this.project = project;
		}

		public void RegisterReference(PropertyReference propRef)
		{
			references.Add(propRef);
		}

		public void ChangeName(string name, params ISkipProperty[] skipProperties)
		{
			if (skipProperties.Any( r=> r.IsPropertySkip(definition)))
			{
				return;
			}

			foreach (var prop in references)
			{
				prop.Name = name;
			}
		}

		public void Resolve(PropertyDefinition prop)
		{
			references.Add(prop);
			definition = prop;
		}
	}
}
