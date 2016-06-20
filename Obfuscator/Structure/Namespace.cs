using Mono.Cecil;
using System.Collections.Generic;
using System;
using Obfuscator.Iterator;
using Obfuscator.SkipRules;
using System.Linq;

namespace Obfuscator.Structure
{
	public class Namespace
	{
		private Project project;
		private bool renamed;
		private string changeTo;
		private string name;
		private Dictionary<string, Type> types = new Dictionary<string, Type>();

		public Namespace(Project project, string name)
		{
			this.project = project;
			this.name = name;
		}

		public void RegisterReference(TypeReference type)
		{
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

		public void ChangeName(string name, params ISkipNamespace[] skipNamespaces)
		{
			if (skipNamespaces.Any(r => r.IsNamespaceSkip(name)))
			{
				return;
			}

			foreach (var type in types.Values)
			{
				type.ChangeNamespace(name);
			}
		}

		public void RunRules(INameIterator nameIterator, List<SkipNamespace> skipNamespaces, List<SkipType> skipTypes, List<SkipMethod> skipMethods, List<SkipField> skipFields, List<SkipProperty> skipProperties)
		{
			foreach (var type in types.Values)
			{
				type.RunRules(nameIterator, skipNamespaces, skipTypes, skipMethods, skipFields, skipProperties);
			}

			nameIterator.Reset();

			var iSkipTypes = new List<ISkipType>(skipTypes);
			iSkipTypes.AddRange(skipNamespaces);

			foreach (var type in types.Values)
			{
				type.ChangeName(nameIterator.Next(), iSkipTypes.ToArray());
			}
		}
	}


}
