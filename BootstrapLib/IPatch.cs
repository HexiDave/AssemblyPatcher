using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;

namespace BootstrapLib
{
    public interface IPatch
    {
        bool InitializePatch(ModuleDefMD module);
    }
}
