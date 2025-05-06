using System;
using DiplomProject.Services.Generators;
using DiplomProject.Services.Finders;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System.IO;

namespace DiplomProject.Services
{
    public class XamlGeneratorService
    {
        private readonly AsyncPackage _package;
        private readonly ViewGenerator _viewGenerator;
        private readonly ViewModelGenerator _viewModelGenerator;
        private readonly DataSourceFinder _dataSourceFinder; 
        private readonly MessageService _messageService;

        public XamlGeneratorService(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _viewGenerator = new ViewGenerator(package);
            _viewModelGenerator = new ViewModelGenerator(package);
            _dataSourceFinder = new DataSourceFinder();
            _messageService = new MessageService(package);
        }

        public void GenerateXaml(string className, bool isGenerateViewModel, bool isUseDataBinding)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                var dte = (DTE)Package.GetGlobalService(typeof(DTE));
                var selectedItem = dte.SelectedItems.Item(1).ProjectItem;

                var codeClassfinder = new CodeClassFinder();
                var codeClass = codeClassfinder.FindClassByName(className) ?? throw new Exception($"Class {className} not found");
                
                _viewGenerator.GenerateViewFiles(codeClass, selectedItem, isUseDataBinding);

                if (isGenerateViewModel)
                {
                    // Находим источник данных
                    string projectPath = Path.GetDirectoryName(selectedItem.ContainingProject.FullName);
                    string jsonFilePath = _dataSourceFinder.FindJsonFile(projectPath, className);
                    string dbSetName = string.IsNullOrEmpty(jsonFilePath) ? _dataSourceFinder.FindDbSetName(codeClass, projectPath) : null;

                    _viewModelGenerator.GenerateViewModel(codeClass, selectedItem, jsonFilePath, dbSetName);
                }

                _messageService.ShowSuccessMessage($"XAML for {className} successfully generated!");
            }
            catch (Exception ex)
            {
                _messageService.ShowErrorMessage($"Error generating XAML: {ex.Message}");
            }
        }
    }
}
