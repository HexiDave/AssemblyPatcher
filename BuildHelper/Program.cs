using System;
using System.Linq;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Xml;

/**
 * This program is designed to seek out the Steam install of Subnautica for the Managed Assemblies directory. 
 * If found, it will then update the Visual Studio solution's build system to use it instead of manually updating it.
 */

namespace BuildHelper
{
    class Program
    {
        /// <summary>
        /// Magic numbers are bad - let's have some exit codes
        /// </summary>
        enum ExitCode
        {
            Success = 0,

            // Just reporting info, not really an error
            Formal,

            Error = -1
        }

        static int Main(string[] args)
        {
            bool waitBeforeExit = true;

            // Check for arguments
            if (args != null && args.Length != 0)
            {
                switch(args[0].ToLower())
                {
                    // Wanna know what this program does
                    case "/?":
                        Console.WriteLine("Finds the Subnautica folder and updates SubnauticaPath.targets with it's location.");
                        Console.WriteLine("\n\nUse /n or /nowait to skip the wait on completion");
                        return (int)ExitCode.Formal;
                    case "/n":
                    case "/nowait":
                        // Useful for automation
                        waitBeforeExit = false;
                        break;
                    default:
                        Console.WriteLine(string.Format("Unknown argument: {0}", args[0]));
                        Console.WriteLine("Aborting...");

                        return (int)ExitCode.Error;
                }
            }

            // Let's go!
            bool result = Start();

            Console.Write("\nPress any key to continue.");

            // Just in case this is launched from outside a console - give the user time to see what happened
            if (waitBeforeExit)
            {
                Console.ReadKey(true);
            }

            return (int)(result ? ExitCode.Success : ExitCode.Error);
        }

        /// <summary>
        /// Start searching for the Subnautica install to update the ManagedPath.targets path
        /// </summary>
        static bool Start()
        {
            string steamPath;
            try
            {
                // Find Steam
                steamPath = GetSteamInstallPath();
            }
            catch
            {
                // Busted install or security problem? Either way, we're not being helpful today.
                Console.WriteLine("Failed to get the Steam install path from the registry - Aborting.");
                return false;
            }

            // Try the standard path at the install location
            string subnauticaPath = GetSubnauticaPath(steamPath);

            // Not in the normal place
            if (!Directory.Exists(subnauticaPath))
            {
                // Time to get fancy...
                subnauticaPath = SearchInLibrariesForPath(steamPath);
            }

            // Found a valid path
            if (subnauticaPath != null)
            {
                // Update the SubnauticaPath.targets file
                UpdatePathTargets(subnauticaPath);
                return true;
            }

            // Maybe some other type of Subnautica install or another issue.
            Console.WriteLine("Failed to find a valid Subnautica install. You're on your own!");

            return false;
        }

        /// <summary>
        /// Read the user Windows registry for the steam install
        /// </summary>
        /// <returns></returns>
        static string GetSteamInstallPath()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
            {
                return key.GetValue("SteamPath") as string;
            }
        }

        /// <summary>
        /// Utility to get the Subnautica install path from a Steam library path
        /// </summary>
        /// <param name="libraryPath">Steam library path</param>
        /// <returns></returns>
        static string GetSubnauticaPath(string libraryPath)
        {
            return Path.GetFullPath(libraryPath + @"\steamapps\common\Subnautica");
        }

        /// <summary>
        /// Search inside [Steam install]\steamapps\libraryfolders.vdf for library folders
        /// </summary>
        /// <param name="steamPath">Steam install path</param>
        /// <returns>The Subnautica managed path</returns>
        static string SearchInLibrariesForPath(string steamPath)
        {
            /*

            This is an example of the libraryfolders.vdf that containts the locations of Steam libraries

            0. "LibraryFolders"
            1. {
	        2.    "TimeNextStatsReport"		"1518738831"
	        3.     "ContentStatsID"		"-8745987195671263442"
	        4.     "1"		"D:\\SteamLibrary"
	        5.     "2"		"F:\\SteamLibrary"
            6. }


            */

            // Regex to match against the path string
            var pathRegex = new Regex(@"^\s+""\d+""\s+""([^""]*)""");

            // Read in the whole library file and search for the paths
            var libraryFileLines = File.ReadAllLines(steamPath + @"\steamapps\libraryfolders.vdf");
            var libraryPaths = libraryFileLines
                .Where((line, index) => index >= 4 && index < libraryFileLines.Length - 1) // Filter between index [4, -1]
                .Select(line =>
                {
                    var match = pathRegex.Match(line);

                    // The first Groups item has the whole line, we want index 1 which has our path match group
                    return match.Groups[1].Value;
                })
                .ToArray();

            // Search through all the paths for Subnautica
            foreach (var libraryPath in libraryPaths)
            {
                string subnauticaPath = GetSubnauticaPath(libraryPath);

                if (Directory.Exists(subnauticaPath))
                {
                    // Found it!
                    return subnauticaPath;
                }
            }

            // No luck...
            return null;
        }

        /// <summary>
        /// Update the [SolutionDir]\SubnauticaPath.targets file's &lt;SubnauticaPath&gt; location
        /// </summary>
        /// <param name="subnauticaPath">Subnautica's path</param>
        static void UpdatePathTargets(string subnauticaPath)
        {
            Console.WriteLine(string.Format("Updating SubnauticaPath.targets with: {0}", subnauticaPath));

            try
            {
                // Process it as an XML file
                string localPath = Directory.GetCurrentDirectory();
                string templateTargetsPath = localPath + @"\SubnauticaPath.targets.template";
                string outputTargetsPath = localPath + @"\SubnauticaPath.targets";

                var subnauticaTargetsDocument = new XmlDocument();
                subnauticaTargetsDocument.Load(templateTargetsPath);

                var pathNode = subnauticaTargetsDocument.GetElementsByTagName("SubnauticaPath").Item(0);

                // Clear the existing contents and put the new path in
                pathNode.RemoveAll();
                pathNode.AppendChild(subnauticaTargetsDocument.CreateTextNode(subnauticaPath));

                // Save
                subnauticaTargetsDocument.Save(outputTargetsPath);
            }
            catch (Exception e)
            {
                Console.WriteLine("Encountered an error while updating the SubnauticaPath.targets file:");
                Console.WriteLine(e.StackTrace);
                return;
            }

            // Success!
            Console.WriteLine("SubnauticaPath.targets updated!");
        }
    }
}
