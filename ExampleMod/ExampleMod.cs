using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BootstrapLib;
using dnlib.DotNet;

namespace ExampleMod
{
    public class ExampleMod : IPatch
    {
        /*public bool InitializeMod()
        {
            Debug.Log("Logging from the ExampleMod Initialize function");
            ErrorMessage.AddMessage("Hello from the Example mod!");
            return true;
        }*/

        public bool InitializePatch(ModuleDefMD module)
        {
            var gameInputButtonType = module.GetTypes().Single(s => s.Name == "GameInput").NestedTypes.Single(s => s.Name == "Button");

            var ts = gameInputButtonType.GetEnumUnderlyingType();

            FieldDef slot9Field = new FieldDefUser("Slot9", new FieldSig(new ValueTypeSig(gameInputButtonType)))
            {
                Constant = module.UpdateRowId(new ConstantUser(99, ts.ElementType)),
                Attributes = FieldAttributes.Literal | FieldAttributes.Static | FieldAttributes.HasDefault
            };

            gameInputButtonType.Fields.Add(slot9Field);

            return true;
        }
    }
}
