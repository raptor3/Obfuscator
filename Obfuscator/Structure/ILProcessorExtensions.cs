using System;
using System.Linq;
using Mono.Cecil.Cil;

namespace Obfuscator.Structure
{
    public static class ILProcessorExtensions
    {
        public static void ChangeAllOpcodeToAnother(this ILProcessor processor, OpCode target, OpCode replacer)
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
            var br = processor.Body.Instructions.Where(i => i.OpCode.Equals(target)).ToList();
            foreach (var instruction in br)
            {
                ReplaceInstruction(processor, instruction, Instruction.Create(replacer, instruction.Operand as Instruction));
            }
        }

        public static void ReplaceInstruction(this ILProcessor processor, Instruction from, Instruction to)
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
