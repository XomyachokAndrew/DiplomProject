using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using DiplomProject.Data;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace DiplomProject.Helper
{
    public class XamlGenerator
    {
        private readonly AsyncPackage package;

        public void GenerateXaml(string className, bool generateViewModel, bool useDataBinding, bool addValidation)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
                var selectedItem = dte.SelectedItems.Item(1).ProjectItem;
                string projectPath = Path.GetDirectoryName(dte.ActiveDocument.Path);

                var finder = new CodeClassFinder();

                var codeClass = finder.FindClassByName(className);
                if (codeClass == null)
                {
                    throw new Exception($"Class {className} not found");
                }

                string xamlContent = BuildXamlContent(codeClass, useDataBinding, addValidation);

                string fileName = $"{className}View.xaml";
                string filePath = Path.Combine(projectPath, fileName);

                File.WriteAllText(filePath, xamlContent);

                selectedItem.ContainingProject.ProjectItems.AddFromFile(filePath);

                if (generateViewModel)
                {
                    ViewModelCreator.GenerateViewModel(codeClass, useDataBinding, addValidation, projectPath, selectedItem);
                }

                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"XAML for {className} successfully generated!",
                    "Success",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            catch (Exception ex)
            {
                // TODO
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error generating XAML: {ex.Message}",
                    "Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        private string BuildXamlContent(CodeClass codeClass, bool useDataBinding, bool addValidation)
        {
            var grid = new Grid();

            var stackPanel = new StackPanel { Margin = new Thickness(20) };
            grid.Children.Add(stackPanel);

            foreach (CodeElement member in codeClass.Members) 
            {
                if (member is CodeProperty property)
                {
                    XamlObjectGenerator.AddFieldForProperty(stackPanel, property, useDataBinding, addValidation);
                }
            }

            if (useDataBinding)
            {
                var submitButton = new Button
                {
                    Content = "Submit",
                    Margin = new Thickness(0, 20, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Width = 100
                };

                submitButton.SetBinding(Button.CommandProperty, new Binding("SubmitCommand"));
                stackPanel.Children.Add(submitButton);
            }

            return XamlWriter.Save(grid);
        }
    }
}
