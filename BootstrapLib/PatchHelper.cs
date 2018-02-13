using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Linq;

namespace BootstrapLib
{
    public static class PatchHelper
    {
        /// <summary>
        /// Find and replace a basic 'call' instruction with a new one
        /// </summary>
        /// <param name="parentMethod">The method we're searching inside the instructions of</param>
        /// <param name="methodToReplace">The method call we want to replace</param>
        /// <param name="methodToReplaceWith">The method we're going to replace it with</param>
        public static void ReplaceCall(MethodDef parentMethod, IMethod methodToReplace, IMethod methodToReplaceWith)
        {
            var instructions = parentMethod.Body.Instructions;

            // Find all the indices of the instructions matching the method
            var replaceIndices = instructions
                .Select((instruction, index) => new { instruction, index })
                .Where(x => x.instruction.OpCode == OpCodes.Call && new SigComparer().Equals(x.instruction.Operand as IMethodDefOrRef, methodToReplace))
                .Select(x => x.index)
                .ToArray();

            // Do the replacement
            foreach (var index in replaceIndices)
            {
                instructions[index] = OpCodes.Call.ToInstruction(methodToReplaceWith);
            }
        }
    }
}
