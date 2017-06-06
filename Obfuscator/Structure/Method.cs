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

		public Method(Project project, Assembly assembly, MethodDefinition stringHider)
		{
			this.project = project;
			this.assembly = assembly;
		    this.stringHider = stringHider;
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

                if (!m.definition.IsConstructor && m.definition.HasBody)
                {
                    var newBody = GetSwitch(m.definition.Body.Instructions);

                    m.definition.Body.Instructions.Clear();

                    foreach (var ins in newBody)
                    {
                        m.definition.Body.Instructions.Add(ins);
                    }
                }

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
	    private MethodDefinition stringHider;

	    public ICollection<Instruction> GetSwitch(IEnumerable<Instruction> instructions)
		{
			var result = new List<Instruction>();

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
			result.Add(Instruction.Create(OpCodes.Br, defaultSwitch));

			for (var i = 0; i < labels.Length; i++)
			{
				var label = labels[i];
				result.Add(label);

				//if (i == switchEl - offset)
				{
					result.AddRange(instructions);
				}
				result.Add(Instruction.Create(OpCodes.Br, endOfswitch));
			}

			result.Add(defaultSwitch);
			result.AddRange(instructions);
			result.Add(Instruction.Create(OpCodes.Br, endOfswitch));
			result.Add(endOfswitch);
			result.Add(Instruction.Create(OpCodes.Ret));
			return result;
		}

	    public void HideStrings()
	    {
            if (!definition.HasBody) return;
            var proc =  definition.Body.GetILProcessor();

	        ChangeAllOpcodeToAnother(OpCodes.Beq_S, OpCodes.Beq);
	        ChangeAllOpcodeToAnother(OpCodes.Bge_S, OpCodes.Bge);
	        ChangeAllOpcodeToAnother(OpCodes.Bgt_S, OpCodes.Bgt);
	        ChangeAllOpcodeToAnother(OpCodes.Bgt_Un_S, OpCodes.Bgt_Un);
	        ChangeAllOpcodeToAnother(OpCodes.Ble_S, OpCodes.Ble);
	        ChangeAllOpcodeToAnother(OpCodes.Ble_Un_S, OpCodes.Ble_Un);
	        ChangeAllOpcodeToAnother(OpCodes.Blt_S, OpCodes.Blt);
	        ChangeAllOpcodeToAnother(OpCodes.Blt_Un_S, OpCodes.Blt);
	        ChangeAllOpcodeToAnother(OpCodes.Bne_Un_S, OpCodes.Bne_Un);
            ChangeAllOpcodeToAnother(OpCodes.Br_S, OpCodes.Br);
            ChangeAllOpcodeToAnother(OpCodes.Brfalse_S, OpCodes.Brfalse);
            ChangeAllOpcodeToAnother(OpCodes.Brtrue_S, OpCodes.Brtrue);

            var instructions = definition.Body.Instructions.Where(i => i.OpCode.Equals(OpCodes.Ldstr)).ToList();
            foreach (var instruction in instructions)
            {
                    proc.InsertAfter(instruction, Instruction.Create(OpCodes.Call, stringHider.GetElementMethod()));
            }
	        definition.Resolve();
	    }

	    private void ChangeAllOpcodeToAnother(OpCode target, OpCode replacer)
	    {
	        if (target.OperandType != OperandType.InlineBrTarget &&
                target.OperandType != OperandType.ShortInlineBrTarget)
	        {
	            throw new ArgumentException("opcode");
	        }
            if (replacer.OperandType != OperandType.InlineBrTarget &&
                replacer.OperandType != OperandType.ShortInlineBrTarget)
            {
                throw new ArgumentException("opcode");
            }
            var br = definition.Body.Instructions.Where(i => i.OpCode.Equals(target)).ToList();
            foreach (var instruction in br)
            {
                ReplaceInstruction(definition.Body.GetILProcessor(), instruction, Instruction.Create(replacer, instruction.Operand as Instruction));
            }
        }

        public static void ReplaceInstruction(ILProcessor processor, Instruction from, Instruction to)
        {
            foreach (var item in processor.Body.Instructions)
            {
                var operInstruction = item.Operand as Instruction;
                if (operInstruction != null && operInstruction == from)
                {
                    item.Operand = to;
                }
            }

            processor.Replace(from, to);
        }
    }
}
