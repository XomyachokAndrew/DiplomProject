using System;
using System.Collections.Generic;
using System.Text;
using DiplomProject.Enum;
using DiplomProject.Services.Finders;
using DiplomProject.Services.Generators;
using DiplomProject.Services.Handler;
using DiplomProject.Services.Resolver;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace DiplomProject.Services.Builder
{
    public class ViewModelBuilder
    {
        private readonly PlatformType _platform;
        private readonly NamespaceResolver _namespaceResolver;
        private readonly DataSourceResolver _dataSourceResolver;
        private readonly DataSourceFinder _dataSourceFinder;

        public ViewModelBuilder(PlatformType platform) 
        {
            _platform = platform;
            _namespaceResolver = new NamespaceResolver(platform);
            _dataSourceResolver = new DataSourceResolver();
            _dataSourceFinder = new DataSourceFinder();
        }

        public string BuildViewModelContent(
            CodeClass modelClass, 
            bool isUseDatabase, 
            string dbProvider, 
            bool isAddingMethod, 
            bool isEditingMethod, 
            bool isDeletingMethod,
            bool isDialog)
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
            BuildEdit(isEditingMethod, isDialog, modelClass, editItemCode);
            BuildAdd(isAddingMethod, isDialog, modelClass, addItemCode);
            BuildDelete(isDeletingMethod, modelClass, deleteItemCode);
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

            var platformSpecific = new PlatformSpecificCodeGenerator(_platform);
            var commandType = platformSpecific.GetCommandType();
            var commandInitialization = platformSpecific.GetCommandInitializationCode();
            var notifyCommandChanged = platformSpecific.GetNotifyCommandChangedCode();


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
                {notifyCommandChanged}
            }}
        }}

        // **Команды**
        public {commandType} AddCommand {{ get; }}
        public {commandType} EditCommand {{ get; }}
        public {commandType} DeleteCommand {{ get; }}
        public {commandType} SaveCommand {{ get; }}

        public {codeClass.Name}ViewModel()
        {{
            LoadItems();
            {commandInitialization}
        }}

        private async void LoadItems() 
        {{
            {initializationCode}
        }}

        private async void AddItem()
        {{
            {addItemCode}
        }}

        private async void EditItem()
        {{
            {editItemCode}
        }}

        private async void DeleteItem()
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

        private void BuildEdit(bool isEdit, bool isDialog, CodeClass codeClass, StringBuilder editItemCode)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!isEdit)
            {
                editItemCode.Append("throw new NotImplementedException();");
            }
            else
            {
                var dialogHandler = new DialogHandler(_platform, codeClass);
                editItemCode.Append(dialogHandler.GetEditDialogCode(isDialog));
            }
        }

        private void BuildAdd(bool isAdd, bool isDialog, CodeClass codeClass, StringBuilder addItemCode)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!isAdd)
            {
                addItemCode.Append("throw new NotImplementedException();");
            }
            else
            {
                var dialogHandler = new DialogHandler(_platform, codeClass);
                addItemCode.Append(dialogHandler.GetAddDialogCode(isDialog));
            }
        }

        private void BuildDelete(bool isDelete, CodeClass codeClass, StringBuilder deleteItemCode)
        {
            if (!isDelete)
            {
                deleteItemCode.Append("throw new NotImplementedException();");
            }
            else
            {
                var dialogHandler = new DialogHandler(_platform, codeClass);
                deleteItemCode.Append(dialogHandler.GetDeleteConfirmationCode());
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