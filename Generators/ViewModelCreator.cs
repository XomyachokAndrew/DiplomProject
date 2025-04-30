using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

namespace DiplomProject.Data
{
    public class ViewModelCreator
    {
        public static void GenerateViewModel(CodeClass codeClass, bool useDataBinding, bool addValidation, string projectPath, ProjectItem selectedItem)
        {
            string viewModelName = $"{codeClass.Name}ViewModel";
            string fileName = $"{viewModelName}.cs";
            string filePath = Path.Combine(projectPath, fileName);

            string viewModelContent = BuildViewModelContent(codeClass, viewModelName, useDataBinding, addValidation);

            File.WriteAllText(filePath, viewModelContent);
            selectedItem.ContainingProject.ProjectItems.AddFromFile(filePath);
        }

        private static string BuildViewModelContent(CodeClass codeClass, string viewModelName, bool useDataBinding, bool addValidation)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.ComponentModel;");
            if (addValidation) sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            if (useDataBinding) sb.AppendLine("using System.Windows.Input;");
            sb.AppendLine();
            sb.AppendLine($"public class {viewModelName} : INotifyPropertyChanged");
            if (addValidation) sb.AppendLine("    , IDataErrorInfo");
            sb.AppendLine("{");
            sb.AppendLine("    public event PropertyChangedEventHandler PropertyChanged;");
            sb.AppendLine();

            // Генерируем свойства для каждого свойства класса
            foreach (CodeElement member in codeClass.Members)
            {
                if (member is CodeProperty property)
                {
                    AppendProperty(sb, property, addValidation);
                }
            }

            if (useDataBinding)
            {
                sb.AppendLine();
                sb.AppendLine("    // Commands");
                sb.AppendLine("    public ICommand SubmitCommand { get; }");
            }

            if (addValidation)
            {
                sb.AppendLine();
                sb.AppendLine("    // Validation");
                sb.AppendLine("    public string Error => null;");
                sb.AppendLine();
                sb.AppendLine("    public string this[string columnName]");
                sb.AppendLine("    {");
                sb.AppendLine("        get");
                sb.AppendLine("        {");

                foreach (CodeElement member in codeClass.Members)
                {
                    if (member is CodeProperty property)
                    {
                        AppendValidationForProperty(sb, property);
                    }
                }

                sb.AppendLine("            return null;");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
            }

            sb.AppendLine();
            sb.AppendLine("    protected virtual void OnPropertyChanged(string propertyName)");
            sb.AppendLine("    {");
            sb.AppendLine("        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static void AppendProperty(StringBuilder sb, CodeProperty property, bool addValidation)
        {
            string typeName = property.Type.AsString;
            string propertyName = property.Name;

            if (addValidation)
            {
                if (typeName == "string" || typeName.Contains("String"))
                {
                    sb.AppendLine($"    [Required(ErrorMessage = \"{propertyName} is required\")]");
                }
                else if (IsNumericType(typeName))
                {
                    sb.AppendLine($"    [Range(0, int.MaxValue, ErrorMessage = \"{propertyName} must be positive\")]");
                }
            }

            sb.AppendLine($"    private {typeName} _{propertyName.ToLower()};");
            sb.AppendLine($"    public {typeName} {propertyName}");
            sb.AppendLine("    {");
            sb.AppendLine($"        get => _{propertyName.ToLower()};");
            sb.AppendLine("        set");
            sb.AppendLine("        {");
            sb.AppendLine($"            _{propertyName.ToLower()} = value;");
            sb.AppendLine($"            OnPropertyChanged(nameof({propertyName}));");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        private static void AppendValidationForProperty(StringBuilder sb, CodeProperty property)
        {
            string typeName = property.Type.AsString;
            string propertyName = property.Name;

            if (typeName == "string" || typeName.Contains("String"))
            {
                sb.AppendLine($"            if (columnName == nameof({propertyName}) && string.IsNullOrEmpty({propertyName}))");
                sb.AppendLine($"                return \"{propertyName} is required\";");
            }
            else if (IsNumericType(typeName))
            {
                sb.AppendLine($"            if (columnName == nameof({propertyName}) && {propertyName} <= 0)");
                sb.AppendLine($"                return \"{propertyName} must be positive\";");
            }
        }

        private static bool IsNumericType(string typeName)
        {
            string[] numericTypes = { "int", "Int32", "long", "Int64", "decimal", "Decimal",
                            "double", "Double", "float", "Single", "short", "Int16" };

            return numericTypes.Contains(typeName);
        }
    }
}
