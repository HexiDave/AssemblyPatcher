using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using BootstrapLib;
using System.Reflection;

namespace AssemblyPatcher
{
    class Program
    {
        static readonly string ManagedPath = @".\Subnautica_Data\Managed\";
        static readonly string MainAssemblyName = "Assembly-CSharp";
        static readonly string MainAssemblyFileName = MainAssemblyName + ".dll";
        static readonly string OriginalMainAssemblyFileName = GetOriginalFileName(MainAssemblyFileName);
        static Assembly MainAssembly;

        /// <summary>
        /// Get the path to the managed assembly directory. 
        /// NOTE: This is duplicated here because BootstrapLib would need to be loaded which would cause an infinite loop of loading.
        /// </summary>
        static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        static string GetOriginalFileName(string assemblyName)
        {
            return assemblyName + ".original";
        }

        static void PrepareAssemblyBackup(string assemblyName, string assemblyBackupName)
        {
            string originalAssemblyPath = ManagedPath + assemblyBackupName;
            string assemblyPath = ManagedPath + assemblyName;

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
            AppDomain.CurrentDomain.AssemblyResolve += Patcher_AssemblyResolver;

            try
            {
                PatchMainAssembly();
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to patch the main assembly file:\n");
                Console.WriteLine(e.Message);
                Console.Write(e.StackTrace);
                Console.ReadKey(true);
                return;
            }

            Console.Write("Patching complete!");
            Console.ReadKey(true);
        }

        static Assembly Patcher_AssemblyResolver(object sender, ResolveEventArgs args)
        {
            string assemblyName = args.Name.Split(new[] { ','}, StringSplitOptions.None)[0];

            if (assemblyName == "Assembly-CSharp")
            {
                return MainAssembly;
            }

            var assemblyPath = AssemblyDirectory + ManagedPath + assemblyName + ".dll";

            bool exists = File.Exists(assemblyPath);

            return Assembly.LoadFile(assemblyPath);
        }

        static void PatchMainAssembly()
        {
            PrepareAssemblyBackup(MainAssemblyFileName, OriginalMainAssemblyFileName);

            // Manually load the assembly to avoid needing to overwrite a loaded assembly
            var assemblyData = File.ReadAllBytes(ManagedPath + OriginalMainAssemblyFileName);

            MainAssembly = Assembly.Load(assemblyData);

            // Load the assembly file to be patched
            var assemblyModule = ModuleDefMD.Load(assemblyData);

            // Find the GameInput.Awake() method to use as a launcher for the mod bootstrapper
            var gameInputAwake = assemblyModule.GetTypes().Single(s => s.Name == "GameInput").FindMethod("Awake");

            // Initialize the Importer and grab the BootstrapLib.Bootstrap.Initialize() method - which collects and starts the mods
            Importer importer = new Importer(assemblyModule);
            ITypeDefOrRef bootstraperRef = importer.Import(typeof(BootstrapLib.Bootstrap));
            IMethod bootstrapInitialize = importer.Import(typeof(BootstrapLib.Bootstrap).GetMethod("Initialize", BindingFlags.Static | BindingFlags.NonPublic));

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

            Console.WriteLine("Starting to patch individual mods...");
            PatchMods(assemblyModule);

            // Same the assembly - overwriting Assembly-CSharp.dll and leaving the .original backup pristine
            assemblyModule.Write(ManagedPath + MainAssemblyFileName);
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
                    patch.InitializePatch(module);
                    Console.WriteLine("Successfully installed patch for " + modType.FullName);
                } catch (Exception e)
                {
                    Console.WriteLine("Failed to patch: " + modType.FullName);
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
        }
    }
}
