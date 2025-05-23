using System.Collections.Generic;
using System.Text;
using DiplomProject.Enum;
using DiplomProject.Services.Builder;
using DiplomProject.Services.Resolver;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Builders
{
    public class DialogBuilder
    {
        private readonly PlatformType _platform;
        private readonly NamespaceResolver _namespaceResolver;
        private readonly PropertyControlBuilder _propertyControlBuilder;

        public DialogBuilder(PlatformType platform)
        {
            _platform = platform;
            _namespaceResolver = new NamespaceResolver(platform);
            _propertyControlBuilder = new PropertyControlBuilder(platform);
        }

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
            var (rootElement, baseAttributes, container) = GetPlatformSpecificElements();
            var titleProperty = _platform == PlatformType.MAUI ? "Title=\"{Binding Title}\"" : "Title=\"{Binding Title}\"";
            string style = _platform == PlatformType.MAUI ? "" : "Width=\"400\"";

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

            string buttons = BuildDialogButtons(isUseDataBinding);

            return $@"<{rootElement} x:Class=""{namespaces["project"]}.Views.Dialog{codeClass.Name}View""
    {baseAttributes}
    {titleProperty}
    {style}
    mc:Ignorable=""d"">
    <{container} Margin=""10"">
        {controls}
        {buttons}
    </{container}>
</{rootElement}>";
        }

        private string BuildDialogButtons(bool isUseDataBinding)
        {
            var buttons = new StringBuilder();

            if (_platform == PlatformType.MAUI)
            {
                buttons.Append(isUseDataBinding
                    ? @"<Button Text=""Save"" Command=""{Binding SaveCommand}"" WidthRequest=""80"" Margin=""5""/>"
                    : @"<Button x:Name=""SaveButton"" Text=""Save"" WidthRequest=""80"" Margin=""5""/>");
                buttons.Append(isUseDataBinding
                    ? @"<Button Text=""Cancel"" Command=""{Binding CancelCommand}"" WidthRequest=""80"" Margin=""5""/>"
                    : @"<Button x:Name=""CancelButton"" Text=""Cancel"" WidthRequest=""80"" Margin=""5""/>");

                return $@"<HorizontalStackLayout Spacing=""5"" HorizontalOptions=""End"">
            {buttons}
        </HorizontalStackLayout>";
            }
            else if (_platform == PlatformType.UWP)
            {
                buttons.Append(isUseDataBinding
                    ? @"<Button Content=""Save"" Command=""{Binding SaveCommand}"" Width=""80"" Margin=""5""/>"
                    : @"<Button x:Name=""SaveButton"" Content=""Save"" Width=""80"" Margin=""5""/>");
                buttons.Append(isUseDataBinding
                    ? @"<Button Content=""Cancel"" Command=""{Binding CancelCommand}"" Width=""80"" Margin=""5""/>"
                    : @"<Button x:Name=""CancelButton"" Content=""Cancel"" Width=""80"" Margin=""5""/>");

                return $@"<StackPanel Orientation=""Horizontal"" HorizontalAlignment=""Right"" Margin=""0,10,0,0"">
            {buttons}
        </StackPanel>";
            }
            else // WPF
            {
                buttons.Append(isUseDataBinding
                    ? @"<Button Content=""Save"" Command=""{Binding SaveCommand}"" Width=""80"" Margin=""5""/>"
                    : @"<Button x:Name=""SaveButton"" Content=""Save"" Width=""80"" Margin=""5""/>");
                buttons.Append(isUseDataBinding
                    ? @"<Button Content=""Cancel"" Command=""{Binding CancelCommand}"" Width=""80"" Margin=""5""/>"
                    : @"<Button x:Name=""CancelButton"" Content=""Cancel"" Width=""80"" Margin=""5""/>");

                return $@"<StackPanel Orientation=""Horizontal"" HorizontalAlignment=""Right"" Margin=""0,10,0,0"">
            {buttons}
        </StackPanel>";
            }
        }

        private string BuildCsDialog(CodeClass codeClass, Dictionary<string, string> namespaces)
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
                    initializeComponent = $"this.LoadFromXaml(typeof(Dialog{codeClass.Name}View));";
                    usingDirectives = "using Microsoft.Maui.Controls;";
                    break;
                default:
                    break;
            }

            return $@"{usingDirectives}

namespace {namespaces["project"]}.Views
{{
    public partial class Dialog{codeClass.Name}View : {baseClass}
    {{
        public Dialog{codeClass.Name}View()
        {{
            {initializeComponent}
        }}
    }}
}}";
        }

        private (string rootElement, string baseAttributes, string container) GetPlatformSpecificElements()
        {
            string rootElement;
            string baseAttributes;
            string element;

            switch (_platform)
            {
                case PlatformType.UWP:
                    rootElement = "Page";
                    element = "StackPanel";
                    baseAttributes = @"xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                    xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
                    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""";
                    return (rootElement, baseAttributes, element);
                case PlatformType.MAUI:
                    rootElement = "ContentPage";
                    element = "VerticalStackLayout";
                    baseAttributes = @"xmlns=""http://schemas.microsoft.com/dotnet/2021/maui""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""";
                    return (rootElement, baseAttributes, element);
                case PlatformType.WPF:
                    rootElement = "Window";
                    element = "StackPanel";
                    baseAttributes = @"xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                    xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
                    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""";
                    return (rootElement, baseAttributes, element);
                default:
                    return ("", "", "");
            }
        }
    }
}
