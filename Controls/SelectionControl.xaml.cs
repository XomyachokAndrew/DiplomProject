using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace DiplomProject.Controls
{
    /// <summary>
    /// Логика взаимодействия для SelectionControl.xaml
    /// </summary>
    public partial class SelectionControl : UserControl
    {
        public event EventHandler OkClicked;
        public event EventHandler CancelClicked;

        public SelectionControl()
        {
            InitializeComponent();

            // Заполните ComboBox моделями данных
            LoadClasses();

            DbProviderComboBox.SelectedIndex = 0; // Устанавливаем JSON по умолчанию
        }

        public string SelectedModel => ModelComboBox.SelectedItem?.ToString();
        public bool GenerateViewModel => GenerateViewModelCheckBox.IsChecked ?? false;
        public bool UseDataBinding => UseDataBindingCheckBox.IsChecked ?? false;
        public bool UseDatabase => UseDatabaseCheckBox.IsChecked ?? false;
        public bool AddAdding => AddMethodAddingCheckBox.IsChecked ?? false;
        public bool AddEditing => AddMethodEditCheckBox.IsChecked ?? false;
        public bool AddDeleting => AddMethodDeleteCheckBox.IsChecked ?? false;

        public string SelectedDbProvider
        {
            get
            {
                if (!UseDatabase)
                    return null;

                if (DbProviderComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    return selectedItem.Tag.ToString();
                }
                return "Json"; // Значение по умолчанию
            }
        }

        private void UseDatabaseCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            DbProviderComboBox.IsEnabled = true;
        }

        private void UseDatabaseCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            DbProviderComboBox.IsEnabled = false;
        }

        private void GenerateViewModelCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UseDataBindingCheckBox.IsEnabled = true;
            AddMethodAddingCheckBox.IsEnabled = true;
            AddMethodEditCheckBox.IsEnabled = true;
            AddMethodDeleteCheckBox.IsEnabled = true;
        }

        private void GenerateViewModelCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            UseDataBindingCheckBox.IsEnabled = false;
            AddMethodAddingCheckBox.IsEnabled = false;
            AddMethodDeleteCheckBox.IsEnabled = false;
            AddMethodEditCheckBox.IsEnabled = false;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            OkClicked?.Invoke(this, EventArgs.Empty);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancelClicked?.Invoke(this, EventArgs.Empty);
        }

        #region Model Class
        private void LoadClasses()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                try
                {
                    ModelComboBox.Items.Clear();
                    var classes = GetAllModelClasses();

                    foreach (var className in classes)
                    {
                        ModelComboBox.Items.Add(className);
                    }

                    if (ModelComboBox.Items.Count > 0)
                        ModelComboBox.SelectedIndex = 0;
                }
                catch (Exception ex)
                {
                    // Получаем сервис IVsUIShell для показа сообщений
                    var uiShell = await AsyncServiceProvider.GlobalProvider.GetServiceAsync<SVsUIShell, IVsUIShell>();

                    Guid clsid = Guid.Empty;
                    int result = uiShell.ShowMessageBox(
                        0,
                        ref clsid,
                        "Error",
                        $"Error loading classes: {ex.Message}",
                        string.Empty,
                        0,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                        OLEMSGICON.OLEMSGICON_CRITICAL,
                        0,
                        out int buttonPressed);
                }
            });
        }

        private List<string> GetAllModelClasses()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (DTE)Package.GetGlobalService(typeof(DTE));
            var classes = new List<string>();

            foreach (Project project in dte.Solution.Projects)
            {
                try
                {
                    if (project.ProjectItems == null) continue;

                    foreach (ProjectItem item in project.ProjectItems)
                    {
                        FindClassesInProjectItem(item, classes);
                    }
                }
                catch
                {
                    // Пропускаем проблемные проекты
                }
            }

            return classes.Distinct().OrderBy(c => c).ToList();
        }

        private void FindClassesInProjectItem(ProjectItem item, List<string> classes)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (item.FileCodeModel != null)
            {
                foreach (CodeElement element in item.FileCodeModel.CodeElements)
                {
                    FindClassesInCodeElement(element, classes);
                }
            }

            // Рекурсивно проверяем вложенные элементы
            if (item.ProjectItems != null)
            {
                foreach (ProjectItem subItem in item.ProjectItems)
                {
                    FindClassesInProjectItem(subItem, classes);
                }
            }
        }

        private void FindClassesInCodeElement(CodeElement element, List<string> classes)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (element is CodeNamespace)
            {
                foreach (CodeElement member in ((CodeNamespace)element).Members)
                {
                    FindClassesInCodeElement(member, classes);
                }
            }
            else if (element is CodeClass)
            {
                var codeClass = (CodeClass)element;
                classes.Add(codeClass.Name);

                // Проверяем вложенные классы
                foreach (CodeElement member in codeClass.Members)
                {
                    if (member is CodeClass)
                    {
                        FindClassesInCodeElement(member, classes);
                    }
                }
            }
        }

        #endregion
    }
}
