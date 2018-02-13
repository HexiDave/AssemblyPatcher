using dnlib.DotNet;

namespace BootstrapLib
{
    /// <summary>
    /// Base interface for all static patch libraries
    /// </summary>
    public interface IPatch
    {
        /// <summary>
        /// Entry point for all static patch libraries
        /// </summary>
        /// <param name="module">Loaded assembly module to be patched</param>
        void InitializePatch(ModuleDefMD module);
    }
}
