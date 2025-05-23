using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiplomProject.Enum;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DiplomProject.Services.Detectors
{
    public static class ProjectPlatformDetector
    {
        public static PlatformType Detect(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project == null)
                return PlatformType.WPF;

            try
            {
                // Проверка по GUID проекта
                string projectTypeGuids = project.Kind.ToUpper();

                if (projectTypeGuids.Contains("{60DC8134-EBA5-43B8-BCC9-BB4BC16C2548}".ToUpper()))
                    return PlatformType.WPF;

                if (projectTypeGuids.Contains("{A5A43C5B-DE2A-4C0C-9213-0A381AF9435A}".ToUpper()))
                    return PlatformType.UWP;

                // Проверка для MAUI
                if (IsMauiProject(project))
                    return PlatformType.MAUI;
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                System.Diagnostics.Debug.WriteLine($"Ошибка определения платформы: {ex.Message}");
            }

            return PlatformType.WPF;
        }

        private static bool IsMauiProject(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                // Проверка по содержимому .csproj файла
                string projectFile = project.FileName;
                if (File.Exists(projectFile))
                {
                    string content = File.ReadAllText(projectFile);
                    return content.Contains("Microsoft.Maui") ||
                           content.Contains("UseMaui>true</UseMaui");
                }
            }
            catch { }

            return false;
        }
    }
}
