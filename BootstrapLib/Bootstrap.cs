using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace BootstrapLib
{
    public class Bootstrap
    {
        /// <summary>
        /// Get the path to the managed assembly directory
        /// </summary>
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

        /// <summary>
        /// This handler will resolve the Mod files' dependencies into the main managed assembly directory,
        /// instead of their own local folders.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        static Assembly ModResolveHandler(object sender, ResolveEventArgs args)
        {
            // TODO: Make sure if a mod needs its own assemblies locally they can be loaded - needs testing

            string dllName = args.Name.Split(new[] { ','}, StringSplitOptions.None)[0];

            return Assembly.LoadFile(AssemblyDirectory + dllName + ".dll");
        }

        public static void LoadModAssemblies()
        {
            // Make sure we resolve the DLL dependencies locally
            AppDomain.CurrentDomain.AssemblyResolve += resolveEventHandler;

            // Find all the Mod assemblies - in the future, might use a table of contents file
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

        /// <summary>
        /// Created for re-use between IMod and IPatch
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<Type> FindAllModsOf<T>()
        {
            List<Type> modsList = new List<Type>();

            // Search through all the assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.GetTypes();
                    var checkModTypes = types.Where(t => typeof(T).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);
                    modsList.AddRange(checkModTypes);
                }
                catch
                {
                    // TODO: We're swallowing this exception for now - revisit
                }
            }

            return modsList;
        }

        /// <summary>
        /// This is the mod loading entry point.
        /// It will load all mods and attach them to the game
        /// </summary>
        static void Initialize()
        {
            Debug.Log("Bootstrap Initialize");

            // Load the mod files
            LoadModAssemblies();

            // Get all the mods
            var modTypes = FindAllModsOf<IMod>();

            // Initialize each mod
            foreach (var modType in modTypes)
            {
                var mod = (IMod)Activator.CreateInstance(modType);
                try
                {
                    mod.InitializeMod();
                    mods.Add(mod);
                } catch (Exception e)
                {
                    Debug.Log("Failed to instantiate and initialize mod: " + modType.FullName);
                    Debug.Log(e.StackTrace);
                }
            }
        }

        private static ResolveEventHandler resolveEventHandler = new ResolveEventHandler(ModResolveHandler);

        private static List<IMod> mods = new List<IMod>();
    }
}
