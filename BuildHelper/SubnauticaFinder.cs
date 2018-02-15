using System;
using System.Linq;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Xml;

namespace BuildHelper
{
    public class SubnauticaFinder : EventLogger
    {
        public bool Start()
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
                LogEvent("Failed to get the Steam install path from the registry - Aborting.");
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
                return UpdatePathTargets(subnauticaPath);
            }

            // Maybe some other type of Subnautica install or another issue.
            LogEvent("Failed to find a valid Subnautica install. You're on your own!");

            return false;
        }

        /// <summary>
        /// Read the user Windows registry for the steam install
        /// </summary>
        /// <returns></returns>
        public string GetSteamInstallPath()
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
        public string GetSubnauticaPath(string libraryPath)
        {
            return Path.GetFullPath(libraryPath + @"\steamapps\common\Subnautica");
        }

        /// <summary>
        /// Search inside [Steam install]\steamapps\libraryfolders.vdf for library folders
        /// </summary>
        /// <param name="steamPath">Steam install path</param>
        /// <returns>The Subnautica managed path</returns>
        public string SearchInLibrariesForPath(string steamPath)
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
        /// <returns>Successful update</returns>
        public bool UpdatePathTargets(string subnauticaPath)
        {
            LogEvent(string.Format("Updating SubnauticaPath.targets with: {0}", subnauticaPath));

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
                LogEvent("Encountered an error while updating the SubnauticaPath.targets file:");
                LogEvent(e.StackTrace);
                return false;
            }

            // Success!
            LogEvent("SubnauticaPath.targets updated!");
            return true;
        }
    }
}
