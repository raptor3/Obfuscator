using Mono.Cecil;
using Mono.Cecil.Cil;
using Obfuscator.Structure.Instrucitons;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

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
		public bool IsSecured { get; private set; }

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

			foreach (var attr in method.CustomAttributes)
			{
				project.RegistrateReference(attr.AttributeType);
			}

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
			//result.Add(Instruction.Create(OpCodes.Ldc_I4, (switchEl)));
			result.Add(Instruction.Create(OpCodes.Ldc_I4, HiderClass.ReverseInt(switchEl)));
			
			result.Add(Instruction.Create(OpCodes.Call, assembly.Hider.BaseGetInt));
			result.Add(Instruction.Create(OpCodes.Ldc_I4, offset));
			result.Add(Instruction.Create(OpCodes.Sub));

			var labels = new Instruction[5];
			for (int i = 0; i < labels.Length; i++)
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
			var proc = definition.Body.GetILProcessor();

			return definition.Body.Instructions.Where(i => i.OpCode.Equals(OpCodes.Ldstr)).Select(i => StringInstruction.GetInstructionWrapper(i, proc)).ToList();
		}

		public IEnumerable<NumberInstruction<double>> GetDoubleInstructions()
		{
			if (!definition.HasBody || !WillChanged()) return new NumberInstruction<double>[0];
			var proc = definition.Body.GetILProcessor();

			return definition.Body.Instructions.Where(i => i.OpCode.Equals(OpCodes.Ldc_R8)).Select(i => NumberInstruction<double>.GetInstructionWrapper(i, proc)).ToList();
		}

		public IEnumerable<NumberInstruction<float>> GetFloatInstructions()
		{
			if (!definition.HasBody || !WillChanged()) return new NumberInstruction<float>[0];
			var proc = definition.Body.GetILProcessor();

			return definition.Body.Instructions.Where(i => i.OpCode.Equals(OpCodes.Ldc_R4)).Select(i => NumberInstruction<float>.GetInstructionWrapper(i, proc)).ToList();
		}

		public IEnumerable<NumberInstruction<int>> GetIntInstructions()
		{
			if (!definition.HasBody || !WillChanged()) return new NumberInstruction<int>[0];
			var proc = definition.Body.GetILProcessor();

			return definition.Body.Instructions.Where(i => i.OpCode.Equals(OpCodes.Ldc_I4)).Select(i => NumberInstruction<int>.GetInstructionWrapper(i, proc)).ToList();
		}

		public IEnumerable<NumberInstruction<long>> GetLongInstructions()
		{
			if (!definition.HasBody || !WillChanged()) return new NumberInstruction<long>[0];
			var proc = definition.Body.GetILProcessor();

			return definition.Body.Instructions.Where(i => i.OpCode.Equals(OpCodes.Ldc_I8)).Select(i => NumberInstruction<long>.GetInstructionWrapper(i, proc)).ToList();
		}

		public IEnumerable<NumberInstruction<sbyte>> GetShortInstructions()
		{
			if (!definition.HasBody || !WillChanged()) return new NumberInstruction<sbyte>[0];
			var proc = definition.Body.GetILProcessor();
			var opcodes = new[]
			{
				OpCodes.Ldc_I4_S,
				OpCodes.Ldc_I4_8,
				OpCodes.Ldc_I4_7,
				OpCodes.Ldc_I4_4,
				OpCodes.Ldc_I4_3,
				OpCodes.Ldc_I4_2,
				OpCodes.Ldc_I4_1,
				OpCodes.Ldc_I4_0,
				OpCodes.Ldc_I4_M1,
			};

			return definition.Body.Instructions.Where(i => opcodes.Any(o => o.Equals(i.OpCode))).Select(i => NumberInstruction<sbyte>.GetInstructionWrapper(i, proc)).ToList();
		}

		public int AddEntryPointSecurity()
		{

			IsSecured = true;

			return 0;
		}

		public void AddSecurity()
		{
			if (IsSecured) return;

			var assType = typeof(System.Reflection.Assembly);
			
			if (false && definition.HasBody)
			{
				var encodingType = typeof(Encoding);
				var getUtf8 = assembly.Import(encodingType.GetMethod("get_UTF8"));
				var getString = assembly.Import(encodingType.GetMethod("GetString", new[] { typeof(byte[]), typeof(int), typeof(int) }));

				var body = definition.Body;
				body.InitLocals = true;
				var arrayVar = new VariableDefinition(assembly.Import(typeof(byte[])));
				var flagVar = new VariableDefinition(assembly.TypeSystem.Boolean);

				body.Variables.Add(arrayVar);
				body.Variables.Add(flagVar);

				var firstInstr = body.Instructions.First();
				var proc = body.GetILProcessor();

				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Nop));
				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Call, assembly.Import(assType.GetMethod("GetExecutingAssembly"))));
				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Callvirt, assembly.Import(assType.GetProperty("EntryPoint").GetGetMethod())));
				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Callvirt, assembly.Import(typeof(MethodInfo).GetMethod("GetMethodBody"))));
				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Callvirt, assembly.Import(typeof(System.Reflection.MethodBody).GetMethod("GetILAsByteArray"))));
				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Stloc_S, arrayVar));

				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Call, getUtf8));
				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Ldloc_S, arrayVar));
				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Ldc_I4_0));
				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Ldloc_S, arrayVar));
				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Ldlen));
				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Conv_I4));

				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Callvirt, getString));
				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Callvirt, assembly.Import(typeof(object).GetMethod("GetHashCode"))));
				var entryHash = Instruction.Create(OpCodes.Ldc_I4, assembly.EntryHashCode);
				proc.InsertBefore(firstInstr, entryHash);
				assembly.EntryHash.Add(entryHash);
				//proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Ldc_I4, HiderClass.ReverseInt(assembly.EntryHashCode)));
				//proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Call, assembly.Hider.BaseGetInt));
				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Ceq));
				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Ldc_I4_0));
				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Ceq));
				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Stloc_S, flagVar));
				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Ldloc_S, flagVar));
				var ret = Instruction.Create(OpCodes.Ret);
				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Brfalse, ret));
				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Nop));
				var stringInstruction = Instruction.Create(OpCodes.Ldstr, "Code was changed.");
				proc.InsertBefore(firstInstr, stringInstruction);
				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Newobj, assembly.Import(typeof(Exception).GetConstructor(new[] { typeof(string)}))));
				proc.InsertBefore(firstInstr, Instruction.Create(OpCodes.Throw));
				proc.InsertBefore(firstInstr, ret);

				//assembly.Hider.HideString(StringInstruction.GetInstructionWrapper(stringInstruction, proc));
			}

			IsSecured = true;
		}

	}

}
