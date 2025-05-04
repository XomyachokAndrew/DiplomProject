using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Windows.Markup;
using EnvDTE80;
using System.Windows.Forms;

namespace DiplomProject.Services
{
    public class XamlGenerator
    {
        private readonly AsyncPackage _package;

        public XamlGenerator(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
        }

        public void GenerateXaml(string className, bool isGenerateViewModel, bool isUseDataBinding)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                var dte = (DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
                var selectedItem = dte.SelectedItems.Item(1).ProjectItem;
                string projectPath = Path.GetDirectoryName(dte.ActiveDocument.Path);

                var finder = new CodeClassFinder();

                var codeClass = finder.FindClassByName(className) ?? throw new Exception($"Class {className} not found");

                GenerateViewFiles(codeClass, selectedItem);

                if (isGenerateViewModel) 
                {
                    GenerateViewModel(codeClass, selectedItem);
                }

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

        private void GenerateViewFiles(CodeClass modelClass, ProjectItem targetProjectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Получаем путь к проекту
                string projectPath = Path.GetDirectoryName(targetProjectItem.ContainingProject.FullName);
                string viewsFolder = Path.Combine(projectPath, "Views");

                string viewName = $"{modelClass.Name}View";
                string xamlFileName = $"{viewName}.xaml";
                string csFileName = $"{viewName}.xaml.cs";

                if (!Directory.Exists(viewsFolder))
                {
                    Directory.CreateDirectory(viewsFolder);
                }

                // Генерируем содержимое XAML
                string xamlContent = BuildXamlContent(modelClass);
                string xamlPath = Path.Combine(viewsFolder, xamlFileName);
                File.WriteAllText(xamlPath, xamlContent);

                // Генерируем содержимое CS
                string csContent = BuildCsForXamlContent(modelClass, viewName);
                string csPath = Path.Combine(viewsFolder, csFileName);
                File.WriteAllText(csPath, csContent);

                // Добавляем файлы в проект
                var viewsFolderItem = targetProjectItem.ContainingProject.ProjectItems
                    .Cast<ProjectItem>()
                    .FirstOrDefault(i =>
                    {
                        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                        return i.Name == "Views";
                    })
                    ?? targetProjectItem.ContainingProject.ProjectItems.AddFolder("Views");

                // Сначала добавляем XAML, затем CS файл будет добавлен автоматически как dependent item
                viewsFolderItem.ProjectItems.AddFromFile(xamlPath);

                // Показываем сообщение об успехе
                VsShellUtilities.ShowMessageBox(
                    _package,
                    $"View for {modelClass.Name} successfully generated!",
                    "Success",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    _package,
                    $"Error generating View: {ex.Message}",
                    "Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        #region Build XAML
        private string BuildXamlContent(CodeClass codeClass)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string projectNamespace = GetProjectNamespace(codeClass);

            var controls = new StringBuilder();
            foreach (CodeElement member in codeClass.Members)
            {
                if (member is CodeProperty property)
                {
                    controls.AppendLine($"<TextBlock Text=\"{property.Name}\"/>");
                    var dataControl = BuildPropertyControl(property);
                    controls.AppendLine(dataControl);
                }
            }
            return $@"
<Window x:Class=""{projectNamespace}.Views.{codeClass.Name}View""
        xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
        xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
        xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
        xmlns:local=""clr-namespace:{projectNamespace}""
        mc:Ignorable=""d""
        Title=""{codeClass.Name}View"" Height=""450"" Width=""800"">
    <Grid>
        <StackPanel>
            {controls}
        </StackPanel>
    </Grid>
</Window>
                    ";
        }

        private string BuildPropertyControl(CodeProperty property)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var propType = GetPropertyType(property).ToLower();

            if (propType.Contains("bool"))
            {
                return $"<CheckBox IsChecked=\"{{Binding {property.Name}}}\"/>";
            }
            else if (propType.Contains("date"))
            {
                return $"<DatePicker SelectedDate=\"{{Binding {property.Name}}}\"/>";
            }
            else if (propType.Contains("int") || propType.Contains("double") ||
                     propType.Contains("decimal") || propType.Contains("float"))
            {
                return $"<TextBox Text=\"{{Binding {property.Name}}}\"/>";
            }
            else if (propType.Contains("string"))
            {
                return $"<TextBox Text=\"{{Binding {property.Name}}}\"/>";
            }
            else
            {
                return $"<TextBox Text=\"{{Binding {property.Name}}}\"/>";
            }
        }

        private string BuildCsForXamlContent(CodeClass modelClass, string fileName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Получаем namespace проекта
            string projectNamespace = GetProjectNamespace(modelClass);

            string className = fileName.Replace(".xaml", "");

            return $@"
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace {projectNamespace}.Views
{{
    public partial class {className} : Window 
    {{
        public {className}()
        {{
            InitializeComponent();
        }}
    }}    
}}
                    ";

        }
        #endregion

        #region Build ViewModel
        private void GenerateViewModel(CodeClass modelClass, ProjectItem targetProjectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Получаем путь к проекту
                string projectPath = Path.GetDirectoryName(targetProjectItem.ContainingProject.FullName);
                string viewModelsFolder = Path.Combine(projectPath, "ViewModels");

                // Создаем папку ViewModels если ее нет
                if (!Directory.Exists(viewModelsFolder))
                {
                    Directory.CreateDirectory(viewModelsFolder);
                }

                // Генерируем содержимое ViewModel
                string viewModelContent = BuildViewModelContent(modelClass);

                // Сохраняем файл
                string viewModelName = $"{modelClass.Name}ViewModel.cs";
                string viewModelPath = Path.Combine(viewModelsFolder, viewModelName);
                File.WriteAllText(viewModelPath, viewModelContent);

                // Добавляем файл в проект
                var viewModelsFolderItem = targetProjectItem.ContainingProject.ProjectItems
                    .Cast<ProjectItem>()
                    .FirstOrDefault(i =>
                    {
                        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                        return i.Name == "ViewModels";
                    })
                    ?? targetProjectItem.ContainingProject.ProjectItems.AddFolder("ViewModels");

                viewModelsFolderItem.ProjectItems.AddFromFile(viewModelPath);

                // Показываем сообщение об успехе
                VsShellUtilities.ShowMessageBox(
                    _package,
                    $"ViewModel for {modelClass.Name} successfully generated!",
                    "Success",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    _package,
                    $"Error generating ViewModel: {ex.Message}",
                    "Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        private string BuildViewModelContent(CodeClass modelClass)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Получаем namespace проекта
            string projectNamespace = GetProjectNamespace(modelClass);

            // Генерируем свойства
            var propertiesCode = new StringBuilder();
            foreach (CodeElement member in modelClass.Members)
            {
                if (member is CodeProperty property)
                {
                    propertiesCode.AppendLine($@"
        public {GetPropertyType(property)} {property.Name}
        {{
            get => _model.{property.Name};
            set
            {{
                if (!EqualityComparer<{GetPropertyType(property)}>.Default.Equals(_model.{property.Name}, value))
                {{
                    _model.{property.Name} = value;
                    OnPropertyChanged();
                }}
            }}
        }}");
                }
            }

            // Генерируем команды для методов
            var commandsCode = new StringBuilder();
            foreach (CodeElement member in modelClass.Members)
            {
                if (member is CodeFunction function && function.Access == vsCMAccess.vsCMAccessPublic)
                {
                    string commandName = $"{function.Name}Command";
                    commandsCode.AppendLine($@"
        public ICommand {commandName} {{ get; }}
        
        private void Execute{function.Name}(object parameter)
        {{
            _model.{function.Name}({GetMethodParameters(function)});
        }}
        
        private bool CanExecute{function.Name}(object parameter)
        {{
            return true;
        }}");
                }
            }

            return $@"using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace {projectNamespace}.ViewModels
{{
    public class {modelClass.Name}ViewModel : INotifyPropertyChanged
    {{
        private readonly {modelClass.Name} _model;

        public {modelClass.Name}ViewModel({modelClass.Name} model)
        {{
            _model = model ?? throw new ArgumentNullException(nameof(model));
            {GenerateCommandInitializations(modelClass)}
        }}
        
        // Свойства модели
        {propertiesCode}

        // Команды
        {commandsCode}

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {{
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }}
    }}
}}";
        }

        private string GetMethodParameters(CodeFunction function)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Генерация параметров для вызова метода
            var parameters = new List<string>();
            foreach (CodeParameter param in function.Parameters)
            {
                parameters.Add($"({param.Type.AsString})parameter");
                // Можно добавить более сложную логику преобразования параметров
            }
            return string.Join(", ", parameters);
        }

        private string GenerateCommandInitializations(CodeClass modelClass)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var initializations = new List<string>();
            foreach (CodeElement member in modelClass.Members)
            {
                if (member is CodeFunction function && function.Access == vsCMAccess.vsCMAccessPublic)
                {
                    initializations.Add($"{function.Name}Command = new RelayCommand(Execute{function.Name}, CanExecute{function.Name})");
                }
            }
            return string.Join("\n            ", initializations);
        }
        #endregion

        // Вспомогательные методы
        private string GetProjectNamespace(CodeClass modelClass)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Ищем namespace модели
            if (modelClass.Namespace != null)
            {
                return modelClass.Namespace.Name.Replace(".Models", "");
            }

            // Альтернативный способ получения namespace
            return "YourProjectNamespace";
        }

        private string GetPropertyType(CodeProperty property)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Упрощенная обработка типов
            return property.Type.AsString;
        }

    }
}
