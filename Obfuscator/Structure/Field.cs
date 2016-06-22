﻿using Mono.Cecil;
using System.Collections.Generic;
using Obfuscator.SkipRules;
using System.Linq;
using System;

namespace Obfuscator.Structure
{
	public class Field
	{
		private Project project;
		private FieldDefinition definition;
		private string changes;

		public string Changes
		{
			get { return changes; }
		}

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

		public bool ChangeName(string name, params ISkipField[] skipFields)
		{
			changes = definition.Name;

			if (skipFields.Any(r => r.IsFieldSkip(definition)))
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
