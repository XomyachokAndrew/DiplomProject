using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiplomProject.Services.Builder;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Generators
{
    public class ViewGenerator
    {
        private readonly AsyncPackage _package;
        private readonly XamlBuilder _xamlBuilder;

        public ViewGenerator(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _xamlBuilder = new XamlBuilder();
        }

        public void GenerateViewFiles(CodeClass modelClass, ProjectItem targetProjectItem, bool isUseDataBinding)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                string projectPath = Path.GetDirectoryName(targetProjectItem.ContainingProject.FullName);
                string viewsFolder = Path.Combine(projectPath, "Views");

                if (!Directory.Exists(viewsFolder))
                {
                    Directory.CreateDirectory(viewsFolder);
                }

                string viewName = $"{modelClass.Name}View";
                var (xamlContent, csContent) = _xamlBuilder.BuildViewContent(modelClass, isUseDataBinding);

                SaveAndAddToProject(
                    targetProjectItem,
                    viewsFolder,
                    $"{viewName}.xaml",
                    xamlContent,
                    $"{viewName}.xaml.cs",
                    csContent,
                    "Views");

                ShowSuccessMessage($"View for {modelClass.Name} successfully generated!");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error generating View: {ex.Message}");
            }
        }

        private void SaveAndAddToProject(
            ProjectItem targetProjectItem,
            string folderPath,
            string fileName1,
            string content1,
            string fileName2,
            string content2,
            string folderName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string filePath1 = Path.Combine(folderPath, fileName1);
            File.WriteAllText(filePath1, content1);

            string filePath2 = Path.Combine(folderPath, fileName2);
            File.WriteAllText(filePath2, content2);

            var folderItem = targetProjectItem.ContainingProject.ProjectItems
                .Cast<ProjectItem>()
                .FirstOrDefault(i =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return i.Name == folderName;
                }) ?? targetProjectItem.ContainingProject.ProjectItems.AddFolder(folderName);

            folderItem.ProjectItems.AddFromFile(filePath1);
        }

        private void ShowSuccessMessage(string message)
        {
            VsShellUtilities.ShowMessageBox(
                _package,
                message,
                "Success",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private void ShowErrorMessage(string message)
        {
            VsShellUtilities.ShowMessageBox(
                _package,
                message,
                "Error",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
