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
	    private List<MethodReference> references = new List<MethodReference>();
		private MethodGroup _group;
        public static Random rand = new Random();

        public string Changes { get; private set; }

	    public bool IsObfuscated { get; private set; }

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


            //obfuscate const
		    var proc = method.Body.GetILProcessor();
            proc.ChangeAllOpcodeToAnother(OpCodes.Beq_S, OpCodes.Beq);
            proc.ChangeAllOpcodeToAnother(OpCodes.Bge_S, OpCodes.Bge);
            proc.ChangeAllOpcodeToAnother(OpCodes.Bgt_S, OpCodes.Bgt);
            proc.ChangeAllOpcodeToAnother(OpCodes.Bgt_Un_S, OpCodes.Bgt_Un);
            proc.ChangeAllOpcodeToAnother(OpCodes.Ble_S, OpCodes.Ble);
            proc.ChangeAllOpcodeToAnother(OpCodes.Ble_Un_S, OpCodes.Ble_Un);
            proc.ChangeAllOpcodeToAnother(OpCodes.Blt_S, OpCodes.Blt);
            proc.ChangeAllOpcodeToAnother(OpCodes.Blt_Un_S, OpCodes.Blt);
            proc.ChangeAllOpcodeToAnother(OpCodes.Bne_Un_S, OpCodes.Bne_Un);
            proc.ChangeAllOpcodeToAnother(OpCodes.Br_S, OpCodes.Br);
            proc.ChangeAllOpcodeToAnother(OpCodes.Brfalse_S, OpCodes.Brfalse);
            proc.ChangeAllOpcodeToAnother(OpCodes.Brtrue_S, OpCodes.Brtrue);

            foreach (var instruction in method.Body.Instructions)
			{
				project.RegistrateInstruction(instruction);
			}
		}

		public bool ChangeName(string name)
		{
			if (!IsObfuscated)
			{
				Changes = definition.Name;
			}

			if (Group.Methods.Any(m => !m.WillChanged()))
			{
				return false;
			}

			foreach (var m in Group.Methods)
			{
				m.Changes = m.definition.Name;
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
				m.IsObfuscated = true;

                if (!m.definition.IsConstructor && m.definition.HasBody)
                {
                    var newBody = GetSwitch(m.definition.Body.Instructions);

                    m.definition.Body.Instructions.Clear();

                    foreach (var ins in newBody)
                    {
                        m.definition.Body.Instructions.Add(ins);
                    }
                }

                m.Changes += " -> " + name;
			}
			return true;
		}

		public bool WillChanged()
		{
			return !IsObfuscated && project.Assemblies.Any(a => a.Name == assembly.Name) && !definition.IsConstructor && !assembly.SkipMethods.Any(r => r.IsMethodSkip(definition));
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

	    public IEnumerable<StringInstruction> GetStringInstructions()
	    {
            if (!definition.HasBody) return new StringInstruction[0];
            var proc =  definition.Body.GetILProcessor();

            return definition.Body.Instructions.Where(i => i.OpCode.Equals(OpCodes.Ldstr)).Select(i => StringInstruction.GetInstructionWrapper(i, proc)).ToList();
	    }
    }
}
