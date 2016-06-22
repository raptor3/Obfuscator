using Mono.Cecil;
using System.Collections.Generic;
using System;
using Obfuscator.Iterator;
using Obfuscator.SkipRules;
using System.Linq;
using System.Text;

namespace Obfuscator.Structure
{
	public class Type
	{
		private Project project;

		private TypeDefinition definition;
		private string changes;
		private List<TypeReference> references = new List<TypeReference>();

		public string Changes
		{
			get { return changes; }
		}

		private Dictionary<string, Method> methods = new Dictionary<string, Method>();
		private Dictionary<string, Property> properties = new Dictionary<string, Property>();
		private Dictionary<string, Field> fields = new Dictionary<string, Field>();

		public Type(Project prj)
		{
			project = prj;
		}

		public void Resolve(TypeDefinition type)
		{
			definition = type;
			references.Add(type);

			foreach (var field in type.Fields)
			{
				Field fld;
				if (!fields.TryGetValue(field.FullName, out fld))
				{
					fld = new Field(project);
					fields.Add(field.FullName, fld);

				}

				fld.Resolve(field);
			}

			foreach (var prop in type.Properties)
			{
				Property prprty;
				if (!properties.TryGetValue(prop.FullName, out prprty))
				{
					prprty = new Property(project);
					properties.Add(prop.FullName, prprty);
				}

				prprty.Resolve(prop);
			}

			foreach (var method in type.Methods)
			{
				Method methd;
				if (!methods.TryGetValue(method.FullName, out methd))
				{
					methd = new Method(project);
					methods.Add(method.FullName, methd);
				}

				methd.Resolve(method);
			}
		}

		public void RegisterReference(TypeReference typeRef)
		{
			references.Add(typeRef);
		}

		public void RegisterReference(FieldReference field)
		{
			Field fld;
			if (!fields.TryGetValue(field.FullName, out fld))
			{
				fld = new Field(project);
				fields.Add(field.FullName, fld);

			}

			fld.RegisterReference(field);
		}

		public void RegisterReference(PropertyReference prop)
		{
			Property prprty;
			if (!properties.TryGetValue(prop.FullName, out prprty))
			{
				prprty = new Property(project);
				properties.Add(prop.FullName, prprty);
			}

			prprty.RegisterReference(prop);
		}

		public void RegisterReference(MethodReference method)
		{
			Method methd;
			if (!methods.TryGetValue(method.FullName, out methd))
			{
				methd = new Method(project);
				methods.Add(method.FullName, methd);
			}

			methd.RegisterReference(method);
		}

		public Method GetMethod(MethodReference methodRef)
		{
			Method methd;
			if (!methods.TryGetValue(methodRef.FullName, out methd))
			{
				return null;
			}

			return methd;
		}

		public void ChangeNamespace(string newNamespace)
		{
			foreach (var type in references)
			{
				type.Namespace = newNamespace;
			}
		}

		public string RunRules(INameIterator nameIterator, List<SkipNamespace> skipNamespaces, List<SkipType> skipTypes, List<SkipMethod> skipMethods, List<SkipField> skipFields, List<SkipProperty> skipProperties)
		{
			var iSkipMethods = new List<ISkipMethod>(skipMethods);
			iSkipMethods.AddRange(skipNamespaces);
			iSkipMethods.AddRange(skipTypes);

			var iSkipProperties = new List<ISkipProperty>(skipProperties);
			iSkipProperties.AddRange(skipNamespaces);
			iSkipProperties.AddRange(skipTypes);

			var iSkipFields = new List<ISkipField>(skipFields);
			iSkipFields.AddRange(skipNamespaces);
			iSkipFields.AddRange(skipTypes);

			var skippedFields = new StringBuilder("SkippedFields");
			var renamedFields = new StringBuilder("RenamedFields");
			skippedFields.AppendLine();
			renamedFields.AppendLine();

			var skippedProperties = new StringBuilder("SkippedProperties");
			var renamedProperties = new StringBuilder("RenamedProperties");
			skippedProperties.AppendLine();
			renamedProperties.AppendLine();

			var skippedMethods = new StringBuilder("SkippedMethods");
			var renamedMethods = new StringBuilder("RenamedMethods");
			skippedMethods.AppendLine();
			renamedMethods.AppendLine();

			nameIterator.Reset();

			foreach (var field in fields.Values)
			{
				if (field.ChangeName(nameIterator.Next(), iSkipFields.ToArray()))
				{
					renamedFields.AppendLine(field.Changes);
				}
				else
				{
					skippedFields.AppendLine(field.Changes);
				}
			}

			nameIterator.Reset();

			foreach (var prop in properties.Values)
			{
				if (prop.ChangeName(nameIterator.Next(), iSkipProperties.ToArray()))
				{
					renamedProperties.AppendLine(prop.Changes);
				}
				else
				{
					skippedProperties.AppendLine(prop.Changes);
				}
			}

			nameIterator.Reset();

			foreach (var method in methods.Values)
			{
				if (method.ChangeName(nameIterator.Next(), iSkipMethods.ToArray()))
				{
					renamedMethods.AppendLine(method.Changes);
				}
				else
				{
					skippedMethods.AppendLine(method.Changes);
				}
			}

			var result = new StringBuilder();
			result.AppendLine(skippedFields.ToString());
			result.AppendLine(renamedFields.ToString());
			result.AppendLine(skippedProperties.ToString());
			result.AppendLine(renamedProperties.ToString());
			result.AppendLine(skippedMethods.ToString());
			result.AppendLine(renamedMethods.ToString());
			return result.ToString();
		}

		public bool ChangeName(string name, params ISkipType[] skipTypes)
		{
			changes = definition.Name;

			if (skipTypes.Any(r=> r.IsTypeSkip(definition)))
			{
				return false;
			}

			foreach (var type in references)
			{
				type.Name = name;
			}

			changes += " -> " + name;

			return true;
		}
	}
}
