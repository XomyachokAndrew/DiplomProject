using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Resolver
{
    public class DataSourceResolver
    {
        public string GetAdditionalUsings(bool isUseDatabase, string dbProvider, Dictionary<string, string> namespaces)
        {
            var usings = new List<string>();
            usings.Add("using CommunityToolkit.Mvvm.Input;");

            if (isUseDatabase)
            {
                switch (dbProvider)
                {
                    case "Json":
                        usings.Add("using System.Text.Json;");
                        usings.Add("using System.IO;");
                        break;
                    case "DbContext":
                        usings.Add("using System.Data.Entity;");
                        usings.Add($"using {namespaces["project"]}.Context");
                        break;
                    default:
                        break;
                }
            }
            return string.Join("\n", usings);
        }

        public string GetDbContextName(CodeClass modelClass)
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