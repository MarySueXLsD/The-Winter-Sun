using System;
using System.IO;
using Newtonsoft.Json;
using VisualNovel.Models;

namespace VisualNovel.Services
{
    public class ConfigService
    {
        private readonly string _configFilePath;
        private GameConfig _config;

        public ConfigService()
        {
            string configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VisualNovel");
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }
            _configFilePath = Path.Combine(configDirectory, "config.json");
            _config = LoadConfig();
        }

        public GameConfig GetConfig()
        {
            return _config;
        }

        public void UpdateConfig(GameConfig config)
        {
            _config = config;
        }

        public void SaveConfig()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(_configFilePath, json);
            }
            catch { }
        }

        public void ReloadConfig()
        {
            _config = LoadConfig();
        }

        private GameConfig LoadConfig()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath);
                    var config = JsonConvert.DeserializeObject<GameConfig>(json);
                    if (config != null)
                    {
                        return config;
                    }
                }
            }
            catch { }
            
            // Return default config if file doesn't exist or can't be loaded
            return new GameConfig();
        }
    }
}

