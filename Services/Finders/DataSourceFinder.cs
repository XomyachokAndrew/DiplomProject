using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Finders
{
    public class DataSourceFinder
    {
        private readonly DTE _dte;

        public DataSourceFinder()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = (DTE)Package.GetGlobalService(typeof(DTE));
        }

        public string GetDbLoadItem(CodeClass codeClass, string dbProvider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string loadItems = null;
            switch (dbProvider)
            {
                case "Json":
                    loadItems = $@"if (File.Exists(JsonFilePath))
            {{
                try
                {{
                    string json = File.ReadAllText(JsonFilePath);
                    var items = JsonSerializer.Deserialize<List<{codeClass.Name}>>(json);
                    if (items != null)
                        Items = new ObservableCollection<{codeClass.Name}>(items);
                }}
                catch (Exception ex)
                {{
                    Console.WriteLine($""Ошибка загрузки данных: {{ex.Message}}"");
                }}
            }}";
                    break;
                case "DbContext":
                    loadItems = $@"_dbContext.{codeClass.Name}.Load(); // Загружаем данные в DbSet
            Items = _dbContext.{codeClass.Name}.Local.ToObservableCollection();";
                    break;
                default:
                    break;
            }
            return loadItems;
        }

        public string FindJsonFile(string projectPath, string modelName)
        {
            string[] possibleFiles = {
                Path.Combine(projectPath, $"{modelName}.json"),
                Path.Combine(projectPath, $"{modelName}s.json"),
                Path.Combine(projectPath, "Data", $"{modelName}.json"),
                Path.Combine(projectPath, "App_Data", $"{modelName}.json")
            };
            return possibleFiles.FirstOrDefault(File.Exists);
        }

        public string FindDbSetName(CodeClass modelClass, string projectPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
           
            Project project = _dte.Solution.FindProjectItem(modelClass.ProjectItem.FileNames[0]).ContainingProject;

            foreach (ProjectItem item in project.ProjectItems)
            {
                if (item.FileCodeModel != null)
                {
                    foreach (CodeElement element in item.FileCodeModel.CodeElements)
                    {
                        if (element is CodeClass dbContextClass &&
                            dbContextClass.Bases.OfType<CodeElement>().Any(b =>
                            {
                                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                                return b.Name.Contains("DbContext");
                            }))
                        {
                            foreach (CodeElement member in dbContextClass.Members)
                            {
                                if (member is CodeProperty property &&
                                    property.Type.AsString.StartsWith("DbSet<") &&
                                    property.Type.AsString.Contains(modelClass.Name))
                                {
                                    return property.Name;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        public string GetDbSaveChange(string dbProvider)
        {
            switch (dbProvider) 
            {
                case "Json":
                    return $@"
                var options = new JsonSerializerOptions {{WriteIndented = true}}; // Красивый формат JSON
                string json = JsonSerializer.Serialize(Items, options);
                File.WriteAllText(JsonFilePath, json);";
                case "DbContext":
                    return "_dbContext.SaveChanges();";
                default:
                    return null;
            }
        }

        public void BuildDbUsingCode(CodeClass codeClass, bool isUseDatabase, string dbProvider, StringBuilder dbUsingCode)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (isUseDatabase)
            {
                switch (dbProvider)
                {
                    case "Json":
                        var dte = (DTE)Package.GetGlobalService(typeof(DTE));
                        var selectedItem = dte.SelectedItems.Item(1).ProjectItem;
                        string projectPath = Path.GetDirectoryName(selectedItem.ContainingProject.FullName);
                        string jsonFilePath = FindJsonFile(projectPath, codeClass.Name);
                        dbUsingCode.Append($@"private const string JsonFilePath = @""{jsonFilePath}"";");
                        break;
                    case "DbContext":
                        dbUsingCode.Append("private readonly AppDbContext _dbContext = new AppDbContext();");
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
