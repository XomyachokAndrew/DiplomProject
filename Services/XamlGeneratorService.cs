using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiplomProject.Services.Generators;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services
{
    public class XamlGeneratorService
    {
        private readonly AsyncPackage _package;
        private readonly ViewGenerator _viewGenerator;
        private readonly ViewModelGenerator _viewModelGenerator;

        public XamlGeneratorService(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _viewGenerator = new ViewGenerator(package);
            _viewModelGenerator = new ViewModelGenerator(package);
        }

        public void GenerateXaml(string className, bool isGenerateViewModel, bool isUseDataBinding)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                var dte = (DTE)Package.GetGlobalService(typeof(DTE));
                var selectedItem = dte.SelectedItems.Item(1).ProjectItem;

                var finder = new CodeClassFinder();
                var codeClass = finder.FindClassByName(className) ?? throw new Exception($"Class {className} not found");

                _viewGenerator.GenerateViewFiles(codeClass, selectedItem, isUseDataBinding);

                if (isGenerateViewModel)
                {
                    _viewModelGenerator.GenerateViewModel(codeClass, selectedItem);
                }

                ShowSuccessMessage($"XAML for {className} successfully generated!");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error generating XAML: {ex.Message}");
            }
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
