﻿using System;
using System.IO;
using System.Linq;
using DiplomProject.Services.Builder;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using DiplomProject.Services.Builders;
using DiplomProject.Enum;

namespace DiplomProject.Services.Generators
{
    public class ViewModelGenerator
    {
        private readonly AsyncPackage _package;
        private readonly PlatformType _platform;
        private readonly ViewModelBuilder _viewModelBuilder;
        private readonly MessageService _messageService;
        private readonly DialogViewModelBuilder _dialogViewModelBuilder;

        public ViewModelGenerator(AsyncPackage package, PlatformType platform)
        {
            _platform = platform;
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _viewModelBuilder = new ViewModelBuilder(platform);
            _dialogViewModelBuilder = new DialogViewModelBuilder(platform);
            _messageService = new MessageService(package);
        }

        public void GenerateViewModel(
            CodeClass modelClass, 
            ProjectItem targetProjectItem, 
            bool isUseDatabase, 
            string dbProvider, 
            bool isAddingMethod, 
            bool isEditingMethod, 
            bool isDeletingMethod,
            bool isDialog)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                string projectPath = Path.GetDirectoryName(targetProjectItem.ContainingProject.FullName);
                string viewModelsFolder = Path.Combine(projectPath, "ViewModels");

                if (!Directory.Exists(viewModelsFolder))
                {
                    Directory.CreateDirectory(viewModelsFolder);
                }

                string viewModelContent = _viewModelBuilder.BuildViewModelContent(modelClass, isUseDatabase, dbProvider, isAddingMethod, isEditingMethod, isDeletingMethod, isDialog);
                string viewModelName = $"{modelClass.Name}ViewModel.cs";
                string viewModelPath = Path.Combine(viewModelsFolder, viewModelName);
                
                SaveProjectAndPath(viewModelPath, viewModelContent, targetProjectItem);

                if (isDialog)
                {
                    string dialogViewModelContent = _dialogViewModelBuilder.BuildDialogViewModelContent(modelClass);
                    string dialogViewModelName = $"Dialog{modelClass.Name}ViewModel.cs";
                    string dialogViewModelPath = Path.Combine(viewModelsFolder, dialogViewModelName);

                    SaveProjectAndPath(dialogViewModelPath, dialogViewModelContent, targetProjectItem);
                }

                _messageService.ShowSuccessMessage($"ViewModel for {modelClass.Name} successfully generated!");
            }
            catch (Exception ex)
            {
                _messageService.ShowErrorMessage($"Error generating ViewModel: {ex.Message}");
            }
        }

        private void SaveProjectAndPath(string viewModelPath, string viewModelContent, ProjectItem targetProjectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            File.WriteAllText(viewModelPath, viewModelContent);

            var viewModelsFolderItem = targetProjectItem.ContainingProject.ProjectItems
                .Cast<ProjectItem>()
                .FirstOrDefault(i =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return i.Name == "ViewModels";
                }) ?? targetProjectItem.ContainingProject.ProjectItems.AddFolder("ViewModels");

            viewModelsFolderItem.ProjectItems.AddFromFile(viewModelPath);
        }
    }
}
