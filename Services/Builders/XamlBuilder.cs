using System.Collections.Generic;
using System.Text;
using DiplomProject.Services.Resolver;
using EnvDTE;
using EnvDTE80;
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
            string csContent = BuildCsForXamlContent(codeClass, isUseDataBinding, namespaces);

            return (xamlContent, csContent);
        }

        private string BuildXamlContent(CodeClass codeClass, bool isUseDataBinding, Dictionary<string, string> namespaces)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var columns = new StringBuilder();
            foreach (CodeElement member in codeClass.Members)
            {
                if (member is CodeProperty property)
                {
                    var dataProperty = isUseDataBinding
                        ? _propertyControlBuilder.BuildDataBoundPropertyColumn(property) : _propertyControlBuilder.BuildPropertyColumn(property);
                    columns.AppendLine(dataProperty);
                }
            }

            return $@"<Window x:Class=""{namespaces["project"]}.Views.{codeClass.Name}View""
                    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                    xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
                    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
                    xmlns:local=""clr-namespace:{namespaces["project"]}""
                    mc:Ignorable=""d""
                    Title=""{codeClass.Name}View"" Height=""450"" Width=""800"">
                <Grid>
                    <DataGrid ItemsSource=""{{Binding Items}}"" AutoGenerateColumns=""False"" IsReadOnly=""True"">
                        <DataGrid.Columns>
                            {columns}
                        </DataGrid.Columns>
                    </DataGrid>
                </Grid>
            </Window>";
        }

        private string BuildCsForXamlContent(CodeClass modelClass, bool isUseDataBinding, Dictionary<string, string> namespaces)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string viewModelBinding = isUseDataBinding
                ? $"DataContext = new {modelClass.Name}ViewModel();"
                : "";

            return $@"using System.Windows;
using {namespaces["project"]}.ViewModels;

namespace {namespaces["project"]}.Views
{{
    public partial class {modelClass.Name}View : Window 
    {{
        public {modelClass.Name}View()
        {{
            InitializeComponent();
            {viewModelBinding}
        }}
    }}    
}}";
        }
    }
}
