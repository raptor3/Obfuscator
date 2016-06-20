using Mono.Cecil;
using System.Collections.Generic;
using System;
using Obfuscator.Iterator;
using Obfuscator.SkipRules;
using System.Linq;

namespace Obfuscator.Structure
{
	public class Type
	{
		List<TypeReference> references = new List<TypeReference>();

		Dictionary<string, Method> methods = new Dictionary<string, Method>();
		Dictionary<string, Property> properties = new Dictionary<string, Property>();
		Dictionary<string, Field> fields = new Dictionary<string, Field>();

		Project project;

		TypeDefinition definition;

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

		public void RunRules(INameIterator nameIterator, List<SkipNamespace> skipNamespaces, List<SkipType> skipTypes, List<SkipMethod> skipMethods, List<SkipField> skipFields, List<SkipProperty> skipProperties)
		{
			nameIterator.Reset();

			var iSkipMethods = new List<ISkipMethod>(skipMethods);
			iSkipMethods.AddRange(skipNamespaces);
			iSkipMethods.AddRange(skipTypes);

			var iSkipProperties = new List<ISkipProperty>(skipProperties);
			iSkipProperties.AddRange(skipNamespaces);
			iSkipProperties.AddRange(skipTypes);

			var iSkipFields = new List<ISkipField>(skipFields);
			iSkipFields.AddRange(skipNamespaces);
			iSkipFields.AddRange(skipTypes);

			foreach (var field in fields.Values)
			{
				field.ChangeName(nameIterator.Next(), iSkipFields.ToArray());
			}

			nameIterator.Reset();

			foreach (var prop in properties.Values)
			{
				prop.ChangeName(nameIterator.Next(), iSkipProperties.ToArray());
			}

			nameIterator.Reset();

			foreach (var method in methods.Values)
			{
				method.ChangeName(nameIterator.Next(), iSkipMethods.ToArray());
			}
		}

		public void ChangeName(string name, params ISkipType[] skipTypes)
		{
			if (skipTypes.Any(r=> r.IsTypeSkip(definition)))
			{
				return;
			}

			foreach (var type in references)
			{
				type.Name = name;
			}
		}
	}
}
