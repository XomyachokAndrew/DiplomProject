using System;
using System.IO;
using System.Linq;
using DiplomProject.Services.Builder;
using DiplomProject.Services.Builders;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Generators
{
    public class ViewGenerator
    {
        private readonly AsyncPackage _package;
        private readonly XamlBuilder _xamlBuilder;
        private readonly MessageService _messageService;
        private readonly DialogBuilder _dialogBuilder;

        public ViewGenerator(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _xamlBuilder = new XamlBuilder();
            _dialogBuilder = new DialogBuilder();
            _messageService = new MessageService(package);
        }

        public void GenerateViewFiles(
            CodeClass modelClass,
            ProjectItem targetProjectItem,
            bool isUseDataBinding,
            bool isAddingButton,
            bool isEditingButton,
            bool isDeletingButton,
            bool isDialog)
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
                var (xamlContent, csContent) = _xamlBuilder.BuildViewContent(modelClass, isUseDataBinding, isAddingButton, isEditingButton, isDeletingButton);

                SaveAndAddToProject(
                    targetProjectItem,
                    viewsFolder,
                    $"{viewName}.xaml",
                    xamlContent,
                    $"{viewName}.xaml.cs",
                    csContent,
                    "Views");

                if (isDialog)
                {
                    var (dialogXamlContent, dialogCsContent) = _dialogBuilder.BuildDialogContent(modelClass, isUseDataBinding);

                    SaveAndAddToProject(
                        targetProjectItem,
                        viewsFolder,
                        $"Dialog{viewName}.xaml",
                        dialogXamlContent,
                        $"Dialog{viewName}.xaml.cs",
                        dialogCsContent,
                        "Views");
                }

                _messageService.ShowSuccessMessage($"View for {modelClass.Name} successfully generated!");
            }
            catch (Exception ex)
            {
                _messageService.ShowErrorMessage($"Error generating View: {ex.Message}");
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
    }
}
