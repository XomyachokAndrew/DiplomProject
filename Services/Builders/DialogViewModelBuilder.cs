using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiplomProject.Enum;
using DiplomProject.Services.Builder;
using DiplomProject.Services.Resolver;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Builders
{
    public class DialogViewModelBuilder
    {
        private readonly PlatformType _platform;
        private readonly NamespaceResolver _namespaceResolver;
        private readonly PropertyCodeBuilder _propertyCodeBuilder;

        public DialogViewModelBuilder(PlatformType platform) 
        {
            _platform = platform;
            _namespaceResolver = new NamespaceResolver(platform);
            _propertyCodeBuilder = new PropertyCodeBuilder(platform);
        }

        public string BuildDialogViewModelContent(CodeClass codeClass)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var namespaces = _namespaceResolver.GetProjectNamespace(codeClass);

            var propertyCode = new StringBuilder();

            BuildProperty(codeClass, propertyCode);
            var (commandUsings, commandType, closeMethod) = GetPlatformSpecificComponents();

            return $@"{GetUsings(namespaces)}
{commandUsings}

namespace {namespaces["project"]}.ViewModels
{{
    public partial class Dialog{codeClass.Name}ViewModel : INotifyPropertyChanged
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

        public {commandType} SaveCommand {{ get; }}
        public {commandType} CancelCommand {{ get; }}
        public bool IsSaved {{ get; private set; }}

        public Dialog{codeClass.Name}ViewModel({codeClass.Name} item = null, string title = ""Add Product"")
        {{
            _item = item ?? new {codeClass.Name}();
            WindowTitle = title;
            
            SaveCommand = {GetCommandInitialization("Save")};
            CancelCommand = {GetCommandInitialization("Cancel")};
        }}

        public {codeClass.Name} GetItem() => _item;

        private void Save()
        {{
            IsSaved = true;
            {closeMethod}
        }}
        
        private void Cancel()
        {{
            IsSaved = false;
            {closeMethod}
        }}

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {{
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }}
    }}
}}";
        }

        private string GetCommandInitialization(string methodName)
        {
            switch (_platform)
            {
                case PlatformType.WPF:
                    return $"new RelayCommand({methodName})";
                case PlatformType.UWP:
                    return $"new RelayCommand({methodName})";
                case PlatformType.MAUI:
                    return $"new Command({methodName})";
                default:
                    throw new NotImplementedException();
            }
        }

        private string GetUsings(Dictionary<string, string> namespaces)
        {
            var usingsCode = $@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Windows;
using {namespaces["project"]}.Views;
using {namespaces["model"]};
";

            switch (_platform)
            {
                case PlatformType.WPF:
                    return usingsCode + $@"
using System.ComponentModel;";
                case PlatformType.UWP:
                    return usingsCode + $@"
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;";
                case PlatformType.MAUI:
                    return usingsCode + $@"
using System.ComponentModel;
using Microsoft.Maui.Controls;";
                default:
                    return "";
            }
        }

        private (string commandUsings, string commandType, string closeMethod) GetPlatformSpecificComponents()
        {
            string commandUsings;
            string commandType;
            string closeMethod;
            switch (_platform)
            {
                case PlatformType.WPF:
                    commandUsings = "using CommunityToolkit.Mvvm.ComponentModel;\nusing CommunityToolkit.Mvvm.Input;";
                    commandType = "IRelayCommand";
                    closeMethod = "Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)?.Close();";
                    break;
                case PlatformType.UWP:
                    commandUsings = "using Windows.UI.Xaml.Input;";
                    commandType = "ICommand";
                    closeMethod = "// UWP закрытие обрабатывается в коде представления";
                    break;
                case PlatformType.MAUI:
                    commandUsings = "using Microsoft.Maui.Controls;";
                    commandType = "ICommand";
                    closeMethod = "Shell.Current.Navigation.PopModalAsync();";
                    break;
                default:
                    throw new NotImplementedException();
            }

            return (commandUsings, commandType, closeMethod);
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
