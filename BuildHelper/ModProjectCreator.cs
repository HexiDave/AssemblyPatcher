using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using EnvDTE90;
using EnvDTE100;
using System.IO;

namespace BuildHelper
{
    public class ModProjectCreator : EventLogger
    {
        static readonly string SubnauticaModsSolutionPath = @"SubnauticaMods.sln";
        static readonly string DnlibProjectPath = @"\Libraries\dnlib\src\dnlib.csproj";
        static readonly string BootstrapLibProjectPath = @"\BootstrapLib\BootstrapLib.csproj";
        static readonly string ModsBasePath = @"\Mods";
        static readonly string TemplateBasePath = @"\ModTemplate\ModTemplate.vstemplate";
        static readonly string VSDTEVersion = "VisualStudio.DTE.15.0";

        public bool Create(string username, string modName)
        {
            var currentDirectory = Directory.GetCurrentDirectory();

            var modsPath = currentDirectory + ModsBasePath;
            if (!Directory.Exists(modsPath))
            {
                LogEvent("Mods directory not found!");
                return false;
            }

            var templatePath = currentDirectory + TemplateBasePath;
            if (!File.Exists(templatePath))
            {
                LogEvent("Template file not found!");
                return false;
            }


            var type = Type.GetTypeFromProgID(VSDTEVersion);
            if (type == null)
            {
                LogEvent("Build system invalid!");
                return false;
            }

            var solutionFolderPath = currentDirectory + string.Format(ModsBasePath + @"\{0}\", username);
            var solutionFilePath = solutionFolderPath + SubnauticaModsSolutionPath;

            var dte = (EnvDTE.DTE)System.Activator.CreateInstance(type);            

            try
            {
                if (!File.Exists(solutionFilePath))
                {
                    dte.Solution.Create(solutionFolderPath, username);
                }
                else
                {
                    dte.Solution.Open(solutionFilePath);
                }
            }
            catch (Exception e)
            {
                LogEvent("Failed to open the solution: " + e.Message);
                LogEvent(e.StackTrace);
                return false;
            }

            try
            {
                dte.Solution.AddFromTemplate(templatePath, string.Format(modsPath + @"\{0}\{1}", username, modName), modName);
            }
            catch (Exception e)
            {
                LogEvent("Failed to create the mod project: " + e.Message);
                LogEvent(e.StackTrace);
                return false;
            }

            try
            {
                AddLibraryProject("BootstrapLib", dte.Solution as Solution2, currentDirectory + BootstrapLibProjectPath);
                AddLibraryProject("dnlib", dte.Solution as Solution2, currentDirectory + DnlibProjectPath);
            }
            catch (Exception e)
            {
                LogEvent("Failed to add the prerequisite libraries: " + e.Message);
                LogEvent(e.StackTrace);
                return false;
            }

            try
            {
                dte.Solution.SaveAs(solutionFilePath);
            }
            catch (Exception e)
            {
                LogEvent("Failed to save the solution: " + e.Message);
                LogEvent(e.StackTrace);
                return false;
            }

            return true;
        }

        void AddLibraryProject(string name, Solution2 solution, string projectPath)
        {
            if (FindProjectByName(solution, name) == null)
            {
                solution.AddFromFile(projectPath);
            }
        }

        Project FindProjectByName(Solution2 solution, string name)
        {
            foreach (Project project in solution.Projects)
            {
                if (project.Name == name)
                {
                    return project;
                }
            }

            return null;
        }
    }
}
