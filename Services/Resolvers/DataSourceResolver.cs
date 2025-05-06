using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Resolver
{
    public class DataSourceResolver
    {
        public string GetAdditionalUsings(string jsonFilePath, string dbSetName)
        {
            var usings = new List<string>();

            if (!string.IsNullOrEmpty(jsonFilePath))
            {
                usings.Add("using System.Text.Json;");
                usings.Add("using System.IO;");
            }

            if (!string.IsNullOrEmpty(dbSetName))
            {
                usings.Add("using System.Data.Entity;");
            }

            return string.Join("\n", usings);
        }

        public string GetDbContextName(CodeClass modelClass, Dictionary<string, string> namespaces)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var project = modelClass.ProjectItem.ContainingProject;

            foreach (ProjectItem item in project.ProjectItems)
            {
                if (item.FileCodeModel != null)
                {
                    foreach (CodeElement element in item.FileCodeModel.CodeElements)
                    {
                        if (element is CodeClass dbContextClass &&
                            dbContextClass.Bases.OfType<CodeElement>().Any(b =>
                            {
                                ThreadHelper.ThrowIfNotOnUIThread();
                                return b.Name.Contains("DbContext");
                            }))
                        {
                            return dbContextClass.Name;
                        }
                    }
                }
            }
            return "YourDbContext"; // Fallback значение
        }
    }
}