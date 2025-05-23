using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiplomProject.Enum;
using DiplomProject.Services.Resolver;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Builder
{
    public class PropertyControlBuilder
    {
        private readonly PlatformType _platform;
        private readonly TypeResolver _typeResolver;

        public PropertyControlBuilder(PlatformType platform)
        {
            _platform = platform;
            _typeResolver = new TypeResolver();
        }

        #region Control Generation
        public string BuildPropertyControl(CodeProperty property)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return BuildControlByType(property, useDataBinding: false);
        }

        public string BuildDataBoundPropertyControl(CodeProperty property)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return BuildControlByType(property, useDataBinding: true);
        }

        private string BuildControlByType(CodeProperty property, bool useDataBinding)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var propType = _typeResolver.GetPropertyType(property).ToLower();
            string binding = useDataBinding ? GetBindingExpression(property.Name) : "";

            if (_platform == PlatformType.MAUI)
            {
                return BuildMauiControl(propType, property.Name, binding);
            }
            return BuildWpfUwpControl(propType, property.Name, binding);
        }

        private string BuildWpfUwpControl(string propType, string propName, string binding)
        {
            string label = $"<TextBlock Text=\"{propName}\" Margin=\"0,5,0,0\"/>";
            string control;

            if (propType.Contains("bool"))
            {
                control = $"<CheckBox IsChecked=\"{binding}\" Margin=\"0,5,0,0\"/>";
            }
            else if (propType.Contains("date"))
            {
                control = $"<DatePicker SelectedDate=\"{binding}\" SelectedDateFormat=\"Short\" Margin=\"0,5,0,0\"/>";
            }
            else
            {
                control = $"<TextBox Text=\"{binding}\" Margin=\"0,5,0,0\"/>";
            }
            
            return $"{label}\n{control}";
        }

        private string BuildMauiControl(string propType, string propName, string binding)
        {
            string label = $"<Label Text=\"{propName}\" Margin=\"0,5,0,0\"/>";
            string control;

            if (propType.Contains("bool"))
            {
                control = $"<CheckBox IsChecked=\"{binding}\" Margin=\"0,5,0,0\"/>";
            }
            else if (propType.Contains("date"))
            {
                control = $"<DatePicker Date=\"{binding}\" Margin=\"0,5,0,0\"/>";
            }
            else if (propType.Contains("int") || propType.Contains("double") || propType.Contains("decimal"))
            {
                control = $"<Entry Text=\"{binding}\" Keyboard=\"Numeric\" Margin=\"0,5,0,0\"/>";
            }
            else
            {
                control = $"<Entry Text=\"{binding}\" Margin=\"0,5,0,0\"/>";
            }
            
            return $"{label}\n{control}";
        }
        #endregion

        #region Column Generation
        public string BuildPropertyColumn(CodeProperty property)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return BuildColumnByType(property, useDataBinding: false);
        }

        public string BuildDataBoundPropertyColumn(CodeProperty property)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return BuildColumnByType(property, useDataBinding: true);
        }

        private string BuildColumnByType(CodeProperty property, bool useDataBinding)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_platform == PlatformType.MAUI)
            {
                // Для MAUI CollectionView используются шаблоны, а не колонки
                return BuildMauiDataTemplate(property, useDataBinding); ;
            }

            string baseColumn = $@"Header=""{property.Name}"" Width=""Auto""";

            if (!useDataBinding)
            {
                return $"<DataGridTextColumn {baseColumn}/>";
            }

            string binding = GetBindingExpression(property.Name, forColumn: true);
            return $"<DataGridTextColumn {baseColumn} {binding}/>";
        }

        private string BuildMauiDataTemplate(CodeProperty property, bool useDataBinding)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string propertyName = property.Name;
            string propType = _typeResolver.GetPropertyType(property).ToLower();
            string binding = useDataBinding ? GetBindingExpression(propertyName, forColumn: true) : "";

            string control;

            if (propType.Contains("bool"))
            {
                control = $@"<CheckBox IsChecked=""{binding}"" HorizontalOptions=""Start"" VerticalOptions=""Center"" />";
            }
            else if (propType.Contains("date"))
            {
                control = $@"<DatePicker Date=""{binding}"" HorizontalOptions=""Fill"" VerticalOptions=""Center"" />";
            }
            else
            {
                control = $@"<Label Text=""{binding}"" HorizontalOptions=""Fill"" VerticalOptions=""Center"" FontSize=""14"" />";
            }

            return $@"<VerticalStackLayout Spacing=""5"" Margin=""5"">
                <Label Text=""{propertyName}"" FontSize=""12"" TextColor=""Gray""/>
                {control}
              </VerticalStackLayout>";
        }
        #endregion

        private string GetBindingExpression(string propertyName, bool forColumn = false)
        {
            if (string.IsNullOrEmpty(propertyName))
                return string.Empty;

            return forColumn
                ? $@"Binding=""{{Binding {propertyName}}}"""
                : $@"""{{Binding {propertyName}, UpdateSourceTrigger=PropertyChanged}}""";
        }
    }
}
