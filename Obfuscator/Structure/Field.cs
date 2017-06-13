﻿using Mono.Cecil;
using System.Collections.Generic;
using Obfuscator.SkipRules;
using System.Linq;

namespace Obfuscator.Structure
{
    public class Field
	{
		private Project project;
		private Assembly assembly;
		private FieldDefinition definition;
		private string changes;
		private List<FieldReference> references = new List<FieldReference>();

		public string Changes { get { return changes; } }

		public Field(Project project, Assembly assembly)
		{
			this.project = project;
			this.assembly = assembly;
		}

		public void RegisterReference(FieldReference fieldRef)
		{
			references.Add(fieldRef);
		}

		public void Resolve(FieldDefinition fieldDef)
		{
			definition = fieldDef;
			references.Add(fieldDef);

			foreach (var attr in fieldDef.CustomAttributes)
			{
				project.RegistrateReference(attr.AttributeType);
			}
		}

		public bool ChangeName(string name)
		{
			changes = definition.Name;

			if (assembly.SkipFields.Any(r => r.IsFieldSkip(definition)))
			{
				return false;
			}

			foreach (var fieldRef in references)
			{
				fieldRef.Name = name;
			}
			changes += " -> " + name;
			return true;
		}
	}
}
