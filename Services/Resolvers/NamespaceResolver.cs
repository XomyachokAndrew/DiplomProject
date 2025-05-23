using System;
using System.Collections.Generic;
using DiplomProject.Enum;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Resolver
{
    public class NamespaceResolver
    {
        private readonly PlatformType _platform;

        public NamespaceResolver(PlatformType platform)
        {
            _platform = platform;
        }

        public Dictionary<string, string> GetProjectNamespace(CodeClass modelClass)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = (DTE)Package.GetGlobalService(typeof(DTE));
                var project = GetContainingProject(modelClass, dte.Solution) ?? dte.Solution.Projects.Item(1);

                var namespaces = new Dictionary<string, string>
                {
                    ["project"] = project.Name,
                    ["model"] = modelClass.Namespace?.Name ?? $"{project.Name}.Models",
                    ["views"] = $"{project.Name}.Views",
                    ["viewmodels"] = $"{project.Name}.ViewModels"
                };

                // Добавляем платформо-специфичные пространства имен
                AddPlatformSpecificNamespaces(namespaces);

                return namespaces;
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                throw new InvalidOperationException("Failed to resolve namespaces", ex);
            }
        }

        private Project GetContainingProject(CodeClass codeClass, Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (codeClass?.ProjectItem?.ContainingProject != null)
                return codeClass.ProjectItem.ContainingProject;

            foreach (Project project in solution.Projects)
            {
                if (project.ProjectItems != null && project.Name == codeClass.Namespace?.Name?.Split('.')[0])
                    return project;
            }

            return null;
        }

        private void AddPlatformSpecificNamespaces(Dictionary<string, string> namespaces)
        {
            switch (_platform)
            {
                case PlatformType.UWP:
                    namespaces["xaml"] = "using:Windows.UI.Xaml";
                    namespaces["controls"] = "using:Windows.UI.Xaml.Controls";
                    break;

                case PlatformType.MAUI:
                    namespaces["xaml"] = "using:Microsoft.Maui.Controls";
                    namespaces["controls"] = "using:Microsoft.Maui.Controls";
                    break;

                default: // WPF
                    namespaces["xaml"] = "using:System.Windows";
                    namespaces["controls"] = "using:System.Windows.Controls";
                    break;
            }
        }
    }
}
