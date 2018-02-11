using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Xml;

namespace BuildHelper
{
    class Program
    {
        static readonly Regex PathRegex = new Regex(@"^\s+""\d+""\s+""([^""]*)""");

        static void Main(string[] args)
        {
            string steamPath;
            try
            {
                steamPath = GetSteamInstallPath();
            } catch
            {
                Console.Write("Failed to get the Steam install path from the registry - Aborting.");
                Console.ReadKey(true);
                return;
            }

            string subnauticaManagedPath = GetSubnauticaManagedPath(steamPath);
            if (Directory.Exists(subnauticaManagedPath))
            {
                BuildTargets(subnauticaManagedPath);
            }

            SearchInLibraries(steamPath);

            Console.Write("Failed to find a valid Subnautica install. You're on your own!");
            Console.ReadKey(true);
        }

        static string GetSteamInstallPath()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
            {
                return key.GetValue("SteamPath") as string;
            }
        }

        static string GetSubnauticaManagedPath(string libraryPath)
        {
            return Path.GetFullPath(libraryPath + @"\steamapps\common\Subnautica\Subnautica_Data\Managed");
        }

        static void SearchInLibraries(string steamPath)
        {
            var libraryFileLines = File.ReadAllLines(steamPath + @"\steamapps\libraryfolders.vdf");
            var libraryPaths = libraryFileLines
                .Where((line, index) => index > 3 && index < libraryFileLines.Length - 1)
                .Select(line =>
                {
                    var match = PathRegex.Match(line);

                    return match.Groups[1].Value;
                })
                .ToArray();

            foreach (var libraryPath in libraryPaths)
            {
                string subnauticaManagedPath = GetSubnauticaManagedPath(libraryPath);

                if (Directory.Exists(subnauticaManagedPath))
                {
                    BuildTargets(subnauticaManagedPath);
                }
            }
        }

        static void BuildTargets(string subnauticaManagedPath)
        {
            Console.WriteLine(string.Format("Found Subnautica's managed data {0}", subnauticaManagedPath));
            // Process XML file

            string commonTargetsPath = @".\Common.targets";
            var commonTargetsDocument = new XmlDocument();
            commonTargetsDocument.Load(commonTargetsPath);

            var pathNode = commonTargetsDocument.GetElementsByTagName("SubnauticaPath").Item(0);

            pathNode.RemoveAll();
            pathNode.AppendChild(commonTargetsDocument.CreateTextNode(subnauticaManagedPath));

            commonTargetsDocument.Save(commonTargetsPath);

            Console.Write("Targets file updated! Press any key to continue.");
            Console.ReadKey(true);
            Environment.Exit(0);            
        }
    }
}
