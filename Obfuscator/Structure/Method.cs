using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
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

			//var writeLineMethod = typeof(System.Console).GetMethod("WriteLine", new System.Type[] { typeof(string) });
			//var writeLineRef = assembly.Import(writeLineMethod);
			//method.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldstr, "Inject!"));
			//// Вызываем метод Console.WriteLine, параметры он берет со стека - в данном случае строку "Injected".
			//method.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Call, writeLineRef));

			//obfuscate const

			if (!method.IsConstructor)
			{
				//List<Instruction> prefix = new List<Instruction>();
				//List<Instruction> suffix = new List<Instruction>();
				//var newBody = GetSwitch(ref prefix, ref suffix);
				//var prefix = method.Body.Instructions.Take(6).ToList();
				//var suffix = method.Body.Instructions.Last();
				//method.Body.Instructions.Insert(0, s);
				//method.Body.Instructions.Clear();
				//foreach (var ins in prefix)
				//{
				//	method.Body.Instructions.Add(ins);
				//}
				//var firstIns = method.Body.Instructions.First();
				//var lastIns = method.Body.Instructions.Last();
				//var proc = method.Body.GetILProcessor();

				//foreach (var ins in prefix)
				//{
				//	proc.InsertBefore(firstIns, ins);
				//}
				//foreach (var ins in suffix)
				//{
				//	proc.InsertAfter(lastIns, ins);
				//}
				////method.Body.Instructions.Add(suffix);
				//proc.Remove(lastIns);
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
		public static Random rand = new Random();

		public ICollection<Instruction> GetSwitch(ref List<Instruction> prefix, ref List<Instruction> suffix)
		{
			var result = prefix;

			var switchEl = rand.Next(5412);
			var offset = switchEl - rand.Next(5);
			var endOfswitch = Instruction.Create(OpCodes.Nop);
			var defaultSwitch = Instruction.Create(OpCodes.Nop);

			result.Add(Instruction.Create(OpCodes.Ldc_I4, switchEl));
			result.Add(Instruction.Create(OpCodes.Ldc_I4, offset));
			result.Add(Instruction.Create(OpCodes.Sub));

			var labels = new Instruction[5];
			for(int i = 0; i < labels.Length; i++)
			{
				labels[i] = Instruction.Create(OpCodes.Nop);
			}

			result.Add(Instruction.Create(OpCodes.Switch, labels));
			result.Add(Instruction.Create(OpCodes.Br_S, defaultSwitch));

			for (var i = 0; i < labels.Length; i++)
			{
				var label = labels[i];
				result.Add(label);

				if (i == switchEl - offset)
				{
					result = suffix;
				}
				result.Add(Instruction.Create(OpCodes.Br_S, endOfswitch));
			}

			result.Add(defaultSwitch);
			//result.AddRange(instructions);
			result.Add(Instruction.Create(OpCodes.Br_S, endOfswitch));
			result.Add(endOfswitch);
			result.Add(Instruction.Create(OpCodes.Ret));
			return result;
		}
	}
}
