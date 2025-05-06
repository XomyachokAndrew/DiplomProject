using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiplomProject.Services.Resolver;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Builder
{
    public class PropertyControlBuilder
    {
        private readonly TypeResolver _typeResolver = new TypeResolver();

        public string BuildPropertyControl(CodeProperty property)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string label = $"<TextBlock Text=\"{property.Name}\" Margin=\"0,5,0,0\"/>";
            string control = BuildControlByType(property, bindingMode: "");
            return $"{label}\n{control}";
        }

        public string BuildDataBoundPropertyControl(CodeProperty property)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string label = $"<TextBlock Text=\"{property.Name}\" Margin=\"0,5,0,0\"/>";
            string control = BuildControlByType(property, bindingMode: ", Mode=TwoWay");
            return $"{label}\n{control}";
        }

        public string BuildPropertyColumn(CodeProperty property)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dataColumn = BuildColumnByType(property, ", Mode=TwoWay");
            return $@"<DataGridTextColumn {dataColumn}/>";
        }

        public string BuildDataBoundPropertyColumn(CodeProperty property)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dataColumn = BuildColumnByType(property, ", Mode=TwoWay");
            return $@"<DataGridTextColumn {dataColumn}/>";
        }

        private string BuildColumnByType(CodeProperty property, string bindingMode)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var propType = _typeResolver.GetPropertyType(property).ToLower();

            return $"Header=\"{property.Name}\" Binding=\"{{Binding {property.Name}}}\" Width=\"Auto\"";
        }

        private string BuildControlByType(CodeProperty property, string bindingMode)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var propType = _typeResolver.GetPropertyType(property).ToLower();
            string margin = "Margin=\"0,5,0,0\"";
            if (propType.Contains("bool"))
            {
                return $"<CheckBox IsChecked=\"{{Binding {property.Name}{bindingMode}}}\" {margin}/>";
            }
            if (propType.Contains("date"))
            {
                string additionalAttributes = "SelectedDateFormat=\"Short\"";

                return $"<DatePicker SelectedDate=\"{{Binding {property.Name}{bindingMode}}}\" {additionalAttributes} {margin}/>";
            }

            return $"<TextBox Text=\"{{Binding {property.Name}{bindingMode}}}\" {margin}/>";
        }
    }
}
