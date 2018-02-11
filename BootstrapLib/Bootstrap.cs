using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace BootstrapLib
{
    public class Bootstrap
    {
        static Bootstrap()
        {
            
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        static Assembly ModResolveHandler(object sender, ResolveEventArgs args)
        {
            string dllName = args.Name.Split(new[] { ','}, StringSplitOptions.None)[0];

            return Assembly.LoadFile(AssemblyDirectory + dllName + ".dll");
        }

        public static void LoadModAssemblies()
        {
            // Make sure we resolve the DLL dependencies locally
            AppDomain.CurrentDomain.AssemblyResolve += resolveEventHandler;

            var modFiles = Directory.GetFiles(AssemblyDirectory + "\\Mods", "*Mod*.dll", SearchOption.AllDirectories);

            foreach (var modFile in modFiles)
            {
                try
                {
                    Assembly.LoadFile(modFile);
                }
                catch (Exception e)
                {
                    // Failed to load potential mod
                    Debug.Log(e);
                }
            }

            // Cleanup
            AppDomain.CurrentDomain.AssemblyResolve -= resolveEventHandler;
        }

        public static List<Type> FindAllModsOf<T>()
        {
            List<Type> modsList = new List<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.GetTypes();
                    var checkModTypes = types.Where(t => typeof(T).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);
                    modsList.AddRange(checkModTypes);
                }
                catch (Exception e)
                {
                    //Debug.Log("Failed to check for mod types in: " + assembly.GetName());
                    //Debug.Log(e);
                }
            }

            return modsList;
        }

        static void Initialize()
        {
            Debug.Log("Bootstrap Initialize");

            LoadModAssemblies();

            var modTypes = FindAllModsOf<IMod>();

            foreach (var modType in modTypes)
            {
                var mod = (IMod)Activator.CreateInstance(modType);
                try
                {
                    if (mod.InitializeMod())
                    {
                        mods.Add(mod);
                    }
                } catch (Exception e)
                {
                    Debug.Log("Failed to instantiate and initialize mod: " + modType.FullName);
                }
            }
        }

        private static ResolveEventHandler resolveEventHandler = new ResolveEventHandler(ModResolveHandler);

        private static List<IMod> mods = new List<IMod>();
    }
}
