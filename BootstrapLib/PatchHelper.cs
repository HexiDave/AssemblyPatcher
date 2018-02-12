using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BootstrapLib
{
    public static class PatchHelper
    {
        public static void ReplaceCall(MethodDef parentMethod, IMethod methodToReplace, IMethod methodToReplaceWith)
        {
            var instructions = parentMethod.Body.Instructions;

            var replaceIndices = instructions
                .Select((instruction, index) => new { instruction, index })
                .Where(x => x.instruction.OpCode == OpCodes.Call && new SigComparer().Equals(x.instruction.Operand as IMethodDefOrRef, methodToReplace))
                .Select(x => x.index)
                .ToArray();

            foreach (var index in replaceIndices)
            {
                instructions[index] = OpCodes.Call.ToInstruction(methodToReplaceWith);
            }
        }
    }
}
