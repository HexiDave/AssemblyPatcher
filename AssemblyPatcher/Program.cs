using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using BootstrapLib;

namespace AssemblyPatcher
{
    class Program
    {
        static string MainAssemblyName = "Assembly-CSharp.dll";
        static string OriginalMainAssemblyName = GetOriginalName(MainAssemblyName);

        public static string GetOriginalName(string assemblyName)
        {
            return assemblyName + ".original";
        }

        public static void PrepareAssemblyBackup(string assemblyName, string assemblyBackupName)
        {
            string originalAssemblyPath = @".\" + assemblyBackupName;
            string assemblyPath = @".\" + assemblyName;

            if (!File.Exists(originalAssemblyPath))
            {
                if (!File.Exists(assemblyPath))
                {
                    // Uh oh, no version to patch with
                    throw new FileNotFoundException(string.Format("Failed to patch: No {0} or {1} found.", assemblyName, assemblyBackupName));
                }

                // Make a backup to work from
                File.Copy(assemblyPath, originalAssemblyPath);
            }
        }

        static void Main(string[] args)
        {
            PatchMainAssembly();
        }

        static void PatchMainAssembly()
        {
            PrepareAssemblyBackup(MainAssemblyName, OriginalMainAssemblyName);

            // Load the assembly file to be patched
            var assemblyModule = ModuleDefMD.Load(OriginalMainAssemblyName);

            // Find the GameInput.Awake() method to use as a launcher for the mod bootstrapper
            var gameInputAwake = assemblyModule.GetTypes().Single(s => s.Name == "GameInput").FindMethod("Awake");

            // Initialize the Importer and grab the BootstrapLib.Bootstrap.Initialize() method - which collects and starts the mods
            Importer importer = new Importer(assemblyModule);
            ITypeDefOrRef bootstraperRef = importer.Import(typeof(BootstrapLib.Bootstrap));
            IMethod bootstrapInitialize = importer.Import(typeof(BootstrapLib.Bootstrap).GetMethod("Initialize", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));

            // Find the IL code instruction point to insert after:
            /*
            private void Awake()
            {
	            if (GameInput.instance != null)
	            {
		            UnityEngine.Object.Destroy(base.gameObject);
		            return;
	            }
	            GameInput.instance = this;
                
            --> Bootstrap.Initialize(); // This is what is inserted

	            GameInput.instance.Initialize();
	            for (int i = 0; i < GameInput.numDevices; i++)
	            {
		            GameInput.SetupDefaultBindings((GameInput.Device)i);
	            }
            }
            */
            var instructions = gameInputAwake.Body.Instructions;
            var index = instructions.IndexOf(instructions.First(i => i.OpCode.Code == Code.Stsfld)) + 1;

            // Create the call instruction to fire the Bootstrap.Initialize() function
            instructions.Insert(index, OpCodes.Call.ToInstruction(bootstrapInitialize));

            PatchMods(assemblyModule);

            // Same the assembly - overwriting Assembly-CSharp.dll and leaving the .original backup pristine
            assemblyModule.Write(MainAssemblyName);
        }

        static void PatchMods(ModuleDefMD module)
        {
            Bootstrap.LoadModAssemblies();

            var modTypes = Bootstrap.FindAllModsOf<IPatch>();

            foreach (var modType in modTypes)
            {
                try
                {
                    var patch = (IPatch)Activator.CreateInstance(modType);
                    if (patch.InitializePatch(module))
                    {
                        //Debug.Log("Successfully installed patch for " + modType.FullName);
                    }

                } catch (Exception e)
                {
                    //Debug.Log("Failed to patch: " + modType.FullName);
                }
            }
        }
    }
}
