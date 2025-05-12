using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiplomProject.Services.Builder;
using DiplomProject.Services.Resolver;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Builders
{
    public class DialogBuilder
    {
        private readonly NamespaceResolver _namespaceResolver = new NamespaceResolver();
        private readonly PropertyControlBuilder _propertyControlBuilder = new PropertyControlBuilder();

        public (string xamlContent, string csContent) BuildDialogContent(CodeClass codeClass, bool isUseDataBinding)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var namespaces = _namespaceResolver.GetProjectNamespace(codeClass);

            string xamlContent = BuildXamlDialog(codeClass, namespaces, isUseDataBinding);

            string csContent = BuildCsDialog(codeClass, namespaces);

            return (xamlContent, csContent);
        }

        private string BuildXamlDialog(CodeClass codeClass, Dictionary<string, string> namespaces, bool isUseDataBinding)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var controls = new StringBuilder();
            foreach (CodeElement member in codeClass.Members)
            {
                if (member is CodeProperty property)
                {
                    var dataProperty = isUseDataBinding
                        ? _propertyControlBuilder.BuildDataBoundPropertyControl(property) : _propertyControlBuilder.BuildPropertyControl(property);
                    controls.AppendLine(dataProperty);
                }
            }

            return $@"<Window x:Class=""{namespaces["project"]}.Views.Dialog{codeClass.Name}View""
                    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                    xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
                    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
                    mc:Ignorable=""d""
                    Width=""400"">
    <StackPanel Margin=""10"">
        {controls}
        <StackPanel Orientation=""Horizontal"" HorizontalAlignment=""Right"" Margin=""0,10,0,0"">
            <Button Content=""Save"" Command=""{{Binding SaveCommand}}"" Width=""80"" Margin=""5""/>
            <Button Content=""Cancel"" Command=""{{Binding CancelCommand}}"" Width=""80"" Margin=""5""/>
        </StackPanel>
    </StackPanel>
</Window>";
        }

        private string BuildCsDialog(CodeClass codeClass, Dictionary<string, string> namespaces)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return $@"using System.Windows;

namespace {namespaces["project"]}.Views
{{
    public partial class Dialog{codeClass.Name}View : Window
    {{
        public Dialog{codeClass.Name}View()
        {{
            InitializeComponent();
        }}
    }}
}}";
        }
    }
}
