using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiplomProject.Services.Builder;
using DiplomProject.Services.Resolver;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Builders
{
    public class DialogViewModelBuilder
    {
        private readonly NamespaceResolver _namespaceResolver = new NamespaceResolver();
        private readonly PropertyCodeBuilder _propertyCodeBuilder = new PropertyCodeBuilder();

        public string BuildDialogViewModelContent(CodeClass codeClass)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var namespaces = _namespaceResolver.GetProjectNamespace(codeClass);

            var propertyCode = new StringBuilder();

            BuildProperty(codeClass, propertyCode);

            return $@"using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using {namespaces["model"]};
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
namespace {namespaces["project"]}.ViewModels
{{
    public class Dialog{codeClass.Name}ViewModel : INotifyPropertyChanged
    {{
        public event PropertyChangedEventHandler PropertyChanged;

        private {codeClass.Name} _item;
        private string _windowTitle;

        public string WindowTitle
        {{
            get => _windowTitle;
            set
            {{
                _windowTitle = value;
                OnPropertyChanged();
            }}
        }}

        {propertyCode}

        public ICommand SaveCommand {{ get; }}
        public ICommand CancelCommand {{ get; }}
        public bool IsSaved {{ get; private set; }}

        public Dialog{codeClass.Name}ViewModel({codeClass.Name} item = null, string title = ""Add Product"")
        {{
            _item = item ?? new {codeClass.Name}();
            WindowTitle = title;
            
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
        }}

        public {codeClass.Name} GetItem() => _item;

        private void Save()
        {{
            IsSaved = true;
            CloseWindow();
        }}
        
        private void Cancel()
        {{
            IsSaved = false;
            CloseWindow();
        }}

        private void CloseWindow()
        {{
            foreach (Window window in Application.Current.Windows)
            {{
                if (window.DataContext == this)
                {{
                    window.Close();
                    break;
                }}
            }}
        }}

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {{
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }}
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
                    propertyCode.AppendLine(_propertyCodeBuilder.BuildPropertyDialogCode(property));
                }
            }
        }
    }
}
