using System;
using System.IO;
using System.Xml.Serialization;

using Fox.Core.Settings;


namespace OrderApprovalSystem.Core.Settings
{

    public static class AppSettingsManager
    {
        private static readonly string ConfigFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Settings",
            "appsettings.xml"
        );

        private static AppSettings _current;
        private static readonly XmlSerializer _serializer = new XmlSerializer(typeof(AppSettings));
        private static readonly object _lockObject = new object();

        public static AppSettings Current
        {
            get
            {
                if (_current == null)
                {
                    lock (_lockObject)
                    {
                        if (_current == null)
                        {
                            LoadSettings();
                        }
                    }
                }
                return _current;
            }
        }

        public static void LoadSettings()
        {
            lock (_lockObject)
            {
                try
                {
                    if (File.Exists(ConfigFilePath))
                    {
                        using (var stream = new FileStream(ConfigFilePath, FileMode.Open))
                        {
                            _current = (AppSettings)_serializer.Deserialize(stream);
                        }
                    }
                    else
                    {
                        // Создаем настройки по умолчанию
                        _current = new AppSettings();

                        // Наследуем базовые настройки из Fox.Core.Settings
                        _current.BuildConfiguration = SettingsManager.BuildConfiguration;
                        _current.AssemblyVersion = SettingsManager.AssemblyVersion;
                        _current.BuildTime = SettingsManager.BuildTime;

                        // Наследуем настройки логирования
                        _current.LogDirectory = SettingsManager.Current.LogDirectory;
                        _current.EnableDetailedLogging = SettingsManager.Current.EnableDetailedLogging;

                        SaveSettings();
                    }
                }
                catch
                {
                    // При ошибке создаем настройки по умолчанию
                    _current = new AppSettings();
                }
            }
        }

        public static void SaveSettings()
        {
            lock (_lockObject)
            {
                if (_current == null)
                    return;

                try
                {
                    // Создаем директорию, если не существует
                    Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));

                    // Создаем временный файл для безопасного сохранения
                    string tempFile = ConfigFilePath + ".tmp";

                    using (var stream = new FileStream(tempFile, FileMode.Create))
                    {
                        _serializer.Serialize(stream, _current);
                    }

                    // Заменяем старый файл новым
                    if (File.Exists(ConfigFilePath))
                    {
                        File.Delete(ConfigFilePath);
                    }
                    File.Move(tempFile, ConfigFilePath);
                }
                catch
                {
                    // Обработка ошибок сохранения
                }
            }
        }

        /// <summary>
        /// Инициализация менеджера настроек приложения
        /// </summary>
        public static void Initialize()
        {
            // 1. Загружаем настройки
            LoadSettings();

            // 2. Подписываемся на изменения для автосохранения
            _current.PropertyChanged += (sender, e) =>
            {
                SaveSettings();
            };
        }
    }

}
