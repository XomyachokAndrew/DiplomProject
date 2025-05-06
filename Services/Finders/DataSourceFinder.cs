using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Finders
{
    public class DataSourceFinder
    {
        private readonly DTE _dte;

        public DataSourceFinder()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = (DTE)Package.GetGlobalService(typeof(DTE));
        }

        public string FindJsonFile(string projectPath, string modelName)
        {
            string[] possibleFiles = {
                Path.Combine(projectPath, $"{modelName}.json"),
                Path.Combine(projectPath, $"{modelName}s.json"),
                Path.Combine(projectPath, "Data", $"{modelName}.json"),
                Path.Combine(projectPath, "App_Data", $"{modelName}.json")
            };
            return possibleFiles.FirstOrDefault(File.Exists);
        }

        public string FindDbSetName(CodeClass modelClass, string projectPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
           
            Project project = _dte.Solution.FindProjectItem(modelClass.ProjectItem.FileNames[0]).ContainingProject;

            foreach (ProjectItem item in project.ProjectItems)
            {
                if (item.FileCodeModel != null)
                {
                    foreach (CodeElement element in item.FileCodeModel.CodeElements)
                    {
                        if (element is CodeClass dbContextClass &&
                            dbContextClass.Bases.OfType<CodeElement>().Any(b =>
                            {
                                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                                return b.Name.Contains("DbContext");
                            }))
                        {
                            foreach (CodeElement member in dbContextClass.Members)
                            {
                                if (member is CodeProperty property &&
                                    property.Type.AsString.StartsWith("DbSet<") &&
                                    property.Type.AsString.Contains(modelClass.Name))
                                {
                                    return property.Name;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
