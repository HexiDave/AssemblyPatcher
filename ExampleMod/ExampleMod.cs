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
        public bool InitializePatch(ModuleDefMD module)
        {
            

            return true;
        }

        
    }
}
