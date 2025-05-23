using System.Collections.Generic;
using System.Text;
using DiplomProject.Enum;
using DiplomProject.Services.Resolver;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Builder
{
    public class XamlBuilder
    {
        private readonly PlatformType _platform;
        private readonly NamespaceResolver _namespaceResolver;
        private readonly PropertyControlBuilder _propertyControlBuilder;

        public XamlBuilder(PlatformType platform) 
        {
            _platform = platform;
            _namespaceResolver = new NamespaceResolver(platform);
            _propertyControlBuilder = new PropertyControlBuilder(platform);
        }
        public (string xamlContent, string csContent) BuildViewContent(
            CodeClass codeClass, 
            bool isUseDataBinding)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var namespaces = _namespaceResolver.GetProjectNamespace(codeClass);

            string xamlContent = BuildXamlContent(codeClass, isUseDataBinding, namespaces);
            string csContent = BuildCsForXamlContent(codeClass, isUseDataBinding, namespaces);

            return (xamlContent, csContent);
        }

        private string BuildXamlContent(
            CodeClass codeClass, 
            bool isUseDataBinding, 
            Dictionary<string, string> namespaces)
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

            var (rootElement, baseAttributes, container) = GetPlatformSpecificElements();

            var addButton = BuildAddButton(isUseDataBinding);

            var actionButtons = BuildActionButtons(isUseDataBinding);
            
            var itemsControl = BuildItemsControl(columns, actionButtons, addButton, isUseDataBinding);

            return $@"<{rootElement} x:Class=""{namespaces["project"]}.Views.{codeClass.Name}View""
                    {baseAttributes}
                    xmlns:local=""clr-namespace:{namespaces["project"]}""
                    mc:Ignorable=""d""
                    Title=""{codeClass.Name}View"" {(_platform != PlatformType.MAUI ? @"Height=""450"" Width=""800""" : "")}>
                <{container}>
                    {addButton}
                    {itemsControl}
                </{container}>
            </{rootElement}>";
        }

        private string BuildItemsControl(StringBuilder columns, string actionButtons, string addButton, bool isUseDataBinding)
        {
            var itemsSource = isUseDataBinding ? $@"ItemsSource=""{{Binding Items}}"" 
                        SelectedItem=""{{Binding SelectedItem, Mode=TwoWay}}""" : "";

            switch (_platform)
            {
                case PlatformType.MAUI:
                    return $@"<CollectionView
                        {itemsSource}
                        >
                        <CollectionView.ItemTemplate>
                            <DataTemplate>
                                <VerticalStackLayout>
                                    {columns}
                                    {actionButtons}
                                </VerticalStackLayout>
                            </DataTemplate>
                        </CollectionView.ItemTemplate>
                    </CollectionView>";
                case PlatformType.WPF:
                    return $@"<DataGrid {(!string.IsNullOrEmpty(addButton) ? @"Grid.Row=""1""" : "")}
                        {itemsSource}
                        AutoGenerateColumns=""False"" 
                        IsReadOnly=""True"">
                        <DataGrid.Columns>
                            {columns}
                            {actionButtons}
                        </DataGrid.Columns>
                    </DataGrid>";
                default:
                    return "";
            }
            ;
        }

        private (string rootElement, string baseAttributes, string container) GetPlatformSpecificElements()
        {
            string rootElement;
            string baseAttributes;
            string container;

            switch (_platform)
            {
                case PlatformType.UWP:
                    rootElement = "Page";
                    container = "Grid";
                    baseAttributes = @"xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                    xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
                    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""";
                    return (rootElement, baseAttributes, container);
                case PlatformType.MAUI:
                    rootElement = "ContentPage";
                    container = "VerticalStackLayout";
                    baseAttributes = @"xmlns=""http://schemas.microsoft.com/dotnet/2021/maui""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""";
                    return (rootElement, baseAttributes, container);
                case PlatformType.WPF:
                    rootElement = "Window";
                    container = "Grid";
                    baseAttributes = @"xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                    xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
                    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""";
                    return (rootElement, baseAttributes, container);
                default:
                    return ("", "", "");
            }
        }

        private string BuildAddButton(bool isUseDataBinding)
        {

            string addButton;

            if (_platform == PlatformType.MAUI)
            {
                addButton = isUseDataBinding
                    ? @"<Button Text=""Add"" Command=""{Binding AddCommand}"" WidthRequest=""80"" Margin=""5,0""/>"
                    : @"<Button Text=""Add"" WidthRequest=""80"" Margin=""5,0""/>";
            }
            else
            {

                addButton = isUseDataBinding
                    ? @"<Button Content=""Add"" Command=""{Binding AddCommand}"" Width=""80"" Margin=""5,0""/>"
                    : @"<Button Content=""Add"" Width=""80"" Margin=""5,0""/>";
            }
            if (_platform == PlatformType.MAUI) 
            {
                return $@"
            <HorizontalStackLayout Spacing=""5"" HorizontalOptions=""End"">
                {addButton}
            </HorizontalStackLayout>";
            }
            return $@"<Grid.RowDefinitions>
                        <RowDefinition></RowDefinition>
                        <RowDefinition></RowDefinition>
                    </Grid.RowDefinitions>
                    <StackPanel Grid.Row=""0"" Margin=""0,0,0,10"">
                        {addButton}
                    </StackPanel>";
        }

        private string BuildActionButtons(bool isUseDataBinding)
        {

            var buttons = new List<string>();
            if (_platform != PlatformType.MAUI)
            {
                    buttons.Add(isUseDataBinding ? $@"<Button Content=""Edit"" 
                                                Command=""{{Binding DataContext.EditCommand, RelativeSource={{RelativeSource AncestorType=DataGrid}}}}"" 
                                                CommandParameter=""{{Binding}}"" 
                                                Margin=""2"" Width=""60""/>" : $@"<Button Content=""Edit"" Margin=""2"" Width=""60""/>");
                    buttons.Add(isUseDataBinding ? $@"<Button Content=""Delete"" 
                                                Command=""{{Binding DataContext.DeleteCommand, RelativeSource={{RelativeSource AncestorType=DataGrid}}}}"" 
                                                CommandParameter=""{{Binding}}"" 
                                                Margin=""2"" Width=""60""/>" : $@"<Button Content=""Delete"" Margin=""2"" Width=""60""/>");

                if (buttons.Count == 0) return string.Empty;

                return $@"<DataGridTemplateColumn Width=""*"" Header=""Actions"">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <StackPanel Orientation=""Horizontal"">
                                    {string.Join("\n", buttons)}
                                </StackPanel>
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>";

            }
            else if (_platform == PlatformType.MAUI)
            {
                    buttons.Add(isUseDataBinding
                        ? @"<Button Text=""Edit"" Command=""{Binding EditCommand}"" WidthRequest=""60""/>"
                        : @"<Button Text=""Edit"" WidthRequest=""60""/>");
                    buttons.Add(isUseDataBinding
                        ? @"<Button Text=""Delete"" Command=""{Binding DeleteCommand}"" WidthRequest=""60""/>"
                        : @"<Button Text=""Delete"" WidthRequest=""60""/>");

                return buttons.Count > 0
                    ? $@"
            <HorizontalStackLayout Spacing=""5"" HorizontalOptions=""End"">
                {string.Join("\n", buttons)}
            </HorizontalStackLayout>"
                    : string.Empty;
            }
            else
            {
                return string.Empty;
            }
        }

        private string BuildCsForXamlContent(CodeClass modelClass, bool isUseDataBinding, Dictionary<string, string> namespaces)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string baseClass = "";
            string initializeComponent = "";
            string usingDirectives = "";
            switch (_platform)
            {
                case PlatformType.WPF:
                    baseClass = "Window";
                    initializeComponent = "InitializeComponent();";
                    usingDirectives = "using System.Windows;";
                    break;
                case PlatformType.UWP:
                    baseClass = "Page";
                    initializeComponent = "InitializeComponent();";
                    usingDirectives = "using Windows.UI.Xaml.Controls;";
                    break;
                case PlatformType.MAUI:
                    baseClass = "ContentPage";
                    initializeComponent = $"this.LoadFromXaml(typeof({modelClass.Name}View));";
                    usingDirectives = "using Microsoft.Maui.Controls;";
                    break;
                default:
                    break;
            }

            string viewModelBinding = isUseDataBinding
                ? _platform == PlatformType.MAUI
                    ? $"BindingContext = new {modelClass.Name}ViewModel();"
                    : $"DataContext = new {modelClass.Name}ViewModel();"
                : "";

            return $@"{usingDirectives}
{(isUseDataBinding ? $"using {namespaces["project"]}.ViewModels;" : "")}

namespace {namespaces["project"]}.Views
{{
    public partial class {modelClass.Name}View : {baseClass} 
    {{
        public {modelClass.Name}View()
        {{
            {initializeComponent}
            {viewModelBinding}
        }}
    }}    
}}";
        }
    }
}
