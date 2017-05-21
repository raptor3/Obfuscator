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
		private Assembly assembly;
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

		public Type(Project project, Assembly assembly)
		{
			this.project = project;
			this.assembly = assembly;
		}

		public void Resolve(TypeDefinition type)
		{
			definition = type;
			references.Add(type);

			foreach (var field in type.Fields)
			{
				GetOrAddField(field).Resolve(field);
			}

			foreach (var prop in type.Properties)
			{
				GetOrAddProperty(prop).Resolve(prop);
			}

			foreach (var method in type.Methods)
			{
				GetOrAddMethod(method).Resolve(method);
			}

			if (type.BaseType != null)
			{
				project.RegistrateReference(type.BaseType);
			}
			foreach (var interf in type.Interfaces)
			{
				project.RegistrateReference(interf);
			}
		}

		public void RegisterReference(TypeReference typeRef)
		{
			references.Add(typeRef);
		}

		public void RegisterReference(FieldReference field)
		{
			GetOrAddField(field).RegisterReference(field);
		}

		public void RegisterReference(PropertyReference prop)
		{
			GetOrAddProperty(prop).RegisterReference(prop);
		}

		public void RegisterReference(MethodReference method)
		{
			GetOrAddMethod(method).RegisterReference(method);
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

		public string RunRules()
		{

			var skippedFields = new StringBuilder("SkippedFields : {");
			var renamedFields = new StringBuilder("RenamedFields : {");
			skippedFields.AppendLine();
			renamedFields.AppendLine();

			var skippedProperties = new StringBuilder("SkippedProperties : {");
			var renamedProperties = new StringBuilder("RenamedProperties : {");
			skippedProperties.AppendLine();
			renamedProperties.AppendLine();

			var skippedMethods = new StringBuilder("SkippedMethods : {");
			var renamedMethods = new StringBuilder("RenamedMethods : {");
			skippedMethods.AppendLine();
			renamedMethods.AppendLine();

			var nameIterator = project.NameIteratorFabric.GetIterator();

			foreach (var field in fields.Values)
			{
				if (field.ChangeName(nameIterator.Next()))
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
				if (prop.ChangeName(nameIterator.Next()))
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
				if (method.ChangeName(nameIterator.Next()))
				{
					renamedMethods.AppendLine(method.Changes);
				}
				else
				{
					skippedMethods.AppendLine(method.Changes);
				}
			}

			skippedFields.AppendLine("}");
			renamedFields.AppendLine("}");
			skippedProperties.AppendLine("}");
			renamedProperties.AppendLine("}");
			skippedMethods.AppendLine("}");
			renamedMethods.AppendLine("}");

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

		public void FindOverrides()
		{
			foreach (var method in methods.Values)
			{
				method.FindOverrides();
			}
		}

		private Method GetOrAddMethod(MethodReference method)
		{
			Method methd;
			if (!methods.TryGetValue(method.FullName, out methd))
			{
				methd = new Method(project, assembly);
				methods.Add(method.FullName, methd);
			}
			return methd;
		}

		private Field GetOrAddField(FieldReference field)
		{
			Field fld;
			if (!fields.TryGetValue(field.FullName, out fld))
			{
				fld = new Field(project, assembly);
				fields.Add(field.FullName, fld);

			}
			return fld;
		}

		private Property GetOrAddProperty(PropertyReference prop)
		{
			Property prprty;
			if (!properties.TryGetValue(prop.FullName, out prprty))
			{
				prprty = new Property(project, assembly);
				properties.Add(prop.FullName, prprty);
			}
			return prprty;
		}
	}
}
