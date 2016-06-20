using Mono.Cecil;
using System.Collections.Generic;
using Obfuscator.SkipRules;
using System.Linq;

namespace Obfuscator.Structure
{
	public class Field
	{
		private Project project;
		private FieldDefinition definition;

		List<FieldReference> references = new List<FieldReference>();

		public Field(Project project)
		{
			this.project = project;
		}

		public void RegisterReference(FieldReference fieldRef)
		{
			references.Add(fieldRef);
		}

		public void Resolve(FieldDefinition fieldDef)
		{
			definition = fieldDef;
			references.Add(fieldDef);
		}

		public void ChangeName(string name, params ISkipField[] skipFields)
		{
			if (skipFields.Any(r => r.IsFieldSkip(definition)))
			{
				return;
			}

			foreach (var fieldRef in references)
			{
				fieldRef.Name = name;
			}
		}
	}
}
