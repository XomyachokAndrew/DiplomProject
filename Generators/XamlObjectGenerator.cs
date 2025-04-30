using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using EnvDTE;

namespace DiplomProject.Data
{
    public class XamlObjectGenerator
    {
        public static void AddFieldForProperty(StackPanel parent, CodeProperty property, bool useDataBinding, bool addValidation)
        {
            string typeName = property.Type.AsString;
            string propertyName = property.Name;
            string label = SplitCamelCase(propertyName) + ":";

            if (typeName == "string" || typeName.Contains("String"))
            {
                AddTextField(parent, propertyName, label, useDataBinding, addValidation);
            }
            else if (typeName == "bool" || typeName.Contains("Boolean"))
            {
                AddCheckBoxField(parent, propertyName, label, useDataBinding, addValidation);
            }
            else if (typeName == "DateTime" || typeName.Contains("DateTime"))
            {
                AddDateField(parent, propertyName, label, useDataBinding);
            }
            else if (IsNumericType(typeName))
            {
                AddNumericField(parent, propertyName, label, useDataBinding, addValidation);
            }
            else
            {
                AddTextField(parent, propertyName, label, useDataBinding, false);
            }
        }

        private static void AddTextField(Panel parent, string propertyName, string label, bool useDataBinding, bool addValidation)
        {
            var textBlock = new TextBlock { Text = label, Margin = new Thickness(0, 5, 0, 2) };

            parent.Children.Add(textBlock);

            var textBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };

            if (useDataBinding)
            {
                var binding = new Binding(propertyName)
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                };

                if (addValidation)
                {
                    binding.ValidatesOnDataErrors = true;
                    binding.NotifyOnValidationError = true;
                }

                textBox.SetBinding(TextBlock.TextProperty, binding);
            }
            parent.Children.Add(textBox);
        }

        private static void AddCheckBoxField(Panel parent, string propertyName, string label, bool useDataBinding, bool addValidation)
        {
            var checkBox = new CheckBox { Content = label, Margin = new Thickness(0, 5, 0, 10) };

            if (useDataBinding)
            {
                checkBox.SetBinding(CheckBox.IsCheckedProperty,
                    new Binding(propertyName) { Mode = BindingMode.TwoWay });
            }

            parent.Children.Add(checkBox);
        }

        private static void AddNumericField(Panel parent, string propertyName, string label, bool useDataBinding, bool addValidation)
        {
            var textBlock = new TextBlock { Text = label, Margin = new Thickness(0, 5, 0, 2) };
            parent.Children.Add(textBlock);

            var textBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };

            if (useDataBinding)
            {
                var binding = new Binding(propertyName)
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                if (addValidation)
                {
                    binding.ValidatesOnDataErrors = true;
                    binding.NotifyOnValidationError = true;
                    // Можно добавить ValidationRule для чисел
                }

                textBox.SetBinding(TextBox.TextProperty, binding);
            }

            parent.Children.Add(textBox);
        }

        private static void AddDateField(Panel parent, string propertyName, string label, bool useDataBinding)
        {
            var textBlock = new TextBlock { Text = label, Margin = new Thickness(0, 5, 0, 2) };
            parent.Children.Add(textBlock);

            var datePicker = new DatePicker { Margin = new Thickness(0, 0, 0, 10) };

            if (useDataBinding)
            {
                datePicker.SetBinding(DatePicker.SelectedDateProperty,
                    new Binding(propertyName) { Mode = BindingMode.TwoWay });
            }

            parent.Children.Add(datePicker);
        }

        private static string SplitCamelCase(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                input,
                "([A-Z])",
                " $1",
                System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
        }

        private static bool IsNumericType(string typeName)
        {
            string[] numericTypes = { "int", "Int32", "long", "Int64", "decimal", "Decimal",
                            "double", "Double", "float", "Single", "short", "Int16" };

            return numericTypes.Contains(typeName);
        }
    }
}
