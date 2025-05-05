using System;
using System.Collections.Generic;
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

        private string BuildControlByType(CodeProperty property, string bindingMode)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var propType = _typeResolver.GetPropertyType(property).ToLower();
            string margin = "Margin=\"0,5,0,0\"";
            string additionalAttributes = "";

            if (propType.Contains("bool"))
            {
                return $"<CheckBox IsChecked=\"{{Binding {property.Name}{bindingMode}}}\" {margin}/>";
            }
            if (propType.Contains("date"))
            {
                additionalAttributes = "SelectedDateFormat=\"Short\"";
                if (bindingMode.Contains("TwoWay"))
                {
                    additionalAttributes += " UpdateSourceTrigger=\"PropertyChanged\"";
                }
                return $"<DatePicker SelectedDate=\"{{Binding {property.Name}{bindingMode}}}\" {additionalAttributes} {margin}/>";
            }

            return $"<TextBox Text=\"{{Binding {property.Name}{bindingMode}}}\" {margin}/>";
        }
    }
}
