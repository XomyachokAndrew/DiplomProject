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

        public string BuildViewModelContent(CodeClass modelClass)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var namespaces = _namespaceResolver.GetProjectNamespace(modelClass);

            var propertiesCode = new StringBuilder();
            var commandsCode = new StringBuilder();

            BuildMembersCode(modelClass, propertiesCode, commandsCode);

            return $@"using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using {namespaces["model"]};

namespace {namespaces["project"]}.ViewModels
{{
    public class {modelClass.Name}ViewModel : INotifyPropertyChanged
    {{
        private readonly {modelClass.Name} _model;

        public {modelClass.Name}ViewModel({modelClass.Name} model)
        {{
            _model = model ?? throw new ArgumentNullException(nameof(model));
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
    }
}
