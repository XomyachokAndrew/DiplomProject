using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Resolver
{

    public class TypeResolver
    {
        private static readonly HashSet<string> _valueTypes = new HashSet<string>
        {
            "int", "double", "decimal", "bool", "DateTime",
            "short", "long", "float", "byte", "char", "Guid"
        };

        public string GetPropertyType(CodeProperty property)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string typeName = property.Type.AsString.Split(new[] { '.' }).Last();
            return typeName;
        }

        public bool IsNullableType(CodeProperty property)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string typeName = property.Type.AsString;
            return typeName.EndsWith("?") ||
                   typeName.StartsWith("Nullable<") ||
                   typeName.StartsWith("System.Nullable<");
        }

        public bool IsValueType(CodeProperty property)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string typeName = GetPropertyType(property);
            return _valueTypes.Contains(typeName);
        }
    }
}
