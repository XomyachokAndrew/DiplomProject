using System.Collections.Generic;
using System.Text;
using DiplomProject.Services.Finders;
using DiplomProject.Services.Resolver;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Builder
{
    public class ViewModelBuilder
    {
        private readonly NamespaceResolver _namespaceResolver = new NamespaceResolver();
        private readonly DataSourceResolver _dataSourceResolver = new DataSourceResolver();
        private readonly DataSourceFinder _dataSourceFinder = new DataSourceFinder();

        public string BuildViewModelContent(
            CodeClass modelClass, 
            bool isUseDatabase, 
            string dbProvider, 
            bool isAddingMethod, 
            bool isEditingMethod, 
            bool isDeletingMethod)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var namespaces = _namespaceResolver.GetProjectNamespace(modelClass);

            var editItemCode = new StringBuilder();
            var addItemCode = new StringBuilder();
            var deleteItemCode = new StringBuilder();
            var initializationCode = new StringBuilder();
            var saveChangesCode = new StringBuilder();
            var dbUsingCode = new StringBuilder();
            var usingsCode = new StringBuilder();

            BuildInitializationCode(modelClass, initializationCode, isUseDatabase, dbProvider);
            BuildEdit(isEditingMethod, modelClass, editItemCode);
            BuildAdd(isAddingMethod, modelClass, addItemCode);
            BuildDelete(isDeletingMethod, deleteItemCode);
            BuildSaveChanges(isUseDatabase, dbProvider, saveChangesCode);
            BuildUsingsContent(namespaces, isUseDatabase, dbProvider, usingsCode);
            _dataSourceFinder.BuildDbUsingCode(modelClass, isUseDatabase, dbProvider, dbUsingCode);

            var viewModelContent = BuildViewModelContent(
                modelClass,
                namespaces,
                editItemCode,
                addItemCode,
                deleteItemCode,
                initializationCode,
                saveChangesCode,
                dbUsingCode,
                usingsCode
                );
            return viewModelContent;
        }

        public string BuildViewModelContent(
            CodeClass codeClass,
            Dictionary<string, string> namespaces,
            StringBuilder editItemCode,
            StringBuilder addItemCode,
            StringBuilder deleteItemCode,
            StringBuilder initializationCode,
            StringBuilder saveChangesCode,
            StringBuilder dbUsingCode,
            StringBuilder usingsCode)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return $@"{usingsCode}

namespace {namespaces["project"]}.ViewModels
{{
    public partial class {codeClass.Name}ViewModel : INotifyPropertyChanged
    {{
        public event PropertyChangedEventHandler? PropertyChanged;
        {dbUsingCode}

        // **Свойства для привязки в UI**
        private ObservableCollection<{codeClass.Name}> _items = new();
        public ObservableCollection<{codeClass.Name}> Items
        {{
            get => _items;
            set
            {{
                _items = value;
                OnPropertyChanged(nameof(Items));
            }}
        }}

        private {codeClass.Name}? _selectedItem;
        public {codeClass.Name}? SelectedItem
        {{
            get => _selectedItem;
            set
            {{
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
                DeleteCommand.NotifyCanExecuteChanged(); // Обновляем доступность команд
                EditCommand.NotifyCanExecuteChanged();
            }}
        }}

        // **Команды**
        public IRelayCommand AddCommand {{ get; }}
        public IRelayCommand EditCommand {{ get; }}
        public IRelayCommand DeleteCommand {{ get; }}
        public IRelayCommand SaveCommand {{ get; }}

        public {codeClass.Name}ViewModel()
        {{
            LoadItems();
            AddCommand = new RelayCommand(AddItem);
            EditCommand = new RelayCommand(EditItem, () => SelectedItem != null);
            DeleteCommand = new RelayCommand(DeleteItem, () => SelectedItem != null);
            SaveCommand = new RelayCommand(SaveChanges);
        }}

        private void LoadItems() 
        {{
            {initializationCode}
        }}

        private void AddItem()
        {{
            {addItemCode}
        }}

        private void EditItem()
        {{
            {editItemCode}
        }}

        private void DeleteItem()
        {{
            {deleteItemCode}
        }}

        private void SaveChanges()
        {{
            {saveChangesCode}
        }}

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }}
}}";
        }

        private void BuildEdit(bool isEdit, CodeClass codeClass, StringBuilder editItemCode)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!isEdit)
            {
                editItemCode.Append("throw new NotImplementedException();");
            }
            else
            {
                var itemProperty = new StringBuilder();

                foreach (CodeElement member in codeClass.Members)
                {
                    if (member is CodeProperty property)
                    {
                        itemProperty.AppendLine($@"{property.Name} = SelectedItem.{property.Name},");
                    }
                }

                var itemProp = new StringBuilder();

                foreach (CodeElement member in codeClass.Members)
                {
                    if (member is CodeProperty property)
                    {
                        itemProp.AppendLine($@"SelectedItem.{property.Name} = updatedItem.{property.Name};");
                    }
                }

                editItemCode.AppendLine($@"
            if (SelectedItem != null)
            {{
                var itemCopy = new {codeClass.Name}
                {{
                    {itemProperty}    
                }};

                var dialogVm = new Dialog{codeClass.Name}ViewModel(itemCopy, ""Edit {codeClass.Name}"");
                var dialog = new Dialog{codeClass.Name}View
                {{
                    Owner = Application.Current.MainWindow,
                    DataContext = dialogVm
                }};

                dialog.ShowDialog();

                if (dialogVm.IsSaved)
                {{
                    var updatedItem = dialogVm.GetItem();
                    {itemProp}
                    SaveChanges();
                }}
                OnPropertyChanged(nameof(Items)); // Обновляем список
            }}
");
            }
        }

        private void BuildAdd(bool isAdd, CodeClass codeClass, StringBuilder addItemCode)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!isAdd)
            {
                addItemCode.Append("throw new NotImplementedException();");
            }
            else
            {
                var itemProp = new StringBuilder();
                var clearProp = new StringBuilder();

                foreach (CodeElement member in codeClass.Members)
                {
                    if (member is CodeProperty property)
                    {
                        if (property.Name != "Id")
                        {
                            itemProp.AppendLine($@"{property.Name} = New{property.Name},");
                        }
                    }
                }

                addItemCode.AppendLine($@"
            var dialogVm = new Dialog{codeClass.Name}ViewModel(title: ""Add New {codeClass.Name}"");
            var dialog = new Dialog{codeClass.Name}View
            {{
                Owner = Application.Current.MainWindow,
                DataContext = dialogVm
            }};

            dialog.ShowDialog();

            if (dialogVm.IsSaved)
            {{
                var newItem = dialogVm.GetItem();
                newItem.Id = Items.Any() ? Items.Max(p => p.Id) + 1 : 1;
                Items.Add(newItem);
                SaveChanges();
            }}
");
            }
        }

        private void BuildDelete(bool isDelete, StringBuilder deleteItemCode)
        {
            if (!isDelete)
            {
                deleteItemCode.Append("throw new NotImplementedException();");
            }
            else
            {
                deleteItemCode.AppendLine($@"if (SelectedItem != null && MessageBox.Show(""Delete this item?"", ""Confirm"", 
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {{
                Items.Remove(SelectedItem);
                SaveChanges();
            }}");
            }
        }

        private void BuildSaveChanges(bool isUseDatabase, string dbProvider, StringBuilder saveChanges)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!isUseDatabase)
            {
                saveChanges.AppendLine("throw new NotImplementedException();");
            }
            else
            {
                string dbSaveChange = _dataSourceFinder.GetDbSaveChange(dbProvider);
                saveChanges.AppendLine($@"
            try
            {{
                {dbSaveChange}
            }}
            catch (Exception ex)
            {{
                System.Diagnostics.Debug.WriteLine($""Error saving changes: {{ex.Message}}"");
            }}");
            }
        }

        private void BuildUsingsContent(Dictionary<string, string> namespaces, bool isUseDatabase, string dbProvider, StringBuilder usings)
        {
            string additionalUsings = _dataSourceResolver.GetAdditionalUsings(isUseDatabase, dbProvider, namespaces);

            usings.AppendLine($@"using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Windows;
using {namespaces["project"]}.Views;
{additionalUsings}
using {namespaces["model"]};");
        }

        private void BuildInitializationCode(
            CodeClass modelClass,
            StringBuilder initializationCode,
            bool isUseDatabase,
            string dbProvider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (isUseDatabase)
            {
                var loadItems = _dataSourceFinder.GetDbLoadItem(modelClass, dbProvider);
                initializationCode.AppendLine(loadItems.ToString());
            }
            else
            {
                initializationCode.AppendLine("throw new NotImplementedException();");
            }
        }
    }
}