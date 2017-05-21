using Mono.Cecil;
using Obfuscator.SkipRules;
using System.Collections.Generic;
using System.Linq;

namespace Obfuscator.Structure
{
	public class Method
	{
		private MethodDefinition definition;
		private Project project;
		private Assembly assembly;
		private string changes;
		private List<MethodReference> references = new List<MethodReference>();
		private MethodGroup group;

		private bool notObfuscated = true;

		public string Changes
		{
			get { return changes; }
		}

		public Method(Project project, Assembly assembly)
		{
			this.project = project;
			this.assembly = assembly;
		}

		public void RegisterReference(MethodReference methodRef)
		{
			references.Add(methodRef);
		}

		public void Resolve(MethodDefinition method)
		{
			definition = method;
			references.Add(method);

			if (!method.HasBody)
			{
				return;
			}

			foreach (var instruction in method.Body.Instructions)
			{
				var fieldReference = instruction.Operand as FieldReference;
				var typeReference = instruction.Operand as TypeReference;
				var propertyReference = instruction.Operand as PropertyReference;
				var methodReference = instruction.Operand as MethodReference;

				if (fieldReference != null)
				{
					project.RegistrateReference(fieldReference);
				}
				if (typeReference != null)
				{
					project.RegistrateReference(typeReference);
				}
				if (propertyReference != null)
				{
					project.RegistrateReference(propertyReference);
				}
				if (methodReference != null)
				{
					project.RegistrateReference(methodReference);
				}
			}
		}

		public bool ChangeName(string name)
		{
			changes = definition.Name;

			if (assembly.SkipMethods.Any(r => r.IsMethodSkip(definition)))
			{
				return false;
			}

			foreach (var methRef in references)
			{
				//methRef.Name = name;
			}
			changes += " -> " + name;
			return true;
		}

		public void FindOverrides()
		{
			TypeReference typeRef = definition.DeclaringType.BaseType;
			while (typeRef != null)
			{
				var typeDef = typeRef.Resolve();
				foreach(var meth in typeDef.Methods)
				{
					
				}

				typeRef = typeDef.BaseType;
			}
		}
	}
}
