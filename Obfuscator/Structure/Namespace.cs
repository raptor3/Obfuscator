using Mono.Cecil;
using System.Collections.Generic;
using System;
using Obfuscator.Iterator;
using Obfuscator.SkipRules;
using System.Linq;
using System.Text;

namespace Obfuscator.Structure
{
	public class Namespace
	{
		private Project project;
		private bool renamed;
		private string changes;
		private string name;
		private Dictionary<string, Type> types = new Dictionary<string, Type>();

		public string Changes {get { return changes; } }

		public Namespace(Project project, string name)
		{
			this.project = project;
			this.name = name;
		}

		public void RegisterReference(TypeReference type)
		{
			changes = type.Namespace;

			Type tpe;

			if (!types.TryGetValue(type.FullName, out tpe))
			{
				tpe = new Type(project);

				types.Add(type.FullName, tpe);
			}
			tpe.RegisterReference(type);
		}

		public void RegisterReference(FieldReference field)
		{
			Type tpe;

			if (!types.TryGetValue(field.DeclaringType.FullName, out tpe))
			{
				tpe = new Type(project);

				types.Add(field.DeclaringType.FullName, tpe);
			}
			tpe.RegisterReference(field);
		}

		public void RegisterReference(PropertyReference propRef)
		{
			Type tpe;

			if (!types.TryGetValue(propRef.DeclaringType.FullName, out tpe))
			{
				tpe = new Type(project);

				types.Add(propRef.DeclaringType.FullName, tpe);
			}
			tpe.RegisterReference(propRef);
		}

		public void RegisterReference(MethodReference methodRef)
		{
			Type tpe;

			if (!types.TryGetValue(methodRef.DeclaringType.FullName, out tpe))
			{
				tpe = new Type(project);

				types.Add(methodRef.DeclaringType.FullName, tpe);
			}

			tpe.RegisterReference(methodRef);
		}

		public void Resolve(TypeDefinition type)
		{
			Type tpe;

			if (!types.TryGetValue(type.FullName, out tpe))
			{
				tpe = new Type(project);

				types.Add(type.FullName, tpe);
			}

			tpe.Resolve(type);

		}

		public Method GetMethod(MethodReference methodRef)
		{
			Type tpe;

			if (!types.TryGetValue(methodRef.DeclaringType.FullName, out tpe))
			{
				return null;
			}

			return tpe.GetMethod(methodRef);
		}

		public bool ChangeName(string newName, params ISkipNamespace[] skipNamespaces)
		{
			if (skipNamespaces.Any(r => r.IsNamespaceSkip(this.name)))
			{
				return false;
			}

			foreach (var type in types.Values)
			{
				type.ChangeNamespace(newName);
			}

			changes = " -> " + name;

			return true;
		}

		public string RunRules(INameIterator nameIterator, List<SkipNamespace> skipNamespaces, List<SkipType> skipTypes, List<SkipMethod> skipMethods, List<SkipField> skipFields, List<SkipProperty> skipProperties)
		{
			nameIterator.Reset();

			var iSkipTypes = new List<ISkipType>(skipTypes);
			iSkipTypes.AddRange(skipNamespaces);

			var skippedTypes = new StringBuilder("SkippedTypes");
			var renamedTypes = new StringBuilder("RenamedTypes");
			skippedTypes.AppendLine();
			renamedTypes.AppendLine();

			foreach (var type in types.Values)
			{
				string typeR = type.RunRules(nameIterator, skipNamespaces, skipTypes, skipMethods, skipFields, skipProperties);

				if (type.ChangeName(nameIterator.Next(), iSkipTypes.ToArray()))
				{
					renamedTypes.AppendLine(type.Changes);
					renamedTypes.AppendLine(typeR);
				} else
				{
					skippedTypes.AppendLine(type.Changes);
					skippedTypes.AppendLine(typeR);
				}
			}

			var result = new StringBuilder();
			result.AppendLine(skippedTypes.ToString());
			result.AppendLine(renamedTypes.ToString());

			return result.ToString();
		}
	}


}
