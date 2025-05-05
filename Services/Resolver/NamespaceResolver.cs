using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Resolver
{
    public class NamespaceResolver
    {
        public Dictionary<string, string> GetProjectNamespace(CodeClass modelClass)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (DTE)Package.GetGlobalService(typeof(DTE));

            return new Dictionary<string, string>
            {
                ["project"] = dte.Solution.Projects.Item(1).Name,
                ["model"] = modelClass.Namespace?.Name ?? $"{dte.Solution.Projects.Item(1).Name}.Models"
            };
        }
    }
}
