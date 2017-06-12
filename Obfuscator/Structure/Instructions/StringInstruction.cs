using System;
using Mono.Cecil.Cil;

namespace Obfuscator.Structure.Instrucitons
{
	public class StringInstruction
	{
		private Instruction instruction;
		private ILProcessor processor;

		public string String
		{
			get { return (string)instruction.Operand; }
			set { instruction.Operand = value; }
		}

		public StringInstruction(Instruction instruction, ILProcessor processor)
		{
			if (instruction == null)
				throw new ArgumentNullException("value");
			if (instruction.OpCode.OperandType != OperandType.InlineString)
				throw new ArgumentException("opcode");
			this.instruction = instruction;
			this.processor = processor;
		}

		public static StringInstruction GetInstructionWrapper(Instruction instruction, ILProcessor processor)
		{
			return new StringInstruction(instruction, processor);
		}

		public void ReplaceStringWithInstruction(Instruction to)
		{
			processor.ReplaceInstruction(instruction, to);
		}
	}
}
