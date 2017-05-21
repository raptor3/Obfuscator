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
		public MethodGroup Group { get; set; }

		private bool notObfuscated = true;

		public string Changes
		{
			get { return changes; }
		}

		public Method(Project project, Assembly assembly)
		{
			this.project = project;
			this.assembly = assembly;
			Group = new MethodGroup(this);
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
			if (!notObfuscated || Group.Methods.Any(m => !m.WillChanged()))
			{
				return false;
			}
			foreach (var m in Group.Methods)
			{
				m.changes = definition.Name;
				foreach (var methRef in m.references)
				{
					methRef.Name = name;
				}
				m.notObfuscated = false;

				m.changes += " -> " + name;
			}
			return true;
		}

		public bool WillChanged()
		{
			return project.Assemblies.Any(a => a.Name == assembly.Name) && !definition.IsConstructor && !assembly.SkipMethods.Any(r => r.IsMethodSkip(definition));
		}

		public void FindOverrides()
		{
			if (!definition.IsNewSlot && definition.IsVirtual)
			{
				TypeReference typeRef = definition.DeclaringType.BaseType;
				while (typeRef != null)
				{
					var typeDef = typeRef.Resolve();
					foreach (var meth in typeDef?.Methods)
					{
						if (definition.SignatureMatches(meth))
						{
							var m = project.GetMethod(meth);
							Group = new MethodGroup(this, m);
							m.Group = Group;
							break;
						}
					}
					typeRef = typeDef.BaseType;
				}
			}
			foreach (var interf in definition.DeclaringType.Interfaces)
			{
				var typeDef = interf.Resolve();
				foreach (var meth in typeDef?.Methods)
				{
					if (definition.SignatureMatches(meth))
					{
						var m = project.GetMethod(meth);
						Group = new MethodGroup(this, m);
						m.Group = Group;
						break;
					}
				}
			}
		}
	}
}
