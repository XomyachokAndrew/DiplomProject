﻿using System;
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
    public class ViewModelGenerator
    {
        private readonly AsyncPackage _package;
        private readonly ViewModelBuilder _viewModelBuilder;
        private readonly MessageService _messageService;

        public ViewModelGenerator(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _viewModelBuilder = new ViewModelBuilder();
            _messageService = new MessageService(package);
        }

        public void GenerateViewModel(CodeClass modelClass, ProjectItem targetProjectItem, string jsonFilePath, string dbSetName)
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

                string viewModelContent = _viewModelBuilder.BuildViewModelContent(modelClass, jsonFilePath, dbSetName);
                string viewModelName = $"{modelClass.Name}ViewModel.cs";
                string viewModelPath = Path.Combine(viewModelsFolder, viewModelName);

                File.WriteAllText(viewModelPath, viewModelContent);

                var viewModelsFolderItem = targetProjectItem.ContainingProject.ProjectItems
                    .Cast<ProjectItem>()
                    .FirstOrDefault(i =>
                    {
                        ThreadHelper.ThrowIfNotOnUIThread();
                        return i.Name == "ViewModels";
                    }) ?? targetProjectItem.ContainingProject.ProjectItems.AddFolder("ViewModels");

                viewModelsFolderItem.ProjectItems.AddFromFile(viewModelPath);

                _messageService.ShowSuccessMessage($"ViewModel for {modelClass.Name} successfully generated!");
            }
            catch (Exception ex)
            {
                _messageService.ShowErrorMessage($"Error generating ViewModel: {ex.Message}");
            }
        }

        
    }
}
