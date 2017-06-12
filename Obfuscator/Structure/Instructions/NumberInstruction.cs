using Mono.Cecil.Cil;
using System;

namespace Obfuscator.Structure.Instrucitons
{
	public class NumberInstruction<Num> where Num : struct
	{
		private Instruction instruction;
		private ILProcessor processor;

		public Num Number
		{
			get
			{
				if (instruction.OpCode.Equals(OpCodes.Ldc_I4_8))
				{
					return (Num)(object)(sbyte)8;
				}
				if (instruction.OpCode.Equals(OpCodes.Ldc_I4_7))
				{
					return (Num)(object)(sbyte)7;
				}
				if (instruction.OpCode.Equals(OpCodes.Ldc_I4_6))
				{
					return (Num)(object)(sbyte)6;
				}
				if (instruction.OpCode.Equals(OpCodes.Ldc_I4_5))
				{
					return (Num)(object)(sbyte)5;
				}
				if (instruction.OpCode.Equals(OpCodes.Ldc_I4_4))
				{
					return (Num)(object)(sbyte)4;
				}
				if (instruction.OpCode.Equals(OpCodes.Ldc_I4_3))
				{
					return (Num)(object)(sbyte)3;
				}
				if (instruction.OpCode.Equals(OpCodes.Ldc_I4_2))
				{
					return (Num)(object)(sbyte)2;
				}
				if (instruction.OpCode.Equals(OpCodes.Ldc_I4_1))
				{
					return (Num)(object)(sbyte)1;
				}
				if (instruction.OpCode.Equals(OpCodes.Ldc_I4_0))
				{
					return (Num)(object)(sbyte)0;
				}
				if (instruction.OpCode.Equals(OpCodes.Ldc_I4_M1))
				{
					return (Num)(object)(sbyte)-1;
				}
				return (Num)instruction.Operand;
			}
			set { instruction.Operand = value; }
		}

		public NumberInstruction(Instruction instruction, ILProcessor processor)
		{
			this.instruction = instruction;
			this.processor = processor;
		}

		public static NumberInstruction<Num> GetInstructionWrapper(Instruction instruction, ILProcessor processor)
		{
			return new NumberInstruction<Num>(instruction, processor);
		}

		public void ReplaceNumberWithInstruction(Instruction to, Instruction after)
		{
			processor.InsertAfter(instruction, after);
			processor.ReplaceInstruction(instruction, to);
		}
	}
}
