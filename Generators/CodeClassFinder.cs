using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;

public class CodeClassFinder
{
    private readonly DTE _dte;

    public CodeClassFinder()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        _dte = (DTE)Package.GetGlobalService(typeof(DTE));
    }

    public CodeClass FindClassByName(string className)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        foreach (Project project in _dte.Solution.Projects)
        {
            try
            {
                if (project.ProjectItems == null) continue;

                foreach (ProjectItem item in project.ProjectItems)
                {
                    var codeClass = FindClassInProjectItem(item, className);
                    if (codeClass != null)
                        return codeClass;
                }
            }
            catch
            {
                // Пропускаем проблемные проекты
            }
        }

        return null;
    }

    private CodeClass FindClassInProjectItem(ProjectItem item, string className)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (item.FileCodeModel != null)
        {
            foreach (CodeElement element in item.FileCodeModel.CodeElements)
            {
                var codeClass = FindClassInCodeElement(element, className);
                if (codeClass != null)
                    return codeClass;
            }
        }

        if (item.ProjectItems != null)
        {
            foreach (ProjectItem subItem in item.ProjectItems)
            {
                var codeClass = FindClassInProjectItem(subItem, className);
                if (codeClass != null)
                    return codeClass;
            }
        }

        return null;
    }

    private CodeClass FindClassInCodeElement(CodeElement element, string className)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (element is CodeNamespace codeNamespace)
        {
            foreach (CodeElement member in codeNamespace.Members)
            {
                var codeClass = FindClassInCodeElement(member, className);
                if (codeClass != null)
                    return codeClass;
            }
        }
        else if (element is CodeClass codeClass && codeClass.Name == className)
        {
            return codeClass;
        }
        else if (element is CodeClass outerClass)
        {
            foreach (CodeElement member in outerClass.Members)
            {
                if (member is CodeClass innerClass && innerClass.Name == className)
                    return innerClass;
            }
        }

        return null;
    }
}