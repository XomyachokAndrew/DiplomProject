using System;
using System.IO;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace DiplomProject.Services
{
    public class XamlGenerator
    {
        private readonly AsyncPackage _package;

        public XamlGenerator(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
        }

        public void GenerateXaml(string className)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                var dte = (DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
                var selectedItem = dte.SelectedItems.Item(1).ProjectItem;
                string projectPath = Path.GetDirectoryName(dte.ActiveDocument.Path);

                var finder = new CodeClassFinder();

                var codeClass = finder.FindClassByName(className);
                if (codeClass == null)
                {
                    throw new Exception($"Class {className} not found");
                }

                string xamlContent = BuildXamlContent(codeClass);

                string fileName = $"{className}View.xaml";
                string filePath = Path.Combine(projectPath, fileName);

                File.WriteAllText(filePath, xamlContent);

                selectedItem.ContainingProject.ProjectItems.AddFromFile(filePath);

                VsShellUtilities.ShowMessageBox(
                    _package,
                    $"XAML for {className} successfully generated!",
                    "Success",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    _package,
                    $"Error generating XAML: {ex.Message}",
                    "Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        private string BuildXamlContent(CodeClass codeClass)
        {
            var properties = codeClass.Members.OfType<CodeProperty>().ToList();
            if (!properties.Any()) return "";

            var sb = new StringBuilder();
            sb.AppendLine(@"<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
        <ScrollViewer>
            <StackPanel Margin=""20"">");

            // Добавляем стили
            sb.AppendLine(@"<StackPanel.Resources>
        <Style TargetType=""TextBlock"">
            <Setter Property=""Margin"" Value=""0,0,0,5""/>
            <Setter Property=""FontWeight"" Value=""SemiBold""/>
        </Style>
        <Style TargetType=""TextBox"">
            <Setter Property=""Margin"" Value=""0,0,0,15""/>
            <Setter Property=""IsReadOnly"" Value=""True""/>
        </Style>
    </StackPanel.Resources>");

            // Добавляем элементы управления
            foreach (var prop in properties)
            {
                sb.AppendLine($@"<TextBlock Text=""{prop.Name}""/>");
                sb.AppendLine($@"<TextBox Text=""{{Binding {prop.Name}, Mode=OneWay}}""/>");
            }

            sb.AppendLine(@"</StackPanel>
        </ScrollViewer>
    </Grid>");

            return sb.ToString();
        }
    }
}
