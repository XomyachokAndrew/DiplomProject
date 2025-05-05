using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Resolver
{

    public class TypeResolver
    {
        public string GetPropertyType(CodeProperty property)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return property.Type.AsString;
        }
    }
}
