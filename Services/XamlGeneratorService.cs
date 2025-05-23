using System;
using DiplomProject.Services.Generators;
using DiplomProject.Services.Finders;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using DiplomProject.Enum;

namespace DiplomProject.Services
{
    public class XamlGeneratorService
    {
        private readonly PlatformType _platform;
        private readonly AsyncPackage _package;
        private readonly ViewGenerator _viewGenerator;
        private readonly ViewModelGenerator _viewModelGenerator;
        private readonly MessageService _messageService;

        public XamlGeneratorService(AsyncPackage package, PlatformType platform)
        {
            _platform = platform;
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _viewGenerator = new ViewGenerator(package, platform);
            _viewModelGenerator = new ViewModelGenerator(package, platform);
            _messageService = new MessageService(package);
        }

        public void GenerateXaml(
            string className,
            bool isGenerateViewModel,
            bool isUseDataBinding,
            bool isUseDatabase,
            string dbProvider,
            bool isAddingMethod,
            bool isEditingMethod,
            bool isDeletingMethod,
            bool isDialog)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentException("Class name cannot be empty", nameof(className));

            try
            {
                var dte = (DTE)Package.GetGlobalService(typeof(DTE));
                if (dte.SelectedItems.Count == 0)
                    throw new InvalidOperationException("No project item selected");

                var selectedItem = dte.SelectedItems.Item(1).ProjectItem;

                var codeClassfinder = new CodeClassFinder();
                var codeClass = codeClassfinder.FindClassByName(className)
                    ?? throw new InvalidOperationException($"Class {className} not found");

                GenerateViewComponents(codeClass, selectedItem, isUseDataBinding,
                    isDialog);

                if (isGenerateViewModel)
                {
                    GenerateViewModelComponents(codeClass, selectedItem, isUseDatabase,
                        dbProvider, isAddingMethod, isEditingMethod, isDeletingMethod, isDialog);
                }

                _messageService.ShowSuccessMessage($"XAML for {className} successfully generated!");
            }
            catch (Exception ex)
            {
                _messageService.ShowErrorMessage($"Error generating XAML: {ex.Message}");
                throw;
            }
        }

        private void GenerateViewComponents(
            CodeClass codeClass,
            ProjectItem selectedItem,
            bool isUseDataBinding,
            bool isDialog)
        {
            _viewGenerator.GenerateViewFiles(
                codeClass,
                selectedItem,
                isUseDataBinding,
                isDialog);
        }

        private void GenerateViewModelComponents(
            CodeClass codeClass,
            ProjectItem selectedItem,
            bool isUseDatabase,
            string dbProvider,
            bool isAddingMethod,
            bool isEditingMethod,
            bool isDeletingMethod,
            bool isDialog)
        {
            _viewModelGenerator.GenerateViewModel(
                codeClass,
                selectedItem,
                isUseDatabase,
                dbProvider,
                isAddingMethod,
                isEditingMethod,
                isDeletingMethod,
                isDialog);
        }
    }
}