using System.Collections.Generic;
using System.Text;
using DiplomProject.Services.Resolver;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;
using System;
using EnvDTE80;

namespace DiplomProject.Services.Builder
{
    public class ViewModelBuilder
    {
        private readonly NamespaceResolver _namespaceResolver = new NamespaceResolver();
        private readonly PropertyCodeBuilder _propertyCodeBuilder = new PropertyCodeBuilder();
        private readonly DataSourceResolver _dataSourceResolver = new DataSourceResolver();

        public string BuildViewModelContent(CodeClass modelClass, string jsonFilePath = null, string dbSetName = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var namespaces = _namespaceResolver.GetProjectNamespace(modelClass);

            var propertyCode = new StringBuilder();
            var editItemCode = new StringBuilder();
            var addItemCode = new StringBuilder();
            var initializationCode = new StringBuilder();
            var saveChangesCode = new StringBuilder();

            string additionalUsings = _dataSourceResolver.GetAdditionalUsings(jsonFilePath, dbSetName);

            if (!additionalUsings.Contains("CommunityToolkit.Mvvm.Input"))
            {
                additionalUsings += "\nusing CommunityToolkit.Mvvm.Input;";
            }
            if (!string.IsNullOrEmpty(dbSetName))
            {
                additionalUsings += $"\nusing {namespaces["project"]}.Context";
            }

            BuildInitializationCode(modelClass, initializationCode, jsonFilePath, dbSetName, namespaces);
            BuildProperty(modelClass, propertyCode);
            BuildEdit(modelClass, editItemCode);
            BuildAdd(modelClass, addItemCode);
            if (jsonFilePath != null || dbSetName != null)
            {
                BuildSaveChanges(modelClass, jsonFilePath, dbSetName, namespaces, saveChangesCode);
            }

            var viewModelContent = BuildViewModelContent(modelClass,
                                                     namespaces,
                                                     additionalUsings,
                                                     propertyCode,
                                                     editItemCode,
                                                     addItemCode,
                                                     initializationCode,
                                                     saveChangesCode,
                                                     jsonFilePath,
                                                     dbSetName);
            return viewModelContent;
        }

        public string BuildViewModelContent(
            CodeClass codeClass,
            Dictionary<string, string> namespaces,
            string additionalUsings,
            StringBuilder propertyCode,
            StringBuilder editItemCode,
            StringBuilder addItemCode,
            StringBuilder initializationCode,
            StringBuilder saveChangesCode,
            string jsonFilePath = null,
            string dbSetName = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return $@"using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Windows;
using {namespaces["project"]}.Views;
{additionalUsings}
using {namespaces["model"]};

namespace {namespaces["project"]}.ViewModels
{{
    public class {codeClass.Name}ViewModel : INotifyPropertyChanged
    {{
        public event PropertyChangedEventHandler? PropertyChanged;
        {(jsonFilePath != null ? $@"private const string JsonFilePath = @""{jsonFilePath}"";" : string.Empty)}
        {(dbSetName != null ? "private readonly AppDbContext _dbContext = new AppDbContext();" : string.Empty)}

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

        // **Свойства для формы добавления/редактирования**
        {propertyCode}

        // **Команды**
        public IRelayCommand AddCommand {{ get; }}
        public IRelayCommand EditCommand {{ get; }}
        public IRelayCommand DeleteCommand {{ get; }}
        {(!string.IsNullOrEmpty(saveChangesCode.ToString()) ? "public IRelayCommand SaveCommand { get; }" : string.Empty)}

        public {codeClass.Name}ViewModel()
        {{
            {(dbSetName != null || jsonFilePath != null ? "LoadItems();" : string.Empty)}
            AddCommand = new RelayCommand(AddItem);
            EditCommand = new RelayCommand(EditItem, () => SelectedItem != null);
            DeleteCommand = new RelayCommand(DeleteItem, () => SelectedItem != null);
            {(!string.IsNullOrEmpty(saveChangesCode.ToString()) ? "SaveCommand = new RelayCommand(SaveChanges);" : string.Empty)}
        }}

        {initializationCode}

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
            if (SelectedItem != null && MessageBox.Show(""Delete this item?"", ""Confirm"", 
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {{
                Items.Remove(SelectedItem);
                SaveChanges();
            }}
            /*
            if (SelectedItem != null)
            {{
                Items.Remove(SelectedItem);
                SelectedItem = null;
            }}
            */
        }}

        {saveChangesCode}

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }}
}}";
        }

        private void BuildProperty(CodeClass codeClass, StringBuilder propertyCode)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (CodeElement member in codeClass.Members)
            {
                if (member is CodeProperty property)
                {
                    propertyCode.AppendLine(_propertyCodeBuilder.BuildPropertyCode(property));
                }
            }
        }

        private void BuildEdit(CodeClass codeClass, StringBuilder editItemCode)
        {
            var itemProp = new StringBuilder();
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (CodeElement member in codeClass.Members)
            {
                if (member is CodeProperty property)
                {
                    itemProp.AppendLine($@"SelectedItem.{property.Name} = updatedItem.{property.Name};");
                }
            }
            var itemProperty = new StringBuilder();
            foreach (CodeElement member in codeClass.Members)
            {
                if (member is CodeProperty property)
                {
                    itemProperty.AppendLine($@"{property.Name} = SelectedItem.{property.Name},");
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

        private void BuildAdd(CodeClass codeClass, StringBuilder addItemCode)
        {
            var itemProp = new StringBuilder();
            var clearProp = new StringBuilder();

            ThreadHelper.ThrowIfNotOnUIThread();
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

            /*
            var item = new {codeClass.Name}
            {{
                Id = Items.Count() + 1,
                {itemProp}
            }};
            Items.Add(item);
            */
");
        }

        private void BuildSaveChanges(CodeClass modelClass, string jsonFilePath, string dbSetName, Dictionary<string, string> namespaces, StringBuilder saveChanges)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            saveChanges.AppendLine($@"private void SaveChanges()
        {{
            try
            {{

                {(jsonFilePath != null ? SaveToJson(modelClass) :
                dbSetName != null ? SaveToDatabase(modelClass, namespaces) : "")}
            }}
            catch (Exception ex)
            {{
                System.Diagnostics.Debug.WriteLine($""Error saving changes: {{ex.Message}}"");
            }}
        }}");
        }

        private string SaveToJson(CodeClass codeClass)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return $@"
            var options = new JsonSerializerOptions {{WriteIndented = true}}; // Красивый формат JSON
            string json = JsonSerializer.Serialize(Items, options);
            File.WriteAllText(JsonFilePath, json);";
        }

        private string SaveToDatabase(CodeClass modelClass, Dictionary<string, string> namespaces)
        {
            string dbContextName = _dataSourceResolver.GetDbContextName(modelClass, namespaces);
            return $@"_dbContext.SaveChanges();";
        }

        private void BuildInitializationCode(
            CodeClass modelClass,
            StringBuilder initializationCode,
            string jsonFilePath,
            string dbSetName,
            Dictionary<string, string> namespaces)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!string.IsNullOrEmpty(jsonFilePath))
            {
                initializationCode.AppendLine($@"
        private void LoadItems() 
        {{
            if (File.Exists(JsonFilePath))
            {{
                try
                {{
                    string json = File.ReadAllText(JsonFilePath);
                    var items = JsonSerializer.Deserialize<List<{modelClass.Name}>>(json);
                    if (items != null)
                        Items = new ObservableCollection<{modelClass.Name}>(items);
                }}
                catch (Exception ex)
                {{
                    Console.WriteLine($""Ошибка загрузки данных: {{ex.Message}}"");
                }}
            }}
        }}");
            }
            else if (!string.IsNullOrEmpty(dbSetName))
            {
                string dbContextName = _dataSourceResolver.GetDbContextName(modelClass, namespaces);
                initializationCode.AppendLine($@"
        private void LoadItems() 
        {{
            _dbContext.{modelClass.Name}.Load(); // Загружаем данные в DbSet
            Items = _dbContext.{modelClass.Name}.Local.ToObservableCollection();
        }}
            // Загрузка данных из БД
            using (var db = new {dbContextName}())
            {{
                Items = db.{dbSetName}.ToList() ?? new List<{modelClass.Name}>();
            }}");
            }
        }
    }
}