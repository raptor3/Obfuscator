using Mono.Cecil;
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
		private MethodGroup _group;
		private bool isObfuscated = false;

		public string Changes
		{
			get { return changes; }
		}

		public bool IsObfuscated
		{
			get { return isObfuscated; }
		}

		public MethodGroup Group
		{
			get
			{
				return _group;
			}
			set
			{
				if (_group == null)
				{
					_group = value;
					return;
				}
				var methods = _group.Methods;
				foreach (var m in methods)
				{
					m._group = value;
				}
			}
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

			if (methodRef.IsGenericInstance)
			{
				var genericInstance = methodRef as GenericInstanceMethod;
				foreach (var genericArguments in genericInstance.GenericArguments)
				{
					project.RegistrateReference(genericArguments);
				}
			}

			foreach (var genericParameters in methodRef.GenericParameters)
			{
				foreach (var constraint in genericParameters.Constraints)
				{
					project.RegistrateReference(constraint);
				}
			}
		}

		public void Resolve(MethodDefinition method)
		{
			definition = method;
			RegisterReference(method);

			if (!method.HasBody)
			{
				return;
			}

			foreach (var instruction in method.Body.Instructions)
			{
				project.RegistrateInstruction(instruction);
			}
		}

		public bool ChangeName(string name)
		{
			if (!isObfuscated)
			{
				changes = definition.Name;
			}

			if (Group.Methods.Any(m => !m.WillChanged()))
			{
				return false;
			}

			foreach (var m in Group.Methods)
			{
				m.changes = m.definition.Name;
				m.definition.Name = name;

				var nameIterator = project.NameIteratorFabric.GetIterator();

				foreach (var genericParameters in definition.GenericParameters)
				{
					genericParameters.Name = nameIterator.Next();
				}

				foreach (var methRef in m.references)
				{
					if (!methRef.IsGenericInstance)
					{
						methRef.Name = name;
					}
					methRef.GetElementMethod().Name = name;
				}
				m.isObfuscated = true;

				m.changes += " -> " + name;
			}
			return true;
		}

		public bool WillChanged()
		{
			return !isObfuscated && project.Assemblies.Any(a => a.Name == assembly.Name) && !definition.IsConstructor && !assembly.SkipMethods.Any(r => r.IsMethodSkip(definition));
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
						if (definition.Name == meth.Name)
						{
							var m = project.GetMethod(meth);
							Group = new MethodGroup(this, m);
							m.Group = Group;
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
					if (definition.Name == meth.Name)
					{
						var m = project.GetMethod(meth);
						Group = new MethodGroup(this, m);
						m.Group = Group;
					}
				}
			}
		}
	}
}
