using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Obfuscator.Structure.Instrucitons;
using System;

namespace Obfuscator.Structure
{
	public class Type
	{
		private Project project;
		private Assembly assembly;
		private TypeDefinition definition;
		private string changes;
		private List<TypeReference> references = new List<TypeReference>();
		private Dictionary<string, Method> methods = new Dictionary<string, Method>();
		private Dictionary<string, Property> properties = new Dictionary<string, Property>();
		private Dictionary<string, Field> fields = new Dictionary<string, Field>();

		public string Changes
		{
			get { return changes; }
		}

		public Type(Project project, Assembly assembly)
		{
			this.project = project;
			this.assembly = assembly;
		}

		public void Resolve(TypeDefinition type)
		{
			definition = type;
			RegisterReference(type);

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
			foreach (var attr in type.CustomAttributes)
			{
				project.RegistrateReference(attr.AttributeType);
			}
		}

		public void RegisterReference(TypeReference typeRef)
		{
			references.Add(typeRef);
			if (typeRef.IsGenericInstance)
			{
				var genericInstance = typeRef as GenericInstanceType;
				foreach (var genericArguments in genericInstance.GenericArguments)
				{
					project.RegistrateReference(genericArguments);
				}
			}
			foreach (var genericParameters in typeRef.GenericParameters)
			{
				foreach (var constraint in genericParameters.Constraints)
				{
					project.RegistrateReference(constraint);
				}
			}
		}

		public void RegisterReference(FieldReference field)
		{
			RegisterReference(field.DeclaringType);
			GetOrAddField(field).RegisterReference(field);
		}

		public void RegisterReference(PropertyReference prop)
		{
			RegisterReference(prop.DeclaringType);
			GetOrAddProperty(prop).RegisterReference(prop);
		}

		public void RegisterReference(MethodReference method)
		{
			RegisterReference(method.DeclaringType);
			GetOrAddMethod(method).RegisterReference(method);
		}

		public Method GetMethod(MethodReference methodRef)
		{
			if (!methods.TryGetValue(methodRef.Resolve().FullName, out Method methd))
			{
				return null;
			}

			return methd;
		}

		public void ChangeNamespace(string newNamespace)
		{
			foreach (var type in references)
			{
				if (!type.IsGenericInstance)
				{
					type.Namespace = newNamespace;
				}
				type.GetElementType().Namespace = newNamespace;
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
				method.ChangeName(nameIterator.Next());

				if (method.IsObfuscated)
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

		public void AddSecurity()
		{
			foreach (var method in methods.Values)
			{
				method.AddSecurity();
			}
		}

		public bool ChangeName(string name)
		{
			changes = definition.Name;

			if (assembly.SkipTypes.Any(r => r.IsTypeSkip(definition)))
			{
				return false;
			}

			var nameIterator = project.NameIteratorFabric.GetIterator();

			foreach (var genericParameters in definition.GenericParameters)
			{
				genericParameters.Name = nameIterator.Next();
			}

			foreach (var type in references)
			{
				type.GetElementType().Name = name;
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
			if (!methods.TryGetValue(method.Resolve().FullName, out Method methd))
			{
				methd = new Method(project, assembly);
				methods.Add(method.Resolve().FullName, methd);
			}
			return methd;
		}

		private Field GetOrAddField(FieldReference field)
		{
			if (!fields.TryGetValue(field.FullName, out Field fld))
			{
				fld = new Field(project, assembly);
				fields.Add(field.FullName, fld);

			}
			return fld;
		}

		private Property GetOrAddProperty(PropertyReference prop)
		{
			if (!properties.TryGetValue(prop.FullName, out Property prprty))
			{
				prprty = new Property(project, assembly);
				properties.Add(prop.FullName, prprty);
			}
			return prprty;
		}

		public IEnumerable<StringInstruction> GetStringInstructions()
		{
			if (definition.IsInterface) return new StringInstruction[0];
			return methods.Values.SelectMany(m => m.GetStringInstructions());
		}

		public IEnumerable<NumberInstruction<long>> GetLongInstructions()
		{
			if (definition.IsInterface) return new NumberInstruction<long>[0];
			return methods.Values.SelectMany(m => m.GetLongInstructions());
		}

		public IEnumerable<NumberInstruction<double>> GetDoubleInstructions()
		{
			if (definition.IsInterface) return new NumberInstruction<double>[0];
			return methods.Values.SelectMany(m => m.GetDoubleInstructions());
		}

		public IEnumerable<NumberInstruction<float>> GetFloatInstructions()
		{
			if (definition.IsInterface) return new NumberInstruction<float>[0];
			return methods.Values.SelectMany(m => m.GetFloatInstructions());
		}

		public IEnumerable<NumberInstruction<int>> GetIntInstructions()
		{
			if (definition.IsInterface) return new NumberInstruction<int>[0];
			return methods.Values.SelectMany(m => m.GetIntInstructions());
		}

		public IEnumerable<NumberInstruction<sbyte>> GetShortInstructions()
		{
			if (definition.IsInterface) return new NumberInstruction<sbyte>[0];
			return methods.Values.SelectMany(m => m.GetShortInstructions());
		}

	}
}
