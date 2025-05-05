using System.Collections.Generic;
using System.Text;
using DiplomProject.Services.Resolver;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Builder
{
    public class XamlBuilder
    {
        private readonly NamespaceResolver _namespaceResolver = new NamespaceResolver();
        private readonly PropertyControlBuilder _propertyControlBuilder = new PropertyControlBuilder();

        public (string xamlContent, string csContent) BuildViewContent(CodeClass codeClass, bool isUseDataBinding)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var namespaces = _namespaceResolver.GetProjectNamespace(codeClass);

            string xamlContent = BuildXamlContent(codeClass, isUseDataBinding, namespaces);
            string csContent = BuildCsForXamlContent(codeClass, namespaces);

            return (xamlContent, csContent);
        }

        private string BuildXamlContent(CodeClass codeClass, bool isUseDataBinding, Dictionary<string, string> namespaces)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var controls = new StringBuilder();
            foreach (CodeElement member in codeClass.Members)
            {
                if (member is CodeProperty property)
                {
                    var dataProperty = isUseDataBinding
                        ? _propertyControlBuilder.BuildDataBoundPropertyControl(property)
                        : _propertyControlBuilder.BuildPropertyControl(property);
                    controls.AppendLine(dataProperty);
                }
            }

            string viewModelBinding = isUseDataBinding
                ? $"\n        DataContext=\"{{Binding {codeClass.Name}ViewModel}}\""
                : "";

            return $@"<Window x:Class=""{namespaces["project"]}.Views.{codeClass.Name}View""
        xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
        xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
        xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
        xmlns:local=""clr-namespace:{namespaces["project"]}""
        mc:Ignorable=""d""
        Title=""{codeClass.Name}View"" Height=""450"" Width=""800""{viewModelBinding}>
    <Grid>
        <StackPanel>
            {controls}
        </StackPanel>
    </Grid>
</Window>";
        }

        private string BuildCsForXamlContent(CodeClass modelClass, Dictionary<string, string> namespaces)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return $@"
using System.Windows;

namespace {namespaces["project"]}.Views
{{
    public partial class {modelClass.Name}View : Window 
    {{
        public {modelClass.Name}View()
        {{
            InitializeComponent();
        }}
    }}    
}}";
        }
    }
}
