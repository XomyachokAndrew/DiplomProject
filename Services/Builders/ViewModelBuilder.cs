using System.Collections.Generic;
using System.Text;
using DiplomProject.Services.Resolver;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Builder
{
    public class ViewModelBuilder
    {
        private readonly NamespaceResolver _namespaceResolver = new NamespaceResolver();
        private readonly PropertyCodeBuilder _propertyCodeBuilder = new PropertyCodeBuilder();
        private readonly CommandCodeBuilder _commandCodeBuilder = new CommandCodeBuilder();
        private readonly DataSourceResolver _dataSourceResolver = new DataSourceResolver();

        public string BuildViewModelContent(CodeClass modelClass, string jsonFilePath = null, string dbSetName = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var namespaces = _namespaceResolver.GetProjectNamespace(modelClass);

            var propertiesCode = new StringBuilder();
            var commandsCode = new StringBuilder();
            var collectionsCode = new StringBuilder();
            var initializationCode = new StringBuilder();

            BuildMembersCode(modelClass, propertiesCode, commandsCode);
            BuildCollectionsCode(modelClass, collectionsCode);
            BuildInitializationCode(modelClass, initializationCode, jsonFilePath, dbSetName, namespaces);

            string additionalUsings = _dataSourceResolver.GetAdditionalUsings(jsonFilePath, dbSetName);

            return $@"using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
{additionalUsings}
using {namespaces["model"]};

namespace {namespaces["project"]}.ViewModels
{{
    public class {modelClass.Name}ViewModel : INotifyPropertyChanged
    {{
        private {modelClass.Name} _model;
        {collectionsCode}

        public {modelClass.Name}ViewModel()
        {{
            {initializationCode}
            {_commandCodeBuilder.GenerateCommandInitializations(modelClass)}
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

        private void BuildMembersCode(CodeClass modelClass, StringBuilder propertiesCode, StringBuilder commandsCode)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (CodeElement member in modelClass.Members)
            {
                if (member is CodeProperty property)
                {
                    propertiesCode.AppendLine(_propertyCodeBuilder.BuildPropertiesCode(property));
                }
                else if (member is CodeFunction function && function.Access == vsCMAccess.vsCMAccessPublic)
                {
                    commandsCode.AppendLine(_commandCodeBuilder.BuildCommandCode(function));
                }
            }
        }

        private void BuildCollectionsCode(CodeClass modelClass, StringBuilder collectionsCode)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            collectionsCode.AppendLine($@"
        private List<{modelClass.Name}> _items;
        public List<{modelClass.Name}> Items
        {{
            get => _items;
            set
            {{
                _items = value;
                OnPropertyChanged();
            }}
        }}");
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
            // Загрузка данных из JSON
            var json = File.ReadAllText(@""{jsonFilePath}"");
            Items = JsonSerializer.Deserialize<List<{modelClass.Name}>>(json);
            _model = new {modelClass.Name}();");
            }
            else if (!string.IsNullOrEmpty(dbSetName))
            {
                string dbContextName = _dataSourceResolver.GetDbContextName(modelClass, namespaces);
                initializationCode.AppendLine($@"
            // Загрузка данных из БД
            using (var db = new {dbContextName}())
            {{
                Items = db.{dbSetName}.ToList();
            }}
            _model = new {modelClass.Name}();");
            }
            else
            {
                initializationCode.AppendLine($@"
            // Инициализация без внешних данных
            _model = new {modelClass.Name}();
            Items = new List<{modelClass.Name}>();");
            }
        }
    }
}