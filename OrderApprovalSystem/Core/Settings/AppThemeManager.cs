using System;

using ThemeManager;


namespace OrderApprovalSystem.Core.Settings
{

    public static class AppThemeManager
    {
        /// <summary>
        /// Инициализация и регистрация тем
        /// </summary>
        public static void InitializeThemes()
        {
            try
            {
                var themeManager = ThemeManagerService.Instance;

                // Регистрируем темы приложения
                themeManager.RegisterTheme(
                    "Default",
                    new Uri("/EmbedResources/Themes/Default.xaml", UriKind.Relative)
                );

                themeManager.RegisterTheme(
                    "Dark",
                    new Uri("/EmbedResources/Themes/Dark.xaml", UriKind.Relative)
                );

                // Регистрируем последовательности
                themeManager.RegisterThemeSequence("DefaultCycle", new[] { "Default", "Dark" });

                // Инициализируем с темой из настроек
                string savedTheme = AppSettingsManager.Current.Theme;
                themeManager.Initialize(savedTheme);

                // Подписываемся на события изменения темы
                themeManager.ThemeChanged += OnThemeChanged;
            }
            catch (Exception ex)
            {
                // Логируем ошибку
                Fox.Core.Logging.LoggerManager.MainLogger?.Error($"Ошибка инициализации тем: {ex.Message}", ex);
            }
        }

        private static void OnThemeChanged(string themeName)
        {
            // Сохраняем выбранную тему в настройках
            AppSettingsManager.Current.Theme = themeName;
        }

        /// <summary>
        /// Переключение на следующую тему
        /// </summary>
        public static void NextTheme()
        {
            ThemeManagerService.Instance.NextThemeInSequence();
        }

        /// <summary>
        /// Получение информации о текущей последовательности
        /// </summary>
        public static ThemeManagerService.SequenceInfo GetCurrentSequenceInfo()
        {
            return ThemeManagerService.Instance.GetCurrentSequenceInfo();
        }
    }

}
