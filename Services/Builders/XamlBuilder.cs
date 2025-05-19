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

        public (string xamlContent, string csContent) BuildViewContent(
            CodeClass codeClass, 
            bool isUseDataBinding, 
            bool isAddingButton, 
            bool isEditingButton, 
            bool isDeletingButton)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var namespaces = _namespaceResolver.GetProjectNamespace(codeClass);

            string xamlContent = BuildXamlContent(codeClass, isUseDataBinding, namespaces, isAddingButton, isEditingButton, isDeletingButton);
            string csContent = BuildCsForXamlContent(codeClass, isUseDataBinding, namespaces);

            return (xamlContent, csContent);
        }

        private string BuildXamlContent(
            CodeClass codeClass, 
            bool isUseDataBinding, 
            Dictionary<string, string> namespaces, 
            bool isAddingButton, 
            bool isEditingButton, 
            bool isDeletingButton)
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

            var editButton = isEditingButton ? (isUseDataBinding ? $@"<Button Content=""Edit"" 
                                                Command=""{{Binding DataContext.EditCommand, RelativeSource={{RelativeSource AncestorType=DataGrid}}}}"" 
                                                CommandParameter=""{{Binding}}"" 
                                                Margin=""2"" Width=""60""/>" : $@"<Button Content=""Edit"" Margin=""2"" Width=""60""/>") : "";
            var deleteButton = isDeletingButton ? (isUseDataBinding ? $@"<Button Content=""Delete"" 
                                                Command=""{{Binding DataContext.DeleteCommand, RelativeSource={{RelativeSource AncestorType=DataGrid}}}}"" 
                                                CommandParameter=""{{Binding}}"" 
                                                Margin=""2"" Width=""60""/>" : $@"<Button Content=""Delete"" Margin=""2"" Width=""60""/>") : "";

            var addButton = isAddingButton ? (isUseDataBinding ? $@"<Button Content=""Add"" Command=""{{Binding AddCommand}}"" Width=""80"" Margin=""5,0""/>"
                : $@"<Button Content=""Add"" Width=""80"" Margin=""5,0""/>") : "";

            if (isEditingButton || isDeletingButton)
            {
                columns.AppendLine($@"<DataGridTemplateColumn Width=""*"" Header=""Actions"">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <StackPanel Orientation=""Horizontal"">
                                        {editButton}
                                        {deleteButton}
                                    </StackPanel>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>");
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
                    {(!string.IsNullOrEmpty(addButton) ? $@"<Grid.RowDefinitions>
                        <RowDefinition></RowDefinition>
                        <RowDefinition></RowDefinition>
                    </Grid.RowDefinitions>
                    <StackPanel Grid.Row=""0"" Orientation=""Horizontal"" HorizontalAlignment=""Right"" Margin=""0,0,0,10"">
                        {addButton}
                    </StackPanel>" : "")}
                    <DataGrid 
                        {(!string.IsNullOrEmpty(addButton) ? @"Grid.Row=""1""" : "")}
                        {(isUseDataBinding ? $@"ItemsSource=""{{Binding Items}}"" 
                        SelectedItem=""{{Binding SelectedItem, Mode=TwoWay}}""" : "")}
                        AutoGenerateColumns=""False"" 
                        IsReadOnly=""True""
                        >
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
{(isUseDataBinding ? $"using {namespaces["project"]}.ViewModels;" : "")}

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
